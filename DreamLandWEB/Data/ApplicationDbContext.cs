using DreamLandWEB.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace DreamLandWEB.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
       : base(options) { }

        public DbSet<Produto> Produtos { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<ItemPedido> ItensPedido { get; set; }
    }

}
