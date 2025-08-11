namespace KbStore.Catalog.Abstractions.Services;

using Contracts;
using KbStore.Abstractions;


/// <summary>
/// Provides a variety of ways to locate products from read models.
/// </summary>
public interface IProductQueryService
{
    /// <summary>
    /// Search through existing products via pagination.
    /// </summary>
    Task<PaginatedResponse<ProductModel>> SearchAsync<TQuery>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : ProductPaginatedQuery;
}