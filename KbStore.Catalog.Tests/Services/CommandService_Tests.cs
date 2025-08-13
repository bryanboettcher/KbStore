using KbStore.Tests;
// ReSharper disable StaticMemberInGenericType

namespace KbStore.Catalog.Tests.Services;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using Catalog.Services;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public abstract class CommandService_Tests<TService> : EventingTestBase
    where TService : class
{
    protected static readonly Guid ExistingId = Guid.Parse("abcd1234-bbbb-cccc-dddd-deadbeef0001");
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

public class InventoryCommandService_Create : CommandService_Tests<MassTransitInventoryCommandService>
{
    protected InventoryModel? Result;

    protected int StockQuantity;
    protected string Description = null!;
    protected string PartNumber = null!;

    protected override void Arrange()
    {
        base.Arrange();

        PartNumber = "TEST_PART_123";
        Description = "Test Description";
        StockQuantity = 100;
    }

    protected override async Task Act()
    {
        Result = await Subject.CreateAsync(PartNumber, Description, StockQuantity);
    }
    
    public class When_creating_valid_inventory : InventoryCommandService_Create
    {
        protected override void OnHarnessCreating(IBusRegistrationConfigurator conf)
        {
            base.OnHarnessCreating(conf);

            conf.AddHandler<CreateInventoryRequest>(async context =>
            {
                await context.RespondAsync<CreateInventoryResponse>(new
                {
                    InventoryId = ExistingId,
                    PartNumber = "TEST_PART_123",
                });
            });
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Result.ShouldNotBeNull();
            Result.ShouldSatisfyAllConditions(x =>
            {
                x.PartNumber.ShouldBe("TEST_PART_123");
                x.InventoryId.ShouldBe(ExistingId);
            });

            (await Harness.Consumed.Any<CreateInventoryRequest>()).ShouldBeTrue();
        });
    }


    public class When_creating_inventory_invalid_quantity : InventoryCommandService_Create
    {
        protected override void Arrange()
        {
            base.Arrange();

            StockQuantity = -1;
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldNotBeNull();
            LastException.ShouldBeOfType<InventoryValidationException>();
            LastException.Message.ShouldContain("quantity");

            Result.ShouldBeNull();
            
            (await Harness.Consumed.Any<CreateInventoryRequest>()).ShouldBeFalse();
        });
    }
}