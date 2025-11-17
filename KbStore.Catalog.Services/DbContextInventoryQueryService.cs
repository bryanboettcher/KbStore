namespace KbStore.Catalog.Services;

using Abstractions.Contracts;
using Abstractions.Services;
using KbStore.Abstractions;
using Microsoft.EntityFrameworkCore;
using Persistence;


public class DbContextInventoryQueryService : IInventoryQueryService
{
    private readonly ApplicationDbContext _context;

    public DbContextInventoryQueryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponse<InventoryModel>> SearchAsync<TQuery>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : PaginatedQuery
    {
        var baseQuery = _context.Inventory.AsQueryable();

        var totalItems = await baseQuery.CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var serverQuery = baseQuery
            .Skip(query.Page * query.Size)
            .Take(query.Size)
            .Select(entity => new  // Anonymous primitives for EF
            {
                InventoryId = entity.CorrelationId,
                PartNumber = entity.PartNumber,
                Description = entity.Description,
                StockQuantity = entity.StockQuantity,
                CurrentState = entity.CurrentState,
                CreatedOn = entity.CreatedOn,
                UpdatedOn = entity.UpdatedOn
            })
            .AsAsyncEnumerable();

        var results = serverQuery
            .Select(x => new InventoryReadModel(  // Constructor handles translation
                x.InventoryId,
                x.PartNumber,
                x.Description,
                x.StockQuantity,
                x.CurrentState,
                x.CreatedOn,
                x.UpdatedOn)
            );

        return new PaginatedResponse<InventoryModel>
        {
            TotalItems = totalItems,
            Page = query.Page,
            Size = query.Size,
            Results = results
        };
    }

    private record InventoryReadModel(
        Guid InventoryId,
        string PartNumber,
        string Description,
        int StockQuantity,
        int CurrentState,
        DateTimeOffset CreatedOn,
        DateTimeOffset UpdatedOn) : InventoryModel
    {
        public Guid CorrelationId => InventoryId;

        // Translation happens in the property
        public InventoryStatus Status => CurrentState switch
        {
            3 => InventoryStatus.Available,
            4 => InventoryStatus.Held,
            5 => InventoryStatus.Backordered,
            6 => InventoryStatus.Discontinued,
            _ => InventoryStatus.Invalid
        };
    }
}