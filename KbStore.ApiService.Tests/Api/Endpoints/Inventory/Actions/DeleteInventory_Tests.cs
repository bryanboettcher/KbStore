namespace KbStore.ApiService.Tests.Api.Endpoints.Inventory.Actions;

using KbStore.ApiService.Endpoints.Inventory;
using KbStore.ApiService.Tests.Api.Endpoints;
using KbStore.Contracts.Domains;
using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using NUnit.Framework;
using Shouldly;

#pragma warning disable CS8618
[Category("Unit")]
[Category("Inventory")]
[Category("Delete")]
public abstract class DeleteInventory_Tests : Endpoint_Tests<DeleteInventoryRequest>
{
    protected static readonly Guid TestId = Guid.NewGuid();

    public class When_deleting_successfully : DeleteInventory_Tests
    {
        protected override void Arrange()
            => RespondWith<DeleteInventoryResponse>(new { });

        protected override async Task Act()
            => Output = await Execute(InventoryEndpoints.Delete, TestId);

        [Test]
        public void It_should_return_something() => Output.ShouldNotBeNull();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<DeleteInventoryResponse>>();
    }

    public class When_inventory_is_missing : DeleteInventory_Tests
    {
        protected override void Arrange()
            => RespondWith<DeleteInventoryResponse>(new { });

        protected override async Task Act()
            => Output = await Execute(InventoryEndpoints.Delete, TestId);

        [Test]
        public void It_should_be_successful() => Output.ShouldBeOfType<Ok<DeleteInventoryResponse>>();

        [Test]
        public void It_should_not_throw() => LastException.ShouldBeNull();
    }

    public class When_backend_returns_unexpected_response : DeleteInventory_Tests
    {
        public class UnexpectedResponse { }

        protected override void Arrange()
            => RespondWith<UnexpectedResponse>(new { });

        protected override async Task Act()
            => Output = await Execute(InventoryEndpoints.Delete, TestId);

        [Test]
        public void It_should_throw_request_timeout_exception() => LastException.ShouldBeOfType<RequestTimeoutException>();

        [Test]
        public void Output_should_be_null() => Output.ShouldBeNull();
    }
}