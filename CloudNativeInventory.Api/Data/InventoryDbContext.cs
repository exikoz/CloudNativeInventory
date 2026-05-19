using CloudNativeInventory.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudNativeInventory.Api.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
}