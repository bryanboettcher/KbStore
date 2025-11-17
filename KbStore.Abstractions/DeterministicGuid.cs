using System.Buffers.Binary;
using System.IO.Hashing;
using System.Text;

namespace KbStore.Abstractions;

/// <summary>
/// Provides deterministic, zero-allocation GUID generation using XxHash64.
/// Enables cross-domain correlation where entities with matching business keys
/// share the same CorrelationId.
/// </summary>
/// <remarks>
/// <para>
/// GUIDs are structured as: [MacroNamespace(4)][Domain(2)][Version(2)][Hash(8)]
/// </para>
/// <para>
/// <b>MacroNamespace:</b> Installation-specific identifier (default: "KbSt" = 0x4B625374).
/// Allows test isolation by changing the namespace without affecting production data.
/// </para>
/// <para>
/// <b>Domain:</b> Identifies the bounded context (1=Product, 2=Inventory, 3=SellableItem).
/// </para>
/// <para>
/// <b>Version:</b> Schema version for future-proofing (currently 0).
/// </para>
/// <para>
/// <b>Hash:</b> XxHash64 of UTF-8 encoded input (SKU, part number, etc.).
/// </para>
/// <para>
/// <b>Zero-allocation design:</b> Uses stackalloc and Span&lt;byte&gt; for all operations.
/// No heap allocations occur during GUID generation.
/// </para>
/// </remarks>
public static class DeterministicGuid
{
    /// <summary>
    /// MacroNamespace identifier for this installation (default: "KbSt" = 0x4B625374).
    /// Can be modified for test isolation via <see cref="SetMacroNamespace"/>.
    /// </summary>
    private static int _macroNamespace = 0x4B625374; // "KbSt" in ASCII

    /// <summary>
    /// Sets the MacroNamespace to a custom value for test isolation.
    /// </summary>
    /// <param name="value">The new MacroNamespace value</param>
    /// <remarks>
    /// This is primarily used in tests to generate different GUIDs for the same
    /// business keys without polluting production data.
    /// </remarks>
    public static void SetMacroNamespace(int value) => _macroNamespace = value;

    /// <summary>
    /// Resets the MacroNamespace to the production default ("KbSt" = 0x4B625374).
    /// </summary>
    public static void ResetToProduction() => _macroNamespace = 0x4B625374;

    /// <summary>
    /// Generates a deterministic GUID for a Product based on its SKU.
    /// </summary>
    /// <param name="sku">The product SKU (case-sensitive)</param>
    /// <returns>A deterministic GUID that will always be the same for the same SKU</returns>
    /// <remarks>
    /// Uses domain=1, version=0. Zero allocations.
    /// </remarks>
    public static Guid FromProductSku(string sku)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku, nameof(sku));
        return From(domain: 1, version: 0, sku.AsSpan());
    }

    /// <summary>
    /// Generates a deterministic GUID for Inventory based on its part number.
    /// </summary>
    /// <param name="partNumber">The inventory part number (case-sensitive)</param>
    /// <returns>A deterministic GUID that will always be the same for the same part number</returns>
    /// <remarks>
    /// Uses domain=2, version=0. Zero allocations.
    /// </remarks>
    public static Guid FromInventoryPartNumber(string partNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partNumber, nameof(partNumber));
        return From(domain: 2, version: 0, partNumber.AsSpan());
    }

    /// <summary>
    /// Generates a deterministic GUID for a SellableItem based on its SKU.
    /// </summary>
    /// <param name="sku">The sellable item SKU (case-sensitive)</param>
    /// <returns>A deterministic GUID that will always be the same for the same SKU</returns>
    /// <remarks>
    /// Uses domain=3, version=0. Zero allocations.
    /// </remarks>
    public static Guid FromSellableItemSku(string sku)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku, nameof(sku));
        return From(domain: 3, version: 0, sku.AsSpan());
    }

    /// <summary>
    /// Core zero-allocation GUID generation logic.
    /// </summary>
    /// <param name="domain">Domain identifier (1=Product, 2=Inventory, 3=SellableItem)</param>
    /// <param name="version">Schema version (currently 0)</param>
    /// <param name="input">Business key to hash (SKU, part number, etc.)</param>
    /// <returns>A deterministic GUID composed of [MacroNamespace][Domain][Version][Hash]</returns>
    /// <remarks>
    /// Uses stackalloc for all buffers. No heap allocations.
    /// </remarks>
    private static Guid From(int domain, int version, ReadOnlySpan<char> input)
    {
        // 16-byte buffer for complete GUID
        Span<byte> guidBytes = stackalloc byte[16];

        // Write first 8 bytes (structured components)
        BinaryPrimitives.WriteInt32LittleEndian(guidBytes[0..4], _macroNamespace);
        BinaryPrimitives.WriteInt16LittleEndian(guidBytes[4..6], (short)domain);
        BinaryPrimitives.WriteInt16LittleEndian(guidBytes[6..8], (short)version);

        // UTF-8 encode input (worst case: 3 bytes per char)
        Span<byte> buffer = stackalloc byte[input.Length * 3];
        int bytesWritten = Encoding.UTF8.GetBytes(input, buffer);

        // Hash directly into last 8 bytes
        XxHash64.Hash(buffer[..bytesWritten], guidBytes[8..16]);

        return new Guid(guidBytes);
    }
}
