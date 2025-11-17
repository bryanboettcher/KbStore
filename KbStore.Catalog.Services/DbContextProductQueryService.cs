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
                entity.Quantity,
                entity.InventoryId,
                entity.StockThreshold,
                entity.LeadTime,
                entity.IsStocked,
                entity.CurrentState,
                entity.CreatedOn,
                entity.UpdatedOn
            })
            .AsAsyncEnumerable();

        var results = serverQuery
            .Select(x => new ProductReadModel(
                x.ProductId,
                x.Sku,
                x.Name,
                GetProductDimensions(x.Width, x.Height, x.Depth, x.Weight),
                x.Quantity,
                x.InventoryId,
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
        int Quantity,
        Guid? InventoryId,
        int? StockThreshold,
        TimeSpan? LeadTime,
        bool IsStocked,
        int CurrentState,
        DateTimeOffset CreatedOn,
        DateTimeOffset UpdatedOn) : ProductModel
    {
        public Guid CorrelationId => ProductId;

        public bool IsEnabled
            => CurrentState switch
            {
                ProductStates.Enabled => true,
                ProductStates.Disabled => false,
                ProductStates.Discontinued => false,
                _ => false
            };

        public bool IsAvailable => IsStocked && IsEnabled;
    }
}