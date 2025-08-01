namespace KbStore.ApiService.Tests.Api.Endpoints.Inventory.Actions;

using Abstractions;
using ApiService.Endpoints.Inventory;
using KbStore.Inventory.Abstractions.Contracts;
using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using NUnit.Framework;
using Shouldly;


public abstract class UpdateStockItemQuantity_Tests : Endpoint_Tests<UpdateStockItemQuantityRequest>
{
    protected static readonly Guid TestId = Guid.NewGuid();
    protected static readonly int ValidQuantity = 500;

    public class When_updating_successfully : UpdateStockItemQuantity_Tests
    {
        protected override void Arrange()
            => RespondWith<UpdateStockItemResponse>(new { });

        protected override async Task Act()
            => Output = await Execute(StockItemEndpoints.UpdateQuantity, TestId, ValidQuantity);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<UpdateStockItemResponse>>();
    }

    public class When_quantity_is_negative : UpdateStockItemQuantity_Tests
    {
        protected override void Arrange()
            => ResponseUnexpected();

        protected override async Task Act()
            => Output = await Execute(StockItemEndpoints.UpdateQuantity, TestId, -1);

        [Test]
        public void It_should_return_bad_request() => Output.ShouldBeOfType<BadRequest>();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();
    }

    public class When_quantity_is_zero : UpdateStockItemQuantity_Tests
    {
        protected override void Arrange()
            => RespondWith<UpdateStockItemResponse>(new { });

        protected override async Task Act()
            => Output = await Execute(StockItemEndpoints.UpdateQuantity, TestId, 0);

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<UpdateStockItemResponse>>();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();
    }

    public class When_StockItem_is_missing : UpdateStockItemQuantity_Tests
    {
        protected override void Arrange() =>
            RespondWith<StockItemFailure>(new
            {
                Message = "StockItem not found",
                FailureType = FailureType.Missing
            });

        protected override async Task Act()
            => Output = await Execute(StockItemEndpoints.UpdateQuantity, TestId, ValidQuantity);

        [Test]
        public void It_should_return_not_found() => Output.ShouldBeOfType<NotFound<StockItemFailure>>();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();
    }

    public class When_backend_returns_unexpected_response : UpdateStockItemQuantity_Tests
    {
        public class UnexpectedResponse { }

        protected override void Arrange()
            => RespondWith<UnexpectedResponse>(new { });

        protected override async Task Act()
            => Output = await Execute(StockItemEndpoints.UpdateQuantity, TestId, ValidQuantity);

        [Test]
        public void It_should_throw_request_timeout_exception() => LastException.ShouldBeOfType<RequestTimeoutException>();

        [Test]
        public void Output_should_be_null() => Output.ShouldBeNull();
    }
}