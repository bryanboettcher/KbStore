using Microsoft.AspNetCore.Http;

namespace KbStore.ApiService.Tests.Api.Endpoints.Catalog.Product;

using NUnit.Framework;


[Category("Endpoints")]
[Category("Products")]
public abstract class ProductEndpoints_Tests : Endpoints_Tests
{
    protected IResult? Output;
}