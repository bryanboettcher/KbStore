namespace KbStore.Storefront.Services;

using KbStore.Storefront.Abstractions.Constants;
using KbStore.Storefront.Abstractions.Contracts;
using KbStore.Storefront.Abstractions.Interfaces;
using KbStore.Storefront.Domains.SellableItems;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

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

    public async Task<SellableItemModel?> GetByIdAsync(
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

        return MapToModel(entity);
    }

    public async Task<SellableItemModel?> GetBySkuAsync(
        string sku,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving sellable item with SKU {SKU}", sku);

        var entity = await _collection
            .Find(x => x.Sku == sku)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity == null)
        {
            _logger.LogDebug("Sellable item with SKU {SKU} not found", sku);
            return null;
        }

        return MapToModel(entity);
    }

    public async Task<List<SellableItemModel>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving all sellable items");

        var entities = await _collection
            .Find(FilterDefinition<SellableItemEntity>.Empty)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} sellable items", entities.Count);

        return entities.Select(MapToModel).ToList();
    }

    public async Task<List<SellableItemModel>> GetByItemTypeAsync(
        string itemType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving sellable items of type {ItemType}", itemType);

        var entities = await _collection
            .Find(x => x.ItemType == itemType)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} sellable items of type {ItemType}",
            entities.Count, itemType);

        return entities.Select(MapToModel).ToList();
    }

    public async Task<List<SellableItemModel>> GetPublishedAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving published sellable items");

        var entities = await _collection
            .Find(x => x.CurrentState == SellableItemStates.Published)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} published sellable items", entities.Count);

        return entities.Select(MapToModel).ToList();
    }

    private static SellableItemModel MapToModel(SellableItemEntity entity)
    {
        return new SellableItemModelImpl(
            entity.CorrelationId,
            entity.ProductId,
            entity.Sku,
            entity.Name,
            entity.Description,
            entity.BasePrice,
            entity.ItemType,
            entity.Payload,
            entity.IsAvailable,
            entity.Version,
            entity.CreatedOn,
            entity.UpdatedOn);
    }

    private sealed record SellableItemModelImpl(
        Guid SellableItemId,
        Guid? ProductId,
        string Sku,
        string Name,
        string? Description,
        decimal BasePrice,
        string ItemType,
        IReadOnlyDictionary<string, object?> Payload,
        bool IsAvailable,
        int Version,
        DateTimeOffset CreatedOn,
        DateTimeOffset UpdatedOn) : SellableItemModel
    {
        public Guid CorrelationId => SellableItemId;
    }
}
