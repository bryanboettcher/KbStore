using KbStore.Storefront.Domains.SellableItems;
using KbStore.Tests;
using MongoDB.Driver;
using NSubstitute;
// ReSharper disable StaticMemberInGenericType

namespace KbStore.Storefront.Tests.Services;

using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;


[TestFixture]
public abstract class QueryService_Tests<TService> : EventingTestBase
    where TService : class
{
    protected static readonly Guid ExistingId = Guid.Parse("abcd1234-bbbb-cccc-dddd-deadbeef0001");

    protected static readonly DateTimeOffset Now = new(2025, 06, 16, 13, 30, 00, TimeSpan.Zero);

    protected TService Subject = null!;
    protected IMongoDatabase MockDatabase = null!;
    protected IMongoCollection<SellableItemEntity> MockCollection = null!;
    protected List<SellableItemEntity> TestData = new();

    protected override void OnServicesCreating(IServiceCollection services)
    {
        base.OnServicesCreating(services);

        MockCollection = Substitute.For<IMongoCollection<SellableItemEntity>>();
        MockDatabase = Substitute.For<IMongoDatabase>();
        MockDatabase.GetCollection<SellableItemEntity>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>())
            .Returns(MockCollection);

        services.AddSingleton(MockDatabase);
        services.AddTransient<TService>();
    }

    protected override void Arrange()
    {
        Subject = ScopedProvider.ServiceProvider.GetRequiredService<TService>();
    }

    protected void SetupFindReturnsEmpty()
    {
        var cursor = CreateEmptyCursor();
        MockCollection.FindAsync(
                Arg.Any<FilterDefinition<SellableItemEntity>>(),
                Arg.Any<FindOptions<SellableItemEntity, SellableItemEntity>>(),
                Arg.Any<CancellationToken>())
            .Returns(cursor);
    }

    protected void SetupFindReturnsItems(params SellableItemEntity[] items)
    {
        TestData = items.ToList();
        var cursor = CreateCursorWithData(items);
        MockCollection.FindAsync(
                Arg.Any<FilterDefinition<SellableItemEntity>>(),
                Arg.Any<FindOptions<SellableItemEntity, SellableItemEntity>>(),
                Arg.Any<CancellationToken>())
            .Returns(cursor);
    }

    private static IAsyncCursor<SellableItemEntity> CreateEmptyCursor()
    {
        var cursor = Substitute.For<IAsyncCursor<SellableItemEntity>>();
        cursor.Current.Returns(Enumerable.Empty<SellableItemEntity>());
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(false);
        return cursor;
    }

    private static IAsyncCursor<SellableItemEntity> CreateCursorWithData(SellableItemEntity[] items)
    {
        var cursor = Substitute.For<IAsyncCursor<SellableItemEntity>>();
        var hasData = true;
        cursor.Current.Returns(items);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(ci =>
        {
            if (hasData)
            {
                hasData = false;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        });
        return cursor;
    }
}
