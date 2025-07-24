using KbStore.ApiService.Endpoints.Inventory;
using KbStore.Contracts.Domains;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using Shouldly;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace KbStore.ApiService.Tests.Api.Endpoints;

[TestFixture]
public class InventoryEndpoint_Tests : Endpoint_Tests
{
    protected IResult? Output;
}

public class CreateInventory_Tests : InventoryEndpoint_Tests
{
    protected override async Task OnPostSetup()
    {
        var payload = new CreateInventoryPayload
        {
            Description = "test description",
            InventoryStatus = InventoryStatus.InStock,
            PartNumber = "FAST_M3X20",
            StockQuantity = 1000,
        };

        Output = Execute(InventoryEndpoints.Create, payload);
        await base.OnPostSetup();
    }

    [Test]
    public void It_should_return_something() => Output.ShouldNotBeNull();

    [Test]
    public void It_should_be_successful() => Output.ShouldBeOfType<OkObjectResult>();
}