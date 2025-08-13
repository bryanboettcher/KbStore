namespace KbStore.Catalog.Tests.Products;

using Abstractions.Contracts;
using NUnit.Framework;
using Services;
using Shouldly;


[Category("Products")]
[Category("Integration")]
public abstract class UpdateName_Tests : Catalog_Tests<MassTransitProductCommandService>
{
    protected ProductModel? Result;

    protected override void Arrange()
    {
        TestName = "Updated Product Name";
    }

    protected override async Task Act()
    {
        await CreateExistingProduct();

        Now = Later;
        Result ??= await Subject.UpdateNameAsync(TestId, TestName);
    }

    public class When_updating_successfully : UpdateName_Tests
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
        public void It_should_have_correct_name()
            => Result!.Name.ShouldBe("Updated Product Name");

        [Test]
        public void It_should_have_updated_on_timestamp()
            => Result!.UpdatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));

        [Test]
        public void It_should_have_unchanged_created_on_timestamp()
            => Result!.CreatedOn.ShouldBe(InitialCreatedOn);

        [Test]
        public async Task It_should_publish_product_name_updated_event()
            => (await Harness.Published.Any<ProductNameUpdated>()).ShouldBeTrue();
    }

    public class When_name_is_null : UpdateName_Tests
    {
        protected override void Arrange()
        {
            base.Arrange();
            TestName = null;
        }

        [Test]
        public void It_should_not_throw()
            => LastException.ShouldBeNull();

        [Test]
        public void It_should_accept_null_name()
            => Result!.Name.ShouldBeNull();

        [Test]
        public async Task It_should_publish_product_name_updated_event()
            => (await Harness.Published.Any<ProductNameUpdated>()).ShouldBeTrue();
    }
}