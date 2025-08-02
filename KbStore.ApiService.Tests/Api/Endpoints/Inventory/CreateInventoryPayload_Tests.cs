namespace KbStore.ApiService.Tests.Api.Endpoints.Inventory;

using ApiService.Endpoints.Catalog;
using NUnit.Framework;
using Shouldly;

#pragma warning disable CS8618
[TestFixture]
public class CreateInventoryPayload_Tests
{
    // PartNumber, Description, StockQuantity, IsValid
    [TestCase(null, "test description", 1000, false)]
    [TestCase("", "test description", 1000, false)]
    [TestCase("   ", "test description", 1000, false)]
    [TestCase("FAST_M3X20", "test description", -1, false)]
    [TestCase("FAST_M3X20", "", -1, false)]
    [TestCase("FAST_M3X20", "", 1000, true)]
    [TestCase("FAST_M3X20", "   ", 1000, true)]
    [TestCase("FAST_M3X20", "test description", 0, true)]
    [TestCase("FAST_M3X20", "test description", 1000, true)]
    [TestCase("FAST_M3X20", "test description", int.MaxValue, true)]
    public void IsValid_should_return_expected_result(string? partNumber, string? description, int stockQuantity, bool expectedValid)
    {
        var payload = new CreateInventoryPayload
        {
            PartNumber = partNumber,
            Description = description,
            StockQuantity = stockQuantity
        };

        payload.IsValid().ShouldBe(expectedValid);
    }

    [Test]
    public void Description_can_be_null()
    {
        var payload = new CreateInventoryPayload
        {
            PartNumber = "FAST_M3X20",
            Description = null,
            StockQuantity = 1000
        };

        payload.IsValid().ShouldBeTrue();
    }
}