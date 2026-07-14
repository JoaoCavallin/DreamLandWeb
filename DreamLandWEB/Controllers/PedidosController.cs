using DreamLandWEB.Data;
using DreamLandWEB.Helpers;
using DreamLandWEB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DreamLandWEB.Controllers
{
    public class PedidosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PedidosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Pedidos
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Pedidos.Include(p => p.Usuario);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Pedidos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pedido = await _context.Pedidos
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (pedido == null)
            {
                return NotFound();
            }

            return View(pedido);
        }

        // GET: Pedidos/Create
        public IActionResult Create()
        {
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "Id", "Id");
            return View();
        }

        // POST: Pedidos/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UsuarioId,Data,Total,Status")] Pedido pedido)
        {
            if (ModelState.IsValid)
            {
                _context.Add(pedido);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "Id", "Id", pedido.UsuarioId);
            return View(pedido);
        }

        // GET: Pedidos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null)
            {
                return NotFound();
            }
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "Id", "Id", pedido.UsuarioId);
            return View(pedido);
        }

        // POST: Pedidos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UsuarioId,Data,Total,Status")] Pedido pedido)
        {
            if (id != pedido.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(pedido);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PedidoExists(pedido.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "Id", "Id", pedido.UsuarioId);
            return View(pedido);
        }

        // GET: Pedidos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pedido = await _context.Pedidos
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (pedido == null)
            {
                return NotFound();
            }

            return View(pedido);
        }

        // POST: Pedidos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido != null)
            {
                _context.Pedidos.Remove(pedido);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PedidoExists(int id)
        {
            return _context.Pedidos.Any(e => e.Id == id);
        }

        private const string CarrinhoSessionKey = "Carrinho";

        // GET: /Pedido/Checkout — tela de confirmação antes de finalizar
        [Authorize]
        public IActionResult Checkout()
        {
            var carrinho = HttpContext.Session.GetObject<List<CarrinhoItem>>(CarrinhoSessionKey)
                           ?? new List<CarrinhoItem>();

            if (!carrinho.Any())
            {
                TempData["Erro"] = "Seu carrinho está vazio.";
                return RedirectToAction("Index", "Carrinho");
            }

            ViewBag.Total = carrinho.Sum(i => i.Subtotal);
            return View(carrinho);
        }

        // POST: /Pedido/ConfirmarCheckout — cria o Pedido de fato
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarCheckout()
        {
            var carrinho = HttpContext.Session.GetObject<List<CarrinhoItem>>(CarrinhoSessionKey)
                           ?? new List<CarrinhoItem>();

            if (!carrinho.Any())
            {
                TempData["Erro"] = "Seu carrinho está vazio.";
                return RedirectToAction("Index", "Carrinho");
            }

            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Revalida estoque antes de confirmar (evita corrida entre dois clientes)
            foreach (var item in carrinho)
            {
                var produto = await _context.Produtos.FindAsync(item.ProdutoId);
                if (produto == null || !produto.Disponivel || produto.Estoque < item.Quantidade)
                {
                    TempData["Erro"] = $"O produto '{item.Nome}' não está mais disponível na quantidade desejada.";
                    return RedirectToAction("Index", "Carrinho");
                }
            }

            var pedido = new Pedido
            {
                UsuarioId = usuarioId,
                Data = DateTime.Now,
                Status = "Pendente",
                Total = carrinho.Sum(i => i.Subtotal),
                Itens = new List<ItemPedido>()
            };

            foreach (var item in carrinho)
            {
                pedido.Itens.Add(new ItemPedido
                {
                    ProdutoId = item.ProdutoId,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = item.Preco
                });

                // Reduz o estoque
                var produto = await _context.Produtos.FindAsync(item.ProdutoId);
                produto!.Estoque -= item.Quantidade;
                if (produto.Estoque == 0)
                    produto.Disponivel = false;
            }

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // Limpa o carrinho
            HttpContext.Session.Remove(CarrinhoSessionKey);

            return RedirectToAction("Confirmacao", new { id = pedido.Id });
        }

        // GET: /Pedido/Confirmacao/5
        [Authorize]
        public async Task<IActionResult> Confirmacao(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                .ThenInclude(i => i.Produto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();

            // Garante que o usuário só veja o próprio pedido (a menos que seja admin)
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var isAdmin = User.FindFirstValue("Admin") == "True";

            if (pedido.UsuarioId != usuarioId && !isAdmin)
                return Forbid();

            return View(pedido);
        }
    }
}
