namespace KbStore.Storefront.Services.Extensions;

using KbStore.Storefront.Abstractions.Exceptions;
using MassTransit;


public static class SellableItemRequestFaultExceptionExtensions
{
    public static SellableItemException ToSellableItemException(this RequestFaultException faultException)
    {
        var fault = faultException.Fault?.Exceptions.FirstOrDefault();
        if (fault == null)
            return new GenericSellableItemException("Unknown sellable item operation failed");

        var message = fault.Message ?? "Unknown error";
        var data = fault.Data ?? new Dictionary<string, object>();

        var typeName = fault.ExceptionType?.Split('.').LastOrDefault() ?? "";
        return typeName switch
        {
            nameof(SellableItemNotFoundException) => RecreateNotFoundException(data),
            nameof(SellableItemConflictException) => RecreateConflictException(message, data),
            nameof(SellableItemStateException) => RecreateStateException(data),
            nameof(SellableItemValidationException) => new SellableItemValidationException(message),
            _ => new GenericSellableItemException(message)
        };
    }

    private static SellableItemNotFoundException RecreateNotFoundException(IDictionary<string, object> data)
    {
        if (data.TryGetValue("sellableItemId", out var idObj) && idObj is Guid sellableItemId)
            return new SellableItemNotFoundException(sellableItemId);
        return new SellableItemNotFoundException(Guid.Empty);
    }

    private static SellableItemConflictException RecreateConflictException(string message, IDictionary<string, object> data)
    {
        if (data.TryGetValue("sku", out var skuObj) && skuObj is string sku)
            return SellableItemConflictException.DuplicateSku(sku);
        return new SellableItemConflictException(message);
    }

    private static SellableItemStateException RecreateStateException(IDictionary<string, object> data)
    {
        var currentState = (data.TryGetValue("currentState", out var stateObj) ? stateObj?.ToString() : null) ?? "Unknown";
        var attemptedAction = (data.TryGetValue("attemptedAction", out var actionObj) ? actionObj?.ToString() : null) ?? "Unknown";

        return new SellableItemStateException(currentState, attemptedAction);
    }
}
