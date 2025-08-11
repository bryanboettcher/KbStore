namespace KbStore.Catalog.Services;

using Abstractions.Contracts;
using Abstractions.Services;
using KbStore.Abstractions;
using Microsoft.EntityFrameworkCore;
using Persistence;


public class DbContextProductQueryService : IProductQueryService
{
    private readonly ApplicationDbContext _context;

    public DbContextProductQueryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponse<ProductModel>> SearchAsync<TQuery>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : ProductPaginatedQuery
    {
        var baseQuery = _context.Products.AsQueryable();

        var totalItems = await baseQuery.CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var serverQuery = baseQuery
            .Skip(query.Page * query.Size)
            .Take(query.Size)
            .Select(entity => new // Anonymous primitives for EF
            {
                ProductId = entity.CorrelationId,
                entity.Sku,
                entity.Name,
                entity.Width,
                entity.Height,
                entity.Depth,
                entity.Weight,
                InventoryItemId = entity.InventoryId,
                entity.StockThreshold,
                entity.LeadTime,
                entity.IsStocked,
                CurrentState = entity.CurrentState,
                entity.CreatedOn,
                entity.UpdatedOn
            })
            .AsAsyncEnumerable();

        var results = serverQuery
            .Select(x => new ProductReadModel( // Constructor handles translation
                x.ProductId,
                x.Sku,
                x.Name,
                GetProductDimensions(x.Width, x.Height, x.Depth, x.Weight),
                x.InventoryItemId,
                x.StockThreshold,
                x.LeadTime,
                x.IsStocked,
                x.CurrentState,
                x.CreatedOn,
                x.UpdatedOn)
            );

        return new PaginatedResponse<ProductModel>
        {
            TotalItems = totalItems,
            Page = query.Page,
            Size = query.Size,
            Results = results
        };
    }

    private static ProductDimensions? GetProductDimensions(decimal? width, decimal? height, decimal? depth, decimal? weight)
    {
        if (width == null && height == null && depth == null && weight == null)
            return null;

        return new ProductDimensions
        {
            Width = width,
            Height = height,
            Length = depth,
            Weight = weight
        };
    }


    private record ProductReadModel(
        Guid ProductId,
        string Sku,
        string? Name,
        ProductDimensions? Dimensions,
        Guid? InventoryItemId,
        int? StockThreshold,
        TimeSpan? LeadTime,
        bool IsStocked,
        int CurrentState,
        DateTimeOffset CreatedOn,
        DateTimeOffset UpdatedOn) : ProductModel
    {
        // Translation happens in the property
        public bool IsEnabled
            => CurrentState switch
            {
                3 => true, // Enabled
                4 => false, // Disabled
                6 => false, // Discontinued
                _ => false
            };

        public bool IsAvailable => IsStocked && IsEnabled;
    }
}