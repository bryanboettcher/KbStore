namespace KbStore.Catalog.Tests.Products;

using Abstractions.Contracts;
using NUnit.Framework;
using Services;
using Shouldly;


[Category("Products")]
[Category("Integration")]
public abstract class Delete_Tests : Catalog_Tests<MassTransitProductCommandService>
{
    protected ProductModel? Result;

    protected override void Arrange() { }

    protected override async Task Act()
    {
        await CreateExistingProduct();

        Now = Later;
        Result ??= await Subject.DeleteAsync(ProductId);
    }

    public class When_deleting_successfully : Delete_Tests
    {
        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();
            Result.ShouldNotBeNull();
            Result.ProductId.ShouldBe(ProductId);
            Result.IsEnabled.ShouldBeFalse();
            Result.IsAvailable.ShouldBeFalse();
            Result.UpdatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));
            Result.CreatedOn.ShouldBe(InitialCreatedOn);

            (await Harness.Published.Any<ProductDiscontinued>()).ShouldBeTrue();
        });
    }

    public class When_deleting_discontinued_product : Delete_Tests
    {
        protected override async Task Act()
        {
            await CreateExistingProduct();
            await Subject.DeleteAsync(ProductId);

            // Delete again to finalize
            await base.Act();
        }

        [Test]
        public async Task It_should_be_correct() => await Assert.MultipleAsync(async () =>
        {
            LastException.ShouldBeNull();

            (await Harness.Published.Any<ProductDeleted>()).ShouldBeTrue();

            ProductSagaHarness.Sagas.Contains(ProductId).CurrentState.ShouldBe(2);
        });
    }
}