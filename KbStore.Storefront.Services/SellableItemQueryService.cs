namespace KbStore.Storefront.Services;

using KbStore.Storefront.Abstractions.Constants;
using KbStore.Storefront.Abstractions.Contracts;
using KbStore.Storefront.Abstractions.Interfaces;
using KbStore.Storefront.Domains.SellableItems;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

/// <summary>
/// Query service for reading SellableItem data directly from MongoDB.
/// Provides various retrieval methods for read-only access to sellable items.
/// </summary>
public class SellableItemQueryService : ISellableItemQueryService
{
    private readonly IMongoCollection<SellableItemEntity> _collection;
    private readonly ILogger<SellableItemQueryService> _logger;

    public SellableItemQueryService(
        IMongoDatabase database,
        ILogger<SellableItemQueryService> logger)
    {
        _collection = database.GetCollection<SellableItemEntity>(CollectionNames.SellableItems);
        _logger = logger;
    }

    public async Task<SellableItemResponse?> GetByIdAsync(
        Guid sellableItemId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving sellable item {SellableItemId}", sellableItemId);

        var entity = await _collection
            .Find(x => x.CorrelationId == sellableItemId)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity == null)
        {
            _logger.LogDebug("Sellable item {SellableItemId} not found", sellableItemId);
            return null;
        }

        return MapToResponse(entity);
    }

    public async Task<SellableItemResponse?> GetBySkuAsync(
        string sku,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving sellable item with SKU {SKU}", sku);

        var entity = await _collection
            .Find(x => x.SKU == sku)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity == null)
        {
            _logger.LogDebug("Sellable item with SKU {SKU} not found", sku);
            return null;
        }

        return MapToResponse(entity);
    }

    public async Task<List<SellableItemResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving all sellable items");

        var entities = await _collection
            .Find(FilterDefinition<SellableItemEntity>.Empty)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} sellable items", entities.Count);

        return entities.Select(MapToResponse).ToList();
    }

    public async Task<List<SellableItemResponse>> GetByItemTypeAsync(
        string itemType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving sellable items of type {ItemType}", itemType);

        var entities = await _collection
            .Find(x => x.ItemType == itemType)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} sellable items of type {ItemType}",
            entities.Count, itemType);

        return entities.Select(MapToResponse).ToList();
    }

    public async Task<List<SellableItemResponse>> GetPublishedAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving published sellable items");

        var entities = await _collection
            .Find(x => x.CurrentState == SellableItemStates.Published)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} published sellable items", entities.Count);

        return entities.Select(MapToResponse).ToList();
    }

    private static SellableItemResponse MapToResponse(SellableItemEntity entity)
    {
        return new SellableItemResponse(
            Id: entity.CorrelationId,
            ProductId: entity.ProductId,
            SKU: entity.SKU,
            Name: entity.Name,
            Description: entity.Description,
            BasePrice: entity.BasePrice,
            ItemType: entity.ItemType,
            Payload: entity.Payload,
            State: GetStateName(entity.CurrentState),
            IsAvailable: entity.IsAvailable,
            Version: entity.Version,
            CreatedAt: entity.CreatedAt,
            UpdatedAt: entity.UpdatedAt
        );
    }

    private static string GetStateName(int state)
    {
        return state switch
        {
            SellableItemStates.Draft => "Draft",
            SellableItemStates.Published => "Published",
            SellableItemStates.Hidden => "Hidden",
            SellableItemStates.Discontinued => "Discontinued",
            _ => "Unknown"
        };
    }
}
