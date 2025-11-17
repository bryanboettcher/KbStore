using System.Security.Cryptography;
using System.Text;

namespace KbStore.Abstractions;

/// <summary>
/// Provides deterministic GUID generation using RFC 4122 UUID v5 (SHA-1 based).
/// Enables cross-domain correlation where entities with matching business keys
/// share the same CorrelationId.
/// </summary>
public static class DeterministicGuid
{
    // Namespace GUIDs for different domains (arbitrary but fixed)
    private static readonly Guid ProductNamespace = new("A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D");
    private static readonly Guid InventoryNamespace = new("B2C3D4E5-F6A7-4B6C-9D0E-1F2A3B4C5D6E");
    private static readonly Guid SellableItemNamespace = new("C3D4E5F6-A7B8-4C7D-0E1F-2A3B4C5D6E7F");

    /// <summary>
    /// Generates a deterministic GUID for a Product based on its SKU.
    /// </summary>
    /// <param name="sku">The product SKU (case-sensitive)</param>
    /// <returns>A deterministic GUID that will always be the same for the same SKU</returns>
    public static Guid FromProductSku(string sku)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku, nameof(sku));
        return CreateVersion5(ProductNamespace, sku);
    }

    /// <summary>
    /// Generates a deterministic GUID for Inventory based on its part number.
    /// </summary>
    /// <param name="partNumber">The inventory part number (case-sensitive)</param>
    /// <returns>A deterministic GUID that will always be the same for the same part number</returns>
    public static Guid FromInventoryPartNumber(string partNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partNumber, nameof(partNumber));
        return CreateVersion5(InventoryNamespace, partNumber);
    }

    /// <summary>
    /// Generates a deterministic GUID for a SellableItem based on its SKU.
    /// </summary>
    /// <param name="sku">The sellable item SKU (case-sensitive)</param>
    /// <returns>A deterministic GUID that will always be the same for the same SKU</returns>
    public static Guid FromSellableItemSku(string sku)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku, nameof(sku));
        return CreateVersion5(SellableItemNamespace, sku);
    }

    /// <summary>
    /// Creates a UUID v5 (SHA-1 based) from a namespace GUID and name.
    /// Implements RFC 4122 section 4.3.
    /// </summary>
    private static Guid CreateVersion5(Guid namespaceId, string name)
    {
        // Convert namespace GUID to network byte order (big-endian)
        var namespaceBytes = namespaceId.ToByteArray();
        SwapByteOrder(namespaceBytes);

        // Convert name to UTF-8 bytes
        var nameBytes = Encoding.UTF8.GetBytes(name);

        // Concatenate namespace and name
        var combined = new byte[namespaceBytes.Length + nameBytes.Length];
        Buffer.BlockCopy(namespaceBytes, 0, combined, 0, namespaceBytes.Length);
        Buffer.BlockCopy(nameBytes, 0, combined, namespaceBytes.Length, nameBytes.Length);

        // Compute SHA-1 hash
        var hash = SHA1.HashData(combined);

        // Take first 16 bytes
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);

        // Set version to 5 (bits 4-7 of time_hi_and_version)
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x50);

        // Set variant to RFC 4122 (bits 6-7 of clock_seq_hi_and_reserved)
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

        // Convert back to host byte order
        SwapByteOrder(guidBytes);

        return new Guid(guidBytes);
    }

    /// <summary>
    /// Swaps byte order for GUID serialization (network byte order vs host byte order).
    /// </summary>
    private static void SwapByteOrder(byte[] guid)
    {
        SwapBytes(guid, 0, 3);
        SwapBytes(guid, 1, 2);
        SwapBytes(guid, 4, 5);
        SwapBytes(guid, 6, 7);
    }

    private static void SwapBytes(byte[] array, int left, int right)
    {
        (array[left], array[right]) = (array[right], array[left]);
    }
}
