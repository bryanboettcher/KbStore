using Microsoft.AspNetCore.Http;

namespace KbStore.ApiService.Tests.Api.Endpoints.Storefront.SellableItem;

using NUnit.Framework;


[Category("Endpoints")]
[Category("SellableItems")]
public abstract class SellableItemEndpoints_Tests : Endpoints_Tests
{
    protected IResult? Output;
}
