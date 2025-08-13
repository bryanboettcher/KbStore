namespace KbStore.Catalog.Tests.Products;

using Abstractions.Contracts;
using Abstractions.Exceptions;
using NUnit.Framework;
using Services;
using Shouldly;


[Category("Products")]
[Category("Integration")]
public abstract class UpdateLeadTime_Tests : ProductService_Tests<MassTransitProductCommandService>
{
    protected ProductModel? Result;

    protected override void Arrange()
    {
        TestLeadTime = TimeSpan.FromDays(14);
    }

    protected override async Task Act()
    {
        await CreateExistingProduct();

        Now = Later;
        Result ??= await Subject.UpdateLeadTimeAsync(TestId, TestLeadTime);
    }

    public class When_updating_successfully : UpdateLeadTime_Tests
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
        public void It_should_have_correct_lead_time()
            => Result!.LeadTime.ShouldBe(TimeSpan.FromDays(14));

        [Test]
        public void It_should_have_updated_on_timestamp()
            => Result!.UpdatedOn.ShouldBe(Now, TimeSpan.FromSeconds(0.25));

        [Test]
        public void It_should_have_unchanged_created_on_timestamp()
            => Result!.CreatedOn.ShouldBe(InitialCreatedOn);

        [Test]
        public async Task It_should_publish_product_lead_time_updated_event()
            => (await Harness!.Published.Any<ProductLeadTimeUpdated>()).ShouldBeTrue();
    }

    public class When_lead_time_is_negative : UpdateLeadTime_Tests
    {
        protected override void Arrange()
        {
            base.Arrange();
            TestLeadTime = TimeSpan.FromDays(-1);
        }

        [Test]
        public void It_should_throw_validation_exception()
            => LastException.ShouldBeOfType<ProductValidationException>();

        [Test]
        public void It_should_have_correct_error_message()
            => LastException!.Message.ShouldContain("Lead time");

        [Test]
        public async Task It_should_not_publish_product_lead_time_updated_event()
            => (await Harness!.Published.Any<ProductLeadTimeUpdated>()).ShouldBeFalse();
    }
}