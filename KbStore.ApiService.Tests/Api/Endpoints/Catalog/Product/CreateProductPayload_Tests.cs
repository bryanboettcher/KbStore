namespace KbStore.ApiService.Tests.Api.Endpoints.Catalog.Product;

using KbStore.ApiService.Endpoints.Catalog;
using NUnit.Framework;
using Shouldly;

#pragma warning disable CS8618
[TestFixture]
public class CreateProductPayload_Tests
{
    // Sku, StockThreshold, LeadTime, IsValid
    [TestCase(null, 10, 7, false)]
    [TestCase("", 10, 7, false)]
    [TestCase("   ", 10, 7, false)]
    [TestCase("TEST_SKU", -1, 7, false)]
    [TestCase("TEST_SKU", 10, -1, false)]
    [TestCase("TEST_SKU", 0, 0, true)]
    [TestCase("TEST_SKU", 10, 7, true)]
    public void IsValid_should_return_expected_result(string? sku, int? stockThreshold, int leadTimeDays, bool expectedValid)
    {
        var payload = new CreateProductPayload
        {
            Sku = sku,
            StockThreshold = stockThreshold,
            LeadTime = leadTimeDays >= 0 ? TimeSpan.FromDays(leadTimeDays) : TimeSpan.FromDays(-1)
        };

        payload.IsValid().ShouldBe(expectedValid);
    }

    [Test]
    public void Name_can_be_null()
    {
        var payload = new CreateProductPayload
        {
            Sku = "TEST_SKU",
            Name = null,
            StockThreshold = 10
        };

        payload.IsValid().ShouldBeTrue();
    }

    [Test]
    public void Dimensions_can_be_null()
    {
        var payload = new CreateProductPayload
        {
            Sku = "TEST_SKU",
            Dimensions = null,
            StockThreshold = 10
        };

        payload.IsValid().ShouldBeTrue();
    }

    [Test]
    public void InventoryItemId_can_be_null()
    {
        var payload = new CreateProductPayload
        {
            Sku = "TEST_SKU",
            InventoryId = null,
            StockThreshold = 10
        };

        payload.IsValid().ShouldBeTrue();
    }
}