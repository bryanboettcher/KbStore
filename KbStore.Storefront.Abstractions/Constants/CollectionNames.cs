namespace KbStore.Storefront.Abstractions.Constants;

/// <summary>
/// MongoDB collection names used by both MassTransit saga repositories and query services.
/// These names must match exactly between saga configuration and query service collection access.
/// Internal to Storefront domain and services layer only.
/// </summary>
public static class CollectionNames
{
    public const string SellableItems = "sellable-items";
    public const string Carts = "carts";
    public const string Orders = "orders";
    public const string Bundles = "bundles";
    public const string Wishlists = "wishlists";
    public const string Quotes = "quotes";
}
