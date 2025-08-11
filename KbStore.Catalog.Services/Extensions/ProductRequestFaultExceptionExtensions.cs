namespace KbStore.Catalog.Services.Extensions;

using Abstractions.Exceptions;
using MassTransit;


/// <summary>
/// Extension methods for converting MassTransit RequestFaultException to domain-specific product exceptions.
/// </summary>
public static class ProductRequestFaultExceptionExtensions
{
    public static ProductException ToProductException(this RequestFaultException faultException)
    {
        var fault = faultException.Fault?.Exceptions.FirstOrDefault();
        if (fault == null)
            return new GenericProductException("Unknown product operation failed");

        var message = fault.Message ?? "Unknown error";
        var data = fault.Data ?? new Dictionary<string, object>();

        var typeName = fault.ExceptionType?.Split('.').LastOrDefault() ?? "";
        return typeName switch
        {
            nameof(ProductNotFoundException) => RecreateNotFoundException(message, data),
            nameof(ProductConflictException) => RecreateConflictException(message, data),
            nameof(ProductStateException) => RecreateStateException(message, data),
            nameof(ProductValidationException) => new ProductValidationException(message),
            _ => new GenericProductException(message)
        };
    }

    private static ProductNotFoundException RecreateNotFoundException(string message, IDictionary<string, object> data)
    {
        if (data.TryGetValue("productId", out var idObj) && idObj is Guid productId)
            return new ProductNotFoundException(productId);
        return new ProductNotFoundException(Guid.Empty);
    }

    private static ProductConflictException RecreateConflictException(string message, IDictionary<string, object> data)
    {
        if (data.TryGetValue("sku", out var skuObj) && skuObj is string sku)
            return ProductConflictException.DuplicateSku(sku);
        return new ProductConflictException(message);
    }

    private static ProductStateException RecreateStateException(string message, IDictionary<string, object> data)
    {
        var productId = data.TryGetValue("productId", out var idObj) && idObj is Guid id ? id : Guid.Empty;
        var currentState = data.TryGetValue("currentState", out var stateObj) ? stateObj?.ToString() : "Unknown";
        var operation = data.TryGetValue("attemptedOperation", out var opObj) ? opObj?.ToString() : "Unknown";

        return new ProductStateException(productId, currentState, operation);
    }
}