namespace KbStore.ApiService.Tests.Api.Endpoints.Catalog.Product;

using ApiService.Endpoints.Catalog;
using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Catalog.Abstractions.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore.Design.Internal;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shouldly;


public abstract class UpdateLeadTime_Tests : ProductEndpoints_Tests
{
    protected static readonly Guid TestId = Guid.NewGuid();
    protected static readonly TimeSpan? ValidLeadTime = TimeSpan.FromDays(14);

    public class When_updating_lead_time_successfully : UpdateLeadTime_Tests
    {
        protected override void Arrange()
        {
            MockOf<IProductCommandService>()
                .UpdateLeadTimeAsync(Arg.Any<Guid>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
                .Returns(new TestProductModel
                {
                    ProductId = TestId,
                    Sku = "TEST_SKU_123",
                    Name = "Test Product",
                    LeadTime = ValidLeadTime,
                    IsStocked = true,
                    IsEnabled = true,
                    IsAvailable = true
                });
        }

        protected override async Task Act()
            => Output = await Execute(ProductEndpoints.UpdateLeadTime, TestId, ValidLeadTime);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<ProductModel>>();
    }

    public class When_service_throws_exception : UpdateLeadTime_Tests
    {
        protected override void Arrange()
        {
            MockOf<IProductCommandService>()
                .UpdateLeadTimeAsync(Arg.Any<Guid>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
                .Throws(new TestProductException("Test error"));
        }

        protected override async Task Act()
            => Output = await Execute(ProductEndpoints.UpdateLeadTime, TestId, ValidLeadTime);

        [Test]
        public void It_should_throw() => LastException.ShouldBeOfType<TestProductException>();

        [Test]
        public void It_should_have_correct_message() => LastException!.Message.ShouldBe("Test error");
    }
}