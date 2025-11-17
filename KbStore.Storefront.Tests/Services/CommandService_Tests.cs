using KbStore.Tests;
// ReSharper disable StaticMemberInGenericType

namespace KbStore.Storefront.Tests.Services;

using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;


[TestFixture]
public abstract class CommandService_Tests<TService> : EventingTestBase
    where TService : class
{
    protected static readonly Guid ExistingId = Guid.Parse("abcd1234-bbbb-cccc-dddd-deadbeef0001");
    protected static readonly Guid LinkedId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-01234567dcba");

    protected static readonly DateTimeOffset Now = new(2025, 06, 16, 13, 30, 00, TimeSpan.Zero);
    protected static readonly DateTimeOffset Later = new(2025, 06, 16, 14, 00, 00, TimeSpan.Zero);

    protected TService Subject = null!;

    protected override void OnServicesCreating(IServiceCollection services)
    {
        base.OnServicesCreating(services);

        services.AddTransient<TService>();
        services.AddTransient<Func<DateTimeOffset>>(sp => () => Now);
    }

    protected override void Arrange()
    {
        Subject = ScopedProvider.ServiceProvider.GetRequiredService<TService>();
    }
}
