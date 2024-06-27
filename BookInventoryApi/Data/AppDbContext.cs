using Microsoft.EntityFrameworkCore;
using BookInventoryApi.Models;
using System.Collections.Generic;

namespace BookInventoryApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Book> Books { get; set; }
    }
}