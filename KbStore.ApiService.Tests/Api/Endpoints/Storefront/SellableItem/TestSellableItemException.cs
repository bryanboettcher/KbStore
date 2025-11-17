namespace KbStore.ApiService.Tests.Api.Endpoints.Storefront.SellableItem;

using KbStore.Storefront.Abstractions.Exceptions;


public class TestSellableItemException : SellableItemException
{
    public TestSellableItemException(string message)
        : base(message)
    {
    }

    public TestSellableItemException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
