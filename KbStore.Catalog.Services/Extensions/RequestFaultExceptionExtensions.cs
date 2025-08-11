namespace KbStore.Catalog.Services.Extensions;

using KbStore.Catalog.Abstractions.Exceptions;
using MassTransit;

/// <summary>
/// Extension methods for converting MassTransit RequestFaultException to domain-specific inventory exceptions.
/// </summary>
public static class RequestFaultExceptionExtensions
{
    public static InventoryException ToInventoryException(this RequestFaultException faultException)
    {
        var fault = faultException.Fault?.Exceptions.FirstOrDefault();
        if (fault == null)
            return new GenericInventoryException("Unknown inventory operation failed");

        var message = fault.Message ?? "Unknown error";
        var data = fault.Data ?? new Dictionary<string, object>();

        return fault.ExceptionType switch
        {
            nameof(InventoryNotFoundException) => RecreateNotFoundException(message, data),
            nameof(InventoryConflictException) => RecreateConflictException(message, data),
            nameof(InventoryValidationException) => new InventoryValidationException(message),
            nameof(InventoryStateException) => RecreateStateException(message, data),
            _ => new GenericInventoryException(message)
        };
    }

    private static InventoryNotFoundException RecreateNotFoundException(string message, IDictionary<string, object> data)
    {
        if (data.TryGetValue("inventoryId", out var idObj) && idObj is Guid inventoryId)
            return new InventoryNotFoundException(inventoryId);
        return new InventoryNotFoundException(Guid.Empty);
    }

    private static InventoryConflictException RecreateConflictException(string message, IDictionary<string, object> data)
    {
        if (data.TryGetValue("partNumber", out var partNumberObj) && partNumberObj is string partNumber)
            return InventoryConflictException.DuplicatePartNumber(partNumber);
        return new InventoryConflictException(message);
    }

    private static InventoryStateException RecreateStateException(string message, IDictionary<string, object> data)
    {
        var inventoryId = data.TryGetValue("inventoryId", out var idObj) && idObj is Guid id ? id : Guid.Empty;
        var currentState = data.TryGetValue("currentState", out var stateObj) ? stateObj?.ToString() : "Unknown";
        var operation = data.TryGetValue("attemptedOperation", out var opObj) ? opObj?.ToString() : "Unknown";

        return new InventoryStateException(inventoryId, currentState, operation);
    }
}