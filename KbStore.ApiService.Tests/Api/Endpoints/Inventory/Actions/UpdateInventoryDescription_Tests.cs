namespace KbStore.ApiService.Tests.Api.Endpoints.Inventory.Actions;

using KbStore.ApiService.Endpoints.Inventory;
using KbStore.ApiService.Tests.Api.Endpoints;
using KbStore.Contracts.Domains;
using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using NUnit.Framework;
using Shouldly;


public abstract class UpdateInventoryDescription_Tests : Endpoint_Tests<UpdateInventoryDescriptionRequest>
{
    protected static readonly Guid TestId = Guid.NewGuid();
    protected static readonly string ValidDescription = "Updated description";

    public class When_updating_successfully : UpdateInventoryDescription_Tests
    {
        protected override void Arrange()
            => RespondWith<UpdateInventoryResponse>(new { });

        protected override async Task Act()
            => Output = await Execute(InventoryEndpoints.UpdateDescription, TestId, ValidDescription);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<UpdateInventoryResponse>>();
    }

    public class When_description_is_null : UpdateInventoryDescription_Tests
    {
        protected override void Arrange()
            => ResponseUnexpected();

        protected override async Task Act()
            => Output = await Execute(InventoryEndpoints.UpdateDescription, TestId, null);

        [Test]
        public void It_should_return_bad_request() => Output.ShouldBeOfType<BadRequest>();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();
    }

    public class When_description_is_empty : UpdateInventoryDescription_Tests
    {
        protected override void Arrange()
            => RespondWith<UpdateInventoryResponse>(new { });

        protected override async Task Act()
            => Output = await Execute(InventoryEndpoints.UpdateDescription, TestId, "");

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<UpdateInventoryResponse>>();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();
    }

    public class When_inventory_is_missing : UpdateInventoryDescription_Tests
    {
        protected override void Arrange() =>
            RespondWith<InventoryFailure>(new
            {
                Message = "Inventory not found",
                FailureType = FailureType.Missing
            });

        protected override async Task Act()
            => Output = await Execute(InventoryEndpoints.UpdateDescription, TestId, ValidDescription);

        [Test]
        public void It_should_return_not_found() => Output.ShouldBeOfType<NotFound<InventoryFailure>>();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();
    }

    public class When_backend_returns_unexpected_response : UpdateInventoryDescription_Tests
    {
        public class UnexpectedResponse { }

        protected override void Arrange()
            => RespondWith<UnexpectedResponse>(new { });

        protected override async Task Act()
            => Output = await Execute(InventoryEndpoints.UpdateDescription, TestId, ValidDescription);

        [Test]
        public void It_should_throw_request_timeout_exception() => LastException.ShouldBeOfType<RequestTimeoutException>();

        [Test]
        public void Output_should_be_null() => Output.ShouldBeNull();
    }
}