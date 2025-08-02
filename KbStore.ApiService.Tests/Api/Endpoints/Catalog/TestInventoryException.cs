namespace KbStore.ApiService.Tests.Api.Endpoints.Catalog;

using KbStore.Catalog.Abstractions.Exceptions;


public class TestInventoryException : InventoryException
{
    public TestInventoryException(string message) : base(message) { }
}