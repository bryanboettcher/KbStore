namespace KbStore.ApiService.Tests.Api.Endpoints.Inventory.Actions;

using ApiService.Endpoints.Inventory;
using Contracts.Domains;
using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using NUnit.Framework;
using Shouldly;


public abstract class UpdateStockItemDescription_Tests : Endpoint_Tests<UpdateStockItemDescriptionRequest>
{
    protected static readonly Guid TestId = Guid.NewGuid();
    protected static readonly string ValidDescription = "Updated description";

    public class When_updating_successfully : UpdateStockItemDescription_Tests
    {
        protected override void Arrange()
            => RespondWith<UpdateStockItemResponse>(new { });

        protected override async Task Act()
            => Output = await Execute(StockItemEndpoints.UpdateDescription, TestId, ValidDescription);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<UpdateStockItemResponse>>();
    }

    public class When_description_is_null : UpdateStockItemDescription_Tests
    {
        protected override void Arrange()
            => ResponseUnexpected();

        protected override async Task Act()
            => Output = await Execute(StockItemEndpoints.UpdateDescription, TestId, null);

        [Test]
        public void It_should_return_bad_request() => Output.ShouldBeOfType<BadRequest>();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();
    }

    public class When_description_is_empty : UpdateStockItemDescription_Tests
    {
        protected override void Arrange()
            => RespondWith<UpdateStockItemResponse>(new { });

        protected override async Task Act()
            => Output = await Execute(StockItemEndpoints.UpdateDescription, TestId, "");

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<UpdateStockItemResponse>>();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();
    }

    public class When_StockItem_is_missing : UpdateStockItemDescription_Tests
    {
        protected override void Arrange() =>
            RespondWith<StockItemFailure>(new
            {
                Message = "StockItem not found",
                FailureType = FailureType.Missing
            });

        protected override async Task Act()
            => Output = await Execute(StockItemEndpoints.UpdateDescription, TestId, ValidDescription);

        [Test]
        public void It_should_return_not_found() => Output.ShouldBeOfType<NotFound<StockItemFailure>>();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();
    }

    public class When_backend_returns_unexpected_response : UpdateStockItemDescription_Tests
    {
        public class UnexpectedResponse { }

        protected override void Arrange()
            => RespondWith<UnexpectedResponse>(new { });

        protected override async Task Act()
            => Output = await Execute(StockItemEndpoints.UpdateDescription, TestId, ValidDescription);

        [Test]
        public void It_should_throw_request_timeout_exception() => LastException.ShouldBeOfType<RequestTimeoutException>();

        [Test]
        public void Output_should_be_null() => Output.ShouldBeNull();
    }
}