using CloudNativeInventory.Api.Controllers;
using CloudNativeInventory.Api.Data;
using CloudNativeInventory.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace CloudNativeInventory.Tests;

public class InventoryControllerTests
{
    private DbContextOptions<InventoryDbContext> CreateNewContextOptions()
    {
        return new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetProducts_ReturnsAllProducts()
    {
        var options = CreateNewContextOptions();
        using var context = new InventoryDbContext(options);
        context.Products.Add(new Product { Id = 1, Name = "Test Item", Price = 100, StockQuantity = 5 });
        await context.SaveChangesAsync();

        var mockConfig = new Mock<IConfiguration>();
        var controller = new InventoryController(context, mockConfig.Object);

        var actionResult = await controller.GetProducts();

        var result = actionResult.Value;
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public void VerifyIntegration_Returns500_WhenSecretIsLocalDefault()
    {
        var options = CreateNewContextOptions();
        using var context = new InventoryDbContext(options);

        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["ExternalServices:VendorApiKey"])
                  .Returns("LOCAL_DEV_SECRET_12345_DO_NOT_DEPLOY");

        var controller = new InventoryController(context, mockConfig.Object);

        var result = controller.VerifyExternalIntegration() as ObjectResult;

        Assert.NotNull(result);
        Assert.Equal(500, result.StatusCode);
    }
}