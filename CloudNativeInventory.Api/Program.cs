using Azure.Identity;
using CloudNativeInventory.Api.Data;
using CloudNativeInventory.Api.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Enable Key Vault integration for production environments
if (builder.Environment.IsProduction())
{
    var keyVaultUrl = new Uri(builder.Configuration["KeyVaultUrl"]!);
    builder.Configuration.AddAzureKeyVault(keyVaultUrl, new DefaultAzureCredential());
}

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseInMemoryDatabase("InventoryDb"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

    if (!db.Products.Any())
    {
        db.Products.Add(new Product { Id = 1, Name = "Laptop", Price = 9999, StockQuantity = 10 });
        db.SaveChanges();
    }
}

app.Run();