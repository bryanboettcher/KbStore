namespace KbStore.Catalog.Tests.Products;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using NUnit.Framework;
using Services;
using Shouldly;


[Category("Products")]
[Category("Integration")]
public abstract class Disable_Tests : ProductService_Tests<MassTransitProductCommandService>
{
    protected ProductModel? Result;

    protected override void Arrange() { }

    protected override async Task Act()
    {
        await CreateExistingProduct();

        Now = Later;
        Result ??= await Subject.DisableAsync(TestId);
    }

    public class When_disabling_successfully : Disable_Tests
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
        public async Task It_should_publish_product_disabled_event()
            => (await Harness!.Published.Any<ProductDisabled>()).ShouldBeTrue();
    }

    public class When_product_already_disabled : Disable_Tests
    {
        protected override async Task Act()
        {
            await CreateExistingProduct();
            await Subject.DisableAsync(TestId);

            // Try to disable again
            await base.Act();
        }

        [Test]
        public void It_should_throw_state_exception()
            => LastException.ShouldBeOfType<ProductStateException>();

        [Test]
        public void It_should_have_correct_error_message()
            => LastException!.Message.ShouldContain("Cannot perform 'Disable'");

        [Test]
        public void It_should_not_publish_additional_product_disabled_event()
            => Harness!.Published.Select<ProductDisabled>().Count().ShouldBe(1);
    }
}