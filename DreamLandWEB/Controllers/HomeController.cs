using DreamLandWEB.Data;
using DreamLandWEB.Enums;
using DreamLandWEB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace DreamLandWEB.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(CategoriaProduto? categoria)
        {
            var produtos = _context.Produtos
                .Where(p => p.Disponivel && p.Estoque > 0)
                .AsQueryable();

            if (categoria.HasValue)
                produtos = produtos.Where(p => p.Categoria == categoria.Value);

            ViewBag.CategoriaSelecionada = categoria;

            return View(await produtos.OrderByDescending(p => p.DataCadastro).ToListAsync());
        }

        public IActionResult Privacy()
        {
            return View();
        }

    }
}
