namespace KbStore.Catalog.Tests.Products;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using Domains.Inventory;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Services;
using Shouldly;


[Category("Products")]
[Category("Integration")]
public abstract class CreateAsync_Tests : Catalog_Tests<MassTransitProductCommandService>
{
    protected ProductModel? Result;

    protected override void Arrange()
    {
        ProductSku = "TEST_SKU_123";
        ProductName = "Test Product";
        ProductDimensions = new ProductDimensions
        {
            Width = 10m,
            Height = 5m,
            Length = 15m,
            Weight = 2.5m
        };
        ProductStockThreshold = 10;
        ProductLeadTime = TimeSpan.FromDays(7);
    }

    protected override async Task Act()
    {
        Result ??= await Subject.CreateAsync(ProductSku, ProductName, ProductDimensions, InventoryId, ProductStockThreshold, ProductLeadTime);
    }
    
    public class When_creating_valid_standalone_product : CreateAsync_Tests
    {
        protected override void Arrange()
        {
            base.Arrange();
            InventoryId = null; // No inventory link
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            Result.ShouldNotBeNull();
            Result.Sku.ShouldBe("TEST_SKU_123");
            Result.Name.ShouldBe("Test Product");
            Result.IsEnabled.ShouldBeTrue();
            Result.IsAvailable.ShouldBeTrue();
            Result.CreatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));
            Result.UpdatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));

            (await Harness.Published.Any<ProductCreated>()).ShouldBeTrue();
        });
    }
    
    public abstract class When_creating_valid_inventory_product : CreateAsync_Tests
    {
        public class When_inventory_succeeds : When_creating_valid_inventory_product
        {
            protected override void Arrange()
            {
                base.Arrange();
                InventoryId = Guid.NewGuid();

                // Add pre-existing inventory saga instance
                Harness.AddSagaInstance<InventoryEntity>(InventoryId, entity =>
                {
                    entity.PartNumber = "INV_PART_123";
                    entity.Description = "Test Inventory";
                    entity.StockQuantity = 50;
                    entity.CurrentState = 3; // Available
                });
            }
            
            [Test]
            public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
            {
                LastException.ShouldBeNull();

                Result.ShouldNotBeNull();
                Result.ProductId.ShouldNotBe(Guid.Empty);
                Result.Sku.ShouldBe("TEST_SKU_123");
                Result.Name.ShouldBe("Test Product");
                Result.InventoryItemId.ShouldBe(InventoryId);
                Result.CreatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));
                Result.UpdatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));

                (await Harness.Published.Any<ProductCreated>()).ShouldBeTrue();

                await ProductSagaHarness.Exists(Result.ProductId, x => x.Enabled);
                
                ProductSagaHarness.Sagas.Contains(ProductId).ShouldSatisfyAllConditions(
                    entity => entity.IsEnabled.ShouldBeTrue(),
                    entity => entity.IsAvailable.ShouldBeTrue(),
                    entity => entity.StockQuantity.ShouldBe(50)
                );
            });
        }

        public class When_inventory_fails : When_creating_valid_inventory_product
        {
            protected override async Task Act()
            {
                // Use non-existent inventory ID to trigger failure path
                InventoryId = Guid.NewGuid();

                // Create product linked to non-existent inventory
                Result ??= await Subject.CreateAsync(ProductSku, ProductName, ProductDimensions, InventoryId, ProductStockThreshold, ProductLeadTime);
            }

            [Test]
            public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
            {
                LastException.ShouldBeNull();

                Result.ShouldNotBeNull();
                Result.ProductId.ShouldNotBe(Guid.Empty);
                Result.Sku.ShouldBe("TEST_SKU_123");
                Result.Name.ShouldBe("Test Product");
                Result.InventoryItemId.ShouldBe(InventoryId);
                Result.CreatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));
                Result.UpdatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));

                (await Harness.Published.Any<ProductCreated>()).ShouldBeTrue();

                await ProductSagaHarness.Exists(Result.ProductId);

                ProductSagaHarness.Sagas.Contains(ProductId).ShouldSatisfyAllConditions(
                    entity => entity.IsEnabled.ShouldBeFalse(),
                    entity => entity.IsAvailable.ShouldBeFalse(),
                    entity => entity.StockQuantity.ShouldBe(0),
                    entity => entity.InventoryId.ShouldBe(InventoryId)
                );
            });
        }

        public class When_creating_with_empty_sku : CreateAsync_Tests
        {
            protected override void Arrange()
            {
                base.Arrange();
                ProductSku = "";
            }

            [Test]
            public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
            {
                LastException.ShouldBeOfType<ProductValidationException>();
                LastException.Message.ShouldContain("SKU");

                (await Harness.Published.Any<ProductCreated>()).ShouldBeFalse();
            });
        }
        
        public class When_creating_with_null_sku : CreateAsync_Tests
        {
            protected override void Arrange()
            {
                base.Arrange();
                ProductSku = null!;
            }

            [Test]
            public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
            {
                LastException.ShouldBeOfType<ProductValidationException>();
                LastException.Message.ShouldContain("SKU");

                (await Harness.Published.Any<ProductCreated>()).ShouldBeFalse();
            });
        }
        
        public class When_creating_with_negative_stock_threshold : CreateAsync_Tests
        {
            protected override void Arrange()
            {
                base.Arrange();
                ProductStockThreshold = -5;
            }

            [Test]
            public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
            {
                LastException.ShouldBeOfType<ProductValidationException>();
                LastException.Message.ShouldContain("threshold");

                (await Harness.Published.Any<ProductCreated>()).ShouldBeFalse();
            });
        }

        public class When_creating_with_negative_lead_time : CreateAsync_Tests
        {
            protected override void Arrange()
            {
                base.Arrange();
                ProductLeadTime = TimeSpan.FromDays(-1);
            }

            [Test]
            public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
            {
                LastException.ShouldBeOfType<ProductValidationException>();
                LastException.Message.ShouldContain("Lead time");

                (await Harness.Published.Any<ProductCreated>()).ShouldBeFalse();
            });
        }

        public class When_creating_duplicate_sku : CreateAsync_Tests
        {
            protected override async Task Act()
            {
                // create our initial
                await Subject.CreateAsync(ProductSku, ProductName, ProductDimensions, InventoryId, ProductStockThreshold, ProductLeadTime);

                // create the duplicate
                await base.Act();
            }

            [Test]
            public void It_should_be_correct() => Assert.Multiple(() =>
            {
                LastException.ShouldNotBeNull();
                LastException.ShouldBeOfType<ProductConflictException>();
                LastException.Message.ShouldContain("TEST_SKU_123");

                Harness.Published.Select<ProductCreated>().Count().ShouldBe(1);
            });
        }

        public class When_linked_inventory_is_discontinued : CreateAsync_Tests
        {
            protected override async Task Act()
            {
                await base.Act();

                // Simulate inventory discontinued after creation
                await PublishInventoryEvent<InventoryDiscontinued>(new
                {
                    InventoryId,
                    StockQuantity = 0,
                    Status = InventoryStatus.Discontinued,
                    Timestamp = Later
                });
            }

            [Test]
            public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
            {
                LastException.ShouldBeNull();
                
                (await Harness.Published.Any<ProductDiscontinued>()).ShouldBeTrue();

                Result.ShouldNotBeNull();
                Result.ProductId.ShouldNotBe(Guid.Empty);

                await ProductSagaHarness.Exists(Result.ProductId, x => x.Disabled);

                ProductSagaHarness.Sagas.Contains(ProductId).ShouldSatisfyAllConditions(
                    entity => entity.IsEnabled.ShouldBeFalse(),
                    entity => entity.InventoryId.ShouldBe(InventoryId)
                );
            });
        }
    }
}