namespace KbStore.Catalog.Services;

/// <summary>
/// Generic InventoryException for unmapped failure types.
/// </summary>
public class InventoryException : Exception
{
    public InventoryException(string message) : base(message) { }
    public InventoryException(string message, Exception innerException) : base(message, innerException) { }
}