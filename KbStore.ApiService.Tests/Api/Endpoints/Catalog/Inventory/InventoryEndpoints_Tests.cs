namespace KbStore.ApiService.Tests.Api.Endpoints.Catalog.Inventory;

using Microsoft.AspNetCore.Http;
using NUnit.Framework;


[Category("Endpoints")]
[Category("Inventory")]
public abstract class InventoryEndpoints_Tests : Endpoints_Tests
{
    protected IResult? Output;
}