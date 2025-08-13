namespace KbStore.Catalog.Tests.Products;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using KbStore.Catalog.Tests;
using NUnit.Framework;
using Services;
using Shouldly;


[Category("Products")]
[Category("Integration")]
public abstract class Enable_Tests : ProductService_Tests<MassTransitProductCommandService>
{
    protected ProductModel? Result = null!;

    protected override void Arrange() { }

    protected override async Task Act()
    {
        await CreateExistingProduct();

        // Disable first
        await Subject.DisableAsync(TestId);

        Now = Later;
        Result ??= await Subject.EnableAsync(TestId);
    }

    public class When_enabling_successfully : Enable_Tests
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
        public void It_should_have_enabled_status()
            => Result!.IsEnabled.ShouldBeTrue();

        [Test]
        public void It_should_be_available()
            => Result!.IsAvailable.ShouldBeTrue();

        [Test]
        public void It_should_have_updated_on_timestamp()
            => Result!.UpdatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));

        [Test]
        public void It_should_have_unchanged_created_on_timestamp()
            => Result!.CreatedOn.ShouldBe(InitialCreatedOn);

        [Test]
        public async Task It_should_publish_product_enabled_event()
            => (await Harness!.Published.Any<ProductEnabled>()).ShouldBeTrue();
    }

    public class When_product_already_enabled : Enable_Tests
    {
        protected override async Task Act()
        {
            await CreateExistingProduct();

            // Don't disable first - try to enable already enabled product
            await base.Act();
        }

        [Test]
        public void It_should_throw_state_exception()
            => LastException.ShouldBeOfType<ProductStateException>();

        [Test]
        public void It_should_have_correct_error_message()
            => LastException!.Message.ShouldContain("Cannot perform 'Enable'");

        [Test]
        public void It_should_not_publish_additional_product_enabled_event()
            => Harness!.Published.Select<ProductEnabled>().Count().ShouldBe(0);
    }
}