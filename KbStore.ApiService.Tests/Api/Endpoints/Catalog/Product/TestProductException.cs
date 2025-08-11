namespace KbStore.ApiService.Tests.Api.Endpoints.Catalog.Product;

using KbStore.Catalog.Abstractions.Exceptions;


public class TestProductException : ProductException
{
    public TestProductException(string message)
        : base(message)
    {
    }

    public TestProductException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}