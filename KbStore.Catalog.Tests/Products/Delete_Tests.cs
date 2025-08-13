namespace KbStore.Catalog.Tests.Products;

using Abstractions.Contracts;
using KbStore.Catalog.Tests;
using NUnit.Framework;
using Services;
using Shouldly;


[Category("Products")]
[Category("Integration")]
public abstract class Delete_Tests : ProductService_Tests<MassTransitProductCommandService>
{
    protected ProductModel? Result = null!;

    protected override void Arrange() { }

    protected override async Task Act()
    {
        await CreateExistingProduct();

        Now = Later;
        Result ??= await Subject.DeleteAsync(TestId);
    }

    public class When_deleting_successfully : Delete_Tests
    {
        [Test]
        public void It_should_not_throw()
            => LastException.ShouldBeNull();

        [Test]
        public void It_should_return_something()
            => Result.ShouldNotBeNull();

        [Test]
        public void It_should_be_successful()
            => Result!.ProductId.ShouldBe(TestId);

        [Test]
        public void It_should_have_disabled_status()
            => Result!.IsEnabled.ShouldBeFalse();

        [Test]
        public void It_should_be_unavailable()
            => Result!.IsAvailable.ShouldBeFalse();

        [Test]
        public void It_should_have_updated_on_timestamp()
            => Result!.UpdatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));

        [Test]
        public void It_should_have_unchanged_created_on_timestamp()
            => Result!.CreatedOn.ShouldBe(InitialCreatedOn);

        [Test]
        public async Task It_should_publish_product_discontinued_event()
            => (await Harness!.Published.Any<ProductDiscontinued>()).ShouldBeTrue();
    }

    public class When_deleting_discontinued_product : Delete_Tests
    {
        protected override async Task Act()
        {
            await CreateExistingProduct();
            await Subject.DeleteAsync(TestId);

            // Delete again to finalize
            await base.Act();
        }

        [Test]
        public void It_should_not_throw()
            => LastException.ShouldBeNull();

        [Test]
        public async Task It_should_publish_product_deleted_event()
            => (await Harness!.Published.Any<ProductDeleted>()).ShouldBeTrue();

        [Test]
        public async Task It_should_finalize_saga()
            => (await SagaHarness!.Consumed.Any<ProductDeleted>()).ShouldBeTrue();
    }
}