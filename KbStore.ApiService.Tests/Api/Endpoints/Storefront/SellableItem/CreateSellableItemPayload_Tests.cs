namespace KbStore.ApiService.Tests.Api.Endpoints.Storefront.SellableItem;

using KbStore.ApiService.Endpoints.Storefront;
using NUnit.Framework;
using Shouldly;

#pragma warning disable CS8618
[TestFixture]
public class CreateSellableItemPayload_Tests
{
    // Sku, Name, ItemType, BasePrice, IsValid
    [TestCase(null, "Name", "Physical", 9.99, false)]
    [TestCase("", "Name", "Physical", 9.99, false)]
    [TestCase("   ", "Name", "Physical", 9.99, false)]
    [TestCase("SKU-001", null, "Physical", 9.99, false)]
    [TestCase("SKU-001", "", "Physical", 9.99, false)]
    [TestCase("SKU-001", "   ", "Physical", 9.99, false)]
    [TestCase("SKU-001", "Name", null, 9.99, false)]
    [TestCase("SKU-001", "Name", "", 9.99, false)]
    [TestCase("SKU-001", "Name", "   ", 9.99, false)]
    [TestCase("SKU-001", "Name", "Physical", -1.00, false)]
    [TestCase("SKU-001", "Name", "Physical", -0.01, false)]
    [TestCase("SKU-001", "Name", "Physical", 0.00, true)]
    [TestCase("SKU-001", "Name", "Physical", 9.99, true)]
    [TestCase("SKU-001", "Name", "Physical", 99999.99, true)]
    public void IsValid_should_return_expected_result(string? sku, string? name, string? itemType, decimal basePrice, bool expectedValid)
    {
        var payload = new CreateSellableItemPayload
        {
            Sku = sku,
            Name = name,
            ItemType = itemType,
            BasePrice = basePrice,
            Description = "Test description"
        };

        payload.IsValid().ShouldBe(expectedValid);
    }

    [Test]
    public void Description_can_be_null()
    {
        var payload = new CreateSellableItemPayload
        {
            Sku = "SKU-001",
            Name = "Test Name",
            ItemType = "Physical",
            BasePrice = 9.99m,
            Description = null
        };

        payload.IsValid().ShouldBeTrue();
    }

    [Test]
    public void Payload_can_be_null()
    {
        var payload = new CreateSellableItemPayload
        {
            Sku = "SKU-001",
            Name = "Test Name",
            ItemType = "Physical",
            BasePrice = 9.99m,
            Payload = null
        };

        payload.IsValid().ShouldBeTrue();
    }

    [Test]
    public void ProductId_can_be_null()
    {
        var payload = new CreateSellableItemPayload
        {
            Sku = "SKU-001",
            Name = "Test Name",
            ItemType = "Physical",
            BasePrice = 9.99m,
            ProductId = null
        };

        payload.IsValid().ShouldBeTrue();
    }

    [Test]
    public void ProductId_can_have_value()
    {
        var payload = new CreateSellableItemPayload
        {
            Sku = "SKU-001",
            Name = "Test Name",
            ItemType = "Physical",
            BasePrice = 9.99m,
            ProductId = Guid.NewGuid()
        };

        payload.IsValid().ShouldBeTrue();
    }
}
