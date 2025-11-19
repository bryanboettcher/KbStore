# Contract Testing: Practical Implementation Guide

Step-by-step implementation guide with complete code examples for KbStore/KbClient contract testing.

**Prerequisites**: Read CONTRACT-TESTING-STRATEGY.md for architecture overview.

---

## Table of Contents

1. [Setup Checklist](#setup-checklist)
2. [Backend Implementation](#backend-implementation)
3. [Frontend Implementation](#frontend-implementation)
4. [Common Patterns](#common-patterns)
5. [Troubleshooting](#troubleshooting)
6. [Best Practices](#best-practices)

---

## Setup Checklist

### Phase 1: Infrastructure (Week 1)

- [ ] **Backend**: Deploy Pact Broker
- [ ] **Backend**: Configure OpenAPI generation
- [ ] **Backend**: Install PactNet package
- [ ] **Frontend**: Install Pact dependencies
- [ ] **Frontend**: Install openapi-typescript
- [ ] **CI/CD**: Create GitHub Actions workflows
- [ ] **Team**: Schedule training session

### Phase 2: Proof of Concept (Week 2)

- [ ] **Backend**: Write first provider verification test
- [ ] **Backend**: Implement provider state middleware
- [ ] **Frontend**: Write first consumer test (simple GET)
- [ ] **Frontend**: Publish contract to broker
- [ ] **Backend**: Verify contract
- [ ] **Frontend**: Generate types from OpenAPI
- [ ] **Team**: Review POC results

### Phase 3: Production Readiness (Weeks 3-12)

- [ ] **Frontend**: Convert 50% of endpoints (weeks 3-6)
- [ ] **Frontend**: Convert remaining endpoints (weeks 7-10)
- [ ] **Backend**: Add provider states for all scenarios
- [ ] **CI/CD**: Enforce contract verification gates
- [ ] **Team**: Delete manual mocks
- [ ] **Documentation**: Finalize playbooks

---

## Backend Implementation

### Step 1: Install Dependencies

**File**: `/KbStore.ApiService.Tests/KbStore.ApiService.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="PactNet" Version="5.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.0" />
    <PackageReference Include="NUnit" Version="4.4.0" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.7.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\KbStore.ApiService\KbStore.ApiService.csproj" />
  </ItemGroup>

</Project>
```

```bash
cd KbStore.ApiService.Tests
dotnet restore
```

---

### Step 2: Enhance OpenAPI Configuration

**File**: `/KbStore.ApiService/Program.cs`

```csharp
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddApplicationServices();

builder.Services.AddProblemDetails();

// Enhanced OpenAPI configuration
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<OpenApiTransformer>();
});

var app = builder.Build();

app.UseExceptionHandler();

// Serve OpenAPI spec at /openapi/v1.json
app.MapOpenApi();

// Human-readable documentation at /scalar/v1
app.MapScalarApiReference();

// Provider state endpoint (Testing environment only)
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Testing")
{
    app.UseMiddleware<ProviderStateMiddleware>();
}

app.MapDefaultEndpoints();
app.MapApplicationEndpoints();

await app.RunAsync();

// Make Program accessible to tests
public partial class Program { }
```

**File**: `/KbStore.ApiService/OpenApiTransformer.cs`

```csharp
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace KbStore.ApiService;

public class OpenApiTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "KbStore API",
            Version = "v1",
            Description = "Backend API for KbStore e-commerce platform",
            Contact = new OpenApiContact
            {
                Name = "KbStore Engineering",
                Email = "api@kbstore.com",
                Url = new Uri("https://github.com/your-org/KbStore")
            },
            License = new OpenApiLicense
            {
                Name = "MIT",
                Url = new Uri("https://opensource.org/licenses/MIT")
            }
        };

        document.Servers = new List<OpenApiServer>
        {
            new() { Url = "https://api.kbstore.com", Description = "Production" },
            new() { Url = "https://api-staging.kbstore.com", Description = "Staging" },
            new() { Url = "http://localhost:5000", Description = "Local Development" }
        };

        // Add common response schemas
        if (!document.Components.Schemas.ContainsKey("ProblemDetails"))
        {
            document.Components.Schemas.Add("ProblemDetails", new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["type"] = new OpenApiSchema { Type = "string", Nullable = true },
                    ["title"] = new OpenApiSchema { Type = "string", Nullable = true },
                    ["status"] = new OpenApiSchema { Type = "integer", Nullable = true },
                    ["detail"] = new OpenApiSchema { Type = "string", Nullable = true },
                    ["instance"] = new OpenApiSchema { Type = "string", Nullable = true }
                }
            });
        }

        return Task.CompletedTask;
    }
}
```

---

### Step 3: Add Provider State Middleware

**File**: `/KbStore.ApiService/Middleware/ProviderStateMiddleware.cs`

```csharp
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace KbStore.ApiService.Middleware;

/// <summary>
/// Handles provider state setup during Pact contract verification.
/// Only active in Testing environment.
/// </summary>
public class ProviderStateMiddleware
{
    private const string ProviderStateEndpoint = "/pact-provider-states";
    private readonly RequestDelegate _next;
    private readonly IServiceProvider _serviceProvider;

    public ProviderStateMiddleware(
        RequestDelegate next,
        IServiceProvider serviceProvider)
    {
        _next = next;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments(ProviderStateEndpoint))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json";

        if (context.Request.Method.Equals(HttpMethods.Post, StringComparison.OrdinalIgnoreCase))
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            var providerState = JsonSerializer.Deserialize<ProviderStateRequest>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (providerState?.State != null)
            {
                await SetupProviderState(providerState);
            }

            await context.Response.WriteAsync(JsonSerializer.Serialize(new { result = "ok" }));
        }
        else
        {
            await context.Response.WriteAsync(string.Empty);
        }
    }

    private async Task SetupProviderState(ProviderStateRequest providerState)
    {
        using var scope = _serviceProvider.CreateScope();
        var stateHandler = scope.ServiceProvider.GetRequiredService<IProviderStateHandler>();

        await stateHandler.SetupAsync(providerState.State, providerState.Params);
    }
}

public class ProviderStateRequest
{
    public string State { get; set; } = string.Empty;
    public Dictionary<string, object>? Params { get; set; }
}
```

---

### Step 4: Implement Provider State Handler

**File**: `/KbStore.ApiService.Tests/Contract/IProviderStateHandler.cs`

```csharp
namespace KbStore.ApiService.Tests.Contract;

public interface IProviderStateHandler
{
    Task SetupAsync(string stateName, Dictionary<string, object>? parameters);
}
```

**File**: `/KbStore.ApiService.Tests/Contract/ProviderStateHandler.cs`

```csharp
using KbStore.Catalog.Abstractions.Services;
using KbStore.Catalog.Abstractions.Contracts;
using KbStore.Storefront.Abstractions.Interfaces;

namespace KbStore.ApiService.Tests.Contract;

public class ProviderStateHandler : IProviderStateHandler
{
    private readonly IProductCommandService _productService;
    private readonly ISellableItemCommandService _sellableItemService;

    public ProviderStateHandler(
        IProductCommandService productService,
        ISellableItemCommandService sellableItemService)
    {
        _productService = productService;
        _sellableItemService = sellableItemService;
    }

    public async Task SetupAsync(string stateName, Dictionary<string, object>? parameters)
    {
        switch (stateName)
        {
            // --- Product States ---

            case "products can be created":
                // No setup needed - creation is always available
                break;

            case "product exists":
                await SetupProductExists(parameters);
                break;

            case "product does not exist":
                await SetupProductDoesNotExist(parameters);
                break;

            case "multiple products exist":
                await SetupMultipleProductsExist(parameters);
                break;

            // --- SellableItem States ---

            case "sellable items can be created":
                // No setup needed
                break;

            case "sellable item exists":
                await SetupSellableItemExists(parameters);
                break;

            case "sellable item does not exist":
                await SetupSellableItemDoesNotExist(parameters);
                break;

            // --- Error States ---

            case "database connection fails":
                // Simulate error condition (for error scenario testing)
                throw new InvalidOperationException("Simulated database failure");

            default:
                throw new InvalidOperationException(
                    $"Unknown provider state: {stateName}. " +
                    $"Add handler in ProviderStateHandler.SetupAsync()"
                );
        }
    }

    private async Task SetupProductExists(Dictionary<string, object>? parameters)
    {
        var productId = GetGuid(parameters, "productId") ?? Guid.NewGuid();
        var sku = GetString(parameters, "sku") ?? "TEST-WIDGET-001";
        var name = GetString(parameters, "name") ?? "Test Widget";
        var quantity = GetInt(parameters, "quantity") ?? 100;

        // Create product if it doesn't exist
        try
        {
            await _productService.GetAsync(productId, CancellationToken.None);
        }
        catch
        {
            await _productService.CreateAsync(
                sku: sku,
                name: name,
                dimensions: null,
                quantity: quantity,
                inventoryId: null,
                stockThreshold: 10,
                leadTime: null,
                cancellationToken: CancellationToken.None
            );
        }
    }

    private async Task SetupProductDoesNotExist(Dictionary<string, object>? parameters)
    {
        var productId = GetGuid(parameters, "productId") ?? Guid.Empty;

        try
        {
            await _productService.DeleteAsync(productId, CancellationToken.None);
        }
        catch
        {
            // Already doesn't exist - OK
        }
    }

    private async Task SetupMultipleProductsExist(Dictionary<string, object>? parameters)
    {
        var count = GetInt(parameters, "count") ?? 5;

        for (int i = 0; i < count; i++)
        {
            await _productService.CreateAsync(
                sku: $"WIDGET-{i:D3}",
                name: $"Test Widget {i}",
                dimensions: null,
                quantity: 100 + i,
                inventoryId: null,
                stockThreshold: 10,
                leadTime: null,
                cancellationToken: CancellationToken.None
            );
        }
    }

    private async Task SetupSellableItemExists(Dictionary<string, object>? parameters)
    {
        var itemId = GetGuid(parameters, "sellableItemId") ?? Guid.NewGuid();
        var sku = GetString(parameters, "sku") ?? "SELLABLE-001";
        var name = GetString(parameters, "name") ?? "Test Sellable Item";
        var price = GetDecimal(parameters, "basePrice") ?? 9.99m;

        try
        {
            await _sellableItemService.GetByIdAsync(itemId, CancellationToken.None);
        }
        catch
        {
            await _sellableItemService.CreateAsync(
                sku: sku,
                name: name,
                description: "Test item for contract verification",
                basePrice: price,
                itemType: "Standard",
                payload: new Dictionary<string, object?>(),
                productId: null,
                cancellationToken: CancellationToken.None
            );
        }
    }

    private async Task SetupSellableItemDoesNotExist(Dictionary<string, object>? parameters)
    {
        var itemId = GetGuid(parameters, "sellableItemId") ?? Guid.Empty;

        try
        {
            await _sellableItemService.DeleteAsync(itemId, CancellationToken.None);
        }
        catch
        {
            // Already doesn't exist - OK
        }
    }

    // Helper methods to extract typed values from parameters dictionary
    private static Guid? GetGuid(Dictionary<string, object>? parameters, string key)
    {
        if (parameters == null || !parameters.TryGetValue(key, out var value))
            return null;

        return value switch
        {
            Guid guid => guid,
            string str when Guid.TryParse(str, out var parsed) => parsed,
            _ => null
        };
    }

    private static string? GetString(Dictionary<string, object>? parameters, string key)
    {
        if (parameters == null || !parameters.TryGetValue(key, out var value))
            return null;

        return value?.ToString();
    }

    private static int? GetInt(Dictionary<string, object>? parameters, string key)
    {
        if (parameters == null || !parameters.TryGetValue(key, out var value))
            return null;

        return value switch
        {
            int i => i,
            long l => (int)l,
            string str when int.TryParse(str, out var parsed) => parsed,
            _ => null
        };
    }

    private static decimal? GetDecimal(Dictionary<string, object>? parameters, string key)
    {
        if (parameters == null || !parameters.TryGetValue(key, out var value))
            return null;

        return value switch
        {
            decimal d => d,
            double db => (decimal)db,
            float f => (decimal)f,
            int i => i,
            string str when decimal.TryParse(str, out var parsed) => parsed,
            _ => null
        };
    }
}
```

---

### Step 5: Create Provider Verification Test

**File**: `/KbStore.ApiService.Tests/Contract/KbStoreProviderTests.cs`

```csharp
using NUnit.Framework;
using PactNet;
using PactNet.Infrastructure.Outputters;
using PactNet.Verifier;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace KbStore.ApiService.Tests.Contract;

[TestFixture]
public class KbStoreProviderTests
{
    private TestServer? _server;
    private readonly string _pactBrokerUrl;
    private readonly string? _pactBrokerToken;

    public KbStoreProviderTests()
    {
        _pactBrokerUrl = Environment.GetEnvironmentVariable("PACT_BROKER_URL")
            ?? "http://localhost:9292";
        _pactBrokerToken = Environment.GetEnvironmentVariable("PACT_BROKER_TOKEN");
    }

    [OneTimeSetUp]
    public void Setup()
    {
        var builder = new WebHostBuilder()
            .UseStartup<Program>()
            .UseEnvironment("Testing")
            .ConfigureTestServices(services =>
            {
                // Register provider state handler
                services.AddScoped<IProviderStateHandler, ProviderStateHandler>();

                // Use in-memory database for tests
                // (Assuming you have a method to configure this)
                // services.UseInMemoryDatabase();
            });

        _server = new TestServer(builder);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _server?.Dispose();
    }

    [Test]
    public void VerifyContractsWithKbClient()
    {
        var config = new PactVerifierConfig
        {
            Outputters = new List<IOutput>
            {
                new ConsoleOutput()
            },
            LogLevel = PactLogLevel.Information
        };

        var verifier = new PactVerifier(config);

        verifier
            .ServiceProvider("KbStore", _server!.BaseAddress)
            .WithPactBrokerSource(new Uri(_pactBrokerUrl), options =>
            {
                // Authentication
                if (!string.IsNullOrEmpty(_pactBrokerToken))
                {
                    options.TokenAuthentication(_pactBrokerToken);
                }

                // Specify which contracts to verify
                options.ConsumerVersionSelectors(
                    // Always verify main branch (production)
                    new ConsumerVersionSelector
                    {
                        Branch = "main",
                        Latest = true
                    },
                    // Verify deployed versions
                    new ConsumerVersionSelector
                    {
                        Deployed = true
                    },
                    // Verify develop branch
                    new ConsumerVersionSelector
                    {
                        Branch = "develop",
                        Latest = true
                    }
                );

                // Enable pending pacts (contracts not yet verified)
                options.EnablePending();

                // Include WIP (work-in-progress) pacts from last 7 days
                options.IncludeWipPactsSince(DateTime.UtcNow.AddDays(-7));

                // Publish verification results back to broker
                var providerVersion = GetProviderVersion();
                var providerBranch = GetBranch();

                if (!string.IsNullOrEmpty(providerVersion))
                {
                    options.PublishVerificationResults(
                        providerVersion: providerVersion,
                        providerBranch: providerBranch
                    );
                }
            })
            .WithProviderStateUrl(new Uri(_server.BaseAddress, "/pact-provider-states"))
            .Verify();
    }

    private static string? GetProviderVersion()
    {
        // Use git commit SHA as version
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse --short HEAD",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return string.IsNullOrEmpty(output) ? null : output;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetBranch()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse --abbrev-ref HEAD",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return string.IsNullOrEmpty(output) ? null : output;
        }
        catch
        {
            return null;
        }
    }
}

public class ConsoleOutput : IOutput
{
    public void WriteLine(string line)
    {
        TestContext.Progress.WriteLine(line);
    }
}
```

---

### Step 6: Run Provider Verification

```bash
# Set environment variables
export PACT_BROKER_URL="http://localhost:9292"
export PACT_BROKER_TOKEN="your-token-here"

# Run verification test
cd KbStore.ApiService.Tests
dotnet test --filter "FullyQualifiedName~KbStoreProviderTests"
```

**Expected Output**:

```
Verifying a pact between KbClient and KbStore

  Given product exists
  a request to get product by ID
    with GET /products/f8e7d6c5-b4a3-9281-7069-584736251abc
      returns a response which
        has status code 200 (OK)
        includes headers
          "Content-Type" with value "application/json" (OK)
        has a matching body (OK)

  Given products can be created
  a request to create a product
    with POST /products
      returns a response which
        has status code 200 (OK)
        has a matching body (OK)

✅ All interactions verified successfully
```

---

## Frontend Implementation

### Step 1: Install Dependencies

**File**: `/KbClient/package.json`

```json
{
  "name": "kbclient",
  "version": "1.0.0",
  "scripts": {
    "dev": "vite",
    "build": "tsc && vite build",
    "test": "vitest",
    "test:contract": "vitest run --config vitest.contract.config.ts",
    "generate:types": "openapi-typescript http://localhost:5000/openapi/v1.json -o src/types/api.d.ts",
    "pact:publish": "node scripts/publish-pacts.js"
  },
  "dependencies": {
    "react": "^18.2.0",
    "axios": "^1.6.0"
  },
  "devDependencies": {
    "@pact-foundation/pact": "^13.1.0",
    "@types/react": "^18.2.0",
    "openapi-typescript": "^7.0.0",
    "typescript": "^5.3.0",
    "vite": "^5.0.0",
    "vitest": "^1.0.0"
  }
}
```

```bash
cd KbClient
npm install
```

---

### Step 2: Configure Pact Test Setup

**File**: `/KbClient/tests/contract/setup.ts`

```typescript
import { Pact } from '@pact-foundation/pact';
import path from 'path';

export const pactProvider = new Pact({
  consumer: 'KbClient',
  provider: 'KbStore',
  port: 9000, // Mock server port (avoid conflicts)
  log: path.resolve(process.cwd(), 'tests/contract/logs', 'pact.log'),
  dir: path.resolve(process.cwd(), 'tests/contract/pacts'),
  logLevel: 'info',
  spec: 3, // Pact specification version
});

export async function setupPact() {
  console.log('Starting Pact mock server...');
  await pactProvider.setup();
  console.log('Pact mock server running on http://localhost:9000');
}

export async function teardownPact() {
  console.log('Finalizing Pact contracts...');
  await pactProvider.finalize();
  console.log('Pact contracts written to tests/contract/pacts/');
}

export async function verifyPact() {
  await pactProvider.verify();
}
```

**File**: `/KbClient/vitest.contract.config.ts`

```typescript
import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    include: ['tests/contract/**/*.pact.test.ts'],
    globals: true,
    environment: 'node',
    setupFiles: ['tests/contract/setup.ts'],
  },
});
```

---

### Step 3: Write Consumer Contract Tests

**File**: `/KbClient/tests/contract/products.pact.test.ts`

```typescript
import { describe, it, beforeAll, afterAll, afterEach, expect } from 'vitest';
import { pactProvider, setupPact, teardownPact, verifyPact } from './setup';
import { Matchers } from '@pact-foundation/pact';
import { ProductService } from '@/services/ProductService';
import type { components } from '@/types/api';

const { like, uuid, iso8601DateTime, integer, boolean } = Matchers;

type ProductModel = components['schemas']['ProductModel'];
type CreateProductPayload = components['schemas']['CreateProductPayload'];

describe('Product API Contract', () => {
  const productService = new ProductService({
    baseURL: 'http://localhost:9000', // Pact mock server
  });

  beforeAll(setupPact);
  afterAll(teardownPact);
  afterEach(verifyPact);

  describe('POST /products', () => {
    it('creates a product successfully', async () => {
      const requestPayload: CreateProductPayload = {
        sku: 'WIDGET-001',
        name: 'Test Widget',
        quantity: 100,
        stockThreshold: 10,
        leadTime: 'P7D', // ISO 8601 duration (7 days)
      };

      await pactProvider.addInteraction({
        state: 'products can be created',
        uponReceiving: 'a request to create a product',
        withRequest: {
          method: 'POST',
          path: '/products',
          headers: {
            'Content-Type': 'application/json',
          },
          body: requestPayload,
        },
        willRespondWith: {
          status: 200,
          headers: {
            'Content-Type': 'application/json',
          },
          body: {
            productId: uuid(),
            sku: like('WIDGET-001'),
            name: like('Test Widget'),
            quantity: integer(100),
            stockThreshold: integer(10),
            leadTime: like('P7D'),
            isStocked: boolean(true),
            isEnabled: boolean(true),
            isAvailable: boolean(true),
            createdOn: iso8601DateTime(),
            updatedOn: iso8601DateTime(),
          },
        },
      });

      const result = await productService.create(requestPayload);

      expect(result.sku).toBe('WIDGET-001');
      expect(result.productId).toBeDefined();
      expect(result.isEnabled).toBe(true);
    });

    it('returns 400 for invalid product data', async () => {
      const invalidPayload = {
        sku: '', // Invalid - empty SKU
        name: 'Test Widget',
        quantity: 100,
      };

      await pactProvider.addInteraction({
        state: 'products can be created',
        uponReceiving: 'a request to create product with invalid SKU',
        withRequest: {
          method: 'POST',
          path: '/products',
          headers: {
            'Content-Type': 'application/json',
          },
          body: invalidPayload,
        },
        willRespondWith: {
          status: 400,
          headers: {
            'Content-Type': 'application/problem+json',
          },
          body: {
            type: like('https://tools.ietf.org/html/rfc7231#section-6.5.1'),
            title: like('Bad Request'),
            status: 400,
          },
        },
      });

      await expect(productService.create(invalidPayload as any)).rejects.toThrow();
    });
  });

  describe('GET /products/{id}', () => {
    it('retrieves a product by ID', async () => {
      const productId = 'f8e7d6c5-b4a3-9281-7069-584736251abc';

      await pactProvider.addInteraction({
        state: 'product exists',
        uponReceiving: 'a request to get product by ID',
        withRequest: {
          method: 'GET',
          path: `/products/${productId}`,
        },
        willRespondWith: {
          status: 200,
          headers: {
            'Content-Type': 'application/json',
          },
          body: {
            productId: uuid(productId),
            sku: like('WIDGET-001'),
            name: like('Test Widget'),
            quantity: integer(100),
            isStocked: boolean(true),
            isEnabled: boolean(true),
            isAvailable: boolean(true),
            createdOn: iso8601DateTime(),
            updatedOn: iso8601DateTime(),
          },
        },
      });

      const result = await productService.getById(productId);

      expect(result.productId).toBe(productId);
      expect(result.sku).toBeDefined();
    });

    it('returns 404 for non-existent product', async () => {
      const nonExistentId = '00000000-0000-0000-0000-000000000000';

      await pactProvider.addInteraction({
        state: 'product does not exist',
        uponReceiving: 'a request to get non-existent product',
        withRequest: {
          method: 'GET',
          path: `/products/${nonExistentId}`,
        },
        willRespondWith: {
          status: 404,
          headers: {
            'Content-Type': 'application/problem+json',
          },
          body: {
            type: like('https://tools.ietf.org/html/rfc7231#section-6.5.4'),
            title: like('Not Found'),
            status: 404,
          },
        },
      });

      await expect(productService.getById(nonExistentId)).rejects.toThrow();
    });
  });

  describe('PATCH /products/{id}/name', () => {
    it('updates product name', async () => {
      const productId = 'f8e7d6c5-b4a3-9281-7069-584736251abc';
      const newName = 'Updated Widget Name';

      await pactProvider.addInteraction({
        state: 'product exists',
        uponReceiving: 'a request to update product name',
        withRequest: {
          method: 'PATCH',
          path: `/products/${productId}/name`,
          headers: {
            'Content-Type': 'application/json',
          },
          body: newName,
        },
        willRespondWith: {
          status: 200,
          headers: {
            'Content-Type': 'application/json',
          },
          body: {
            productId: uuid(productId),
            name: like(newName),
            // ... other fields
            updatedOn: iso8601DateTime(),
          },
        },
      });

      const result = await productService.updateName(productId, newName);

      expect(result.name).toBe(newName);
    });
  });

  describe('DELETE /products/{id}', () => {
    it('deletes a product', async () => {
      const productId = 'f8e7d6c5-b4a3-9281-7069-584736251abc';

      await pactProvider.addInteraction({
        state: 'product exists',
        uponReceiving: 'a request to delete product',
        withRequest: {
          method: 'DELETE',
          path: `/products/${productId}`,
        },
        willRespondWith: {
          status: 200,
          headers: {
            'Content-Type': 'application/json',
          },
          body: {
            productId: uuid(productId),
            // Product model after deletion
          },
        },
      });

      const result = await productService.delete(productId);

      expect(result.productId).toBe(productId);
    });
  });
});
```

---

### Step 4: Generate TypeScript Types

```bash
# Start backend API
cd KbStore.ApiService
dotnet run

# In another terminal, generate types
cd KbClient
npm run generate:types
```

**Generated File**: `/KbClient/src/types/api.d.ts` (auto-generated, do not edit)

---

### Step 5: Implement Service Layer with Types

**File**: `/KbClient/src/services/ProductService.ts`

```typescript
import axios, { AxiosInstance } from 'axios';
import type { components } from '@/types/api';

type ProductModel = components['schemas']['ProductModel'];
type CreateProductPayload = components['schemas']['CreateProductPayload'];
type ProductDimensions = components['schemas']['ProductDimensions'];

export class ProductService {
  private readonly client: AxiosInstance;

  constructor(config: { baseURL: string }) {
    this.client = axios.create({
      baseURL: config.baseURL,
      headers: {
        'Content-Type': 'application/json',
      },
    });
  }

  async create(payload: CreateProductPayload): Promise<ProductModel> {
    const response = await this.client.post<ProductModel>('/products', payload);
    return response.data;
  }

  async getById(id: string): Promise<ProductModel> {
    const response = await this.client.get<ProductModel>(`/products/${id}`);
    return response.data;
  }

  async getAll(): Promise<ProductModel[]> {
    const response = await this.client.get<ProductModel[]>('/products');
    return response.data;
  }

  async updateName(id: string, name: string): Promise<ProductModel> {
    const response = await this.client.patch<ProductModel>(
      `/products/${id}/name`,
      JSON.stringify(name), // Backend expects raw JSON string
      { headers: { 'Content-Type': 'application/json' } }
    );
    return response.data;
  }

  async updateDimensions(
    id: string,
    dimensions: ProductDimensions | null
  ): Promise<ProductModel> {
    const response = await this.client.patch<ProductModel>(
      `/products/${id}/dimensions`,
      dimensions
    );
    return response.data;
  }

  async updateQuantity(id: string, quantity: number): Promise<ProductModel> {
    const response = await this.client.patch<ProductModel>(
      `/products/${id}/quantity`,
      quantity
    );
    return response.data;
  }

  async enable(id: string): Promise<ProductModel> {
    const response = await this.client.patch<ProductModel>(
      `/products/${id}/enable`,
      null
    );
    return response.data;
  }

  async disable(id: string): Promise<ProductModel> {
    const response = await this.client.patch<ProductModel>(
      `/products/${id}/disable`,
      null
    );
    return response.data;
  }

  async delete(id: string): Promise<ProductModel> {
    const response = await this.client.delete<ProductModel>(`/products/${id}`);
    return response.data;
  }
}
```

---

### Step 6: Run Consumer Tests & Publish Contracts

```bash
# Run contract tests
npm run test:contract

# Contracts generated in: tests/contract/pacts/KbClient-KbStore.json

# Publish to Pact Broker
npm run pact:publish
```

**File**: `/KbClient/scripts/publish-pacts.js`

```javascript
const { publisher } = require('@pact-foundation/pact-node');
const { execSync } = require('child_process');

const pactBrokerUrl = process.env.PACT_BROKER_URL || 'http://localhost:9292';
const pactBrokerToken = process.env.PACT_BROKER_TOKEN;
const gitCommit = execSync('git rev-parse --short HEAD').toString().trim();
const gitBranch = execSync('git rev-parse --abbrev-ref HEAD').toString().trim();

const opts = {
  pactFilesOrDirs: ['tests/contract/pacts'],
  pactBroker: pactBrokerUrl,
  pactBrokerToken: pactBrokerToken,
  consumerVersion: gitCommit,
  branch: gitBranch,
  tags: gitBranch === 'main' ? ['main', 'prod'] : [gitBranch],
};

publisher(opts)
  .then(() => {
    console.log(`✅ Contracts published successfully for KbClient@${gitCommit}`);
  })
  .catch((error) => {
    console.error('❌ Failed to publish contracts:', error);
    process.exit(1);
  });
```

---

## Common Patterns

### Pattern 1: Testing Pagination

```typescript
describe('GET /products (paginated)', () => {
  it('returns paginated list of products', async () => {
    await pactProvider.addInteraction({
      state: 'multiple products exist',
      uponReceiving: 'a request for paginated products',
      withRequest: {
        method: 'GET',
        path: '/products',
        query: {
          page: '1',
          pageSize: '10',
        },
      },
      willRespondWith: {
        status: 200,
        body: {
          items: eachLike({
            productId: uuid(),
            sku: like('WIDGET-001'),
            name: like('Test Widget'),
          }),
          totalCount: integer(50),
          page: integer(1),
          pageSize: integer(10),
        },
      },
    });

    const result = await productService.getAll({ page: 1, pageSize: 10 });

    expect(result.items).toHaveLength(10);
    expect(result.totalCount).toBe(50);
  });
});
```

---

### Pattern 2: Testing Error Scenarios

```typescript
describe('Error Handling', () => {
  it('handles 409 conflict when duplicate SKU', async () => {
    await pactProvider.addInteraction({
      state: 'product with SKU WIDGET-001 exists',
      uponReceiving: 'a request to create duplicate product',
      withRequest: {
        method: 'POST',
        path: '/products',
        body: {
          sku: 'WIDGET-001',
          name: 'Duplicate Widget',
          quantity: 100,
        },
      },
      willRespondWith: {
        status: 409,
        headers: {
          'Content-Type': 'application/problem+json',
        },
        body: {
          type: like('https://tools.ietf.org/html/rfc7231#section-6.5.8'),
          title: like('Conflict'),
          status: 409,
          detail: like('Product with SKU WIDGET-001 already exists'),
        },
      },
    });

    await expect(
      productService.create({
        sku: 'WIDGET-001',
        name: 'Duplicate Widget',
        quantity: 100,
      })
    ).rejects.toMatchObject({
      response: {
        status: 409,
      },
    });
  });
});
```

---

### Pattern 3: Testing Optional Fields

```typescript
it('creates product with optional dimensions', async () => {
  await pactProvider.addInteraction({
    state: 'products can be created',
    uponReceiving: 'a request to create product with dimensions',
    withRequest: {
      method: 'POST',
      path: '/products',
      body: {
        sku: 'WIDGET-002',
        name: 'Widget with Dimensions',
        quantity: 100,
        dimensions: {
          width: decimal(10.5),
          length: decimal(20.0),
          height: decimal(5.0),
          weight: decimal(2.5),
        },
      },
    },
    willRespondWith: {
      status: 200,
      body: {
        productId: uuid(),
        sku: like('WIDGET-002'),
        dimensions: like({
          width: 10.5,
          length: 20.0,
          height: 5.0,
          weight: 2.5,
        }),
        // ... other fields
      },
    },
  });

  const result = await productService.create({
    sku: 'WIDGET-002',
    name: 'Widget with Dimensions',
    quantity: 100,
    dimensions: {
      width: 10.5,
      length: 20.0,
      height: 5.0,
      weight: 2.5,
    },
  });

  expect(result.dimensions).toBeDefined();
  expect(result.dimensions?.width).toBe(10.5);
});
```

---

## Troubleshooting

### Issue 1: Provider Verification Fails with "Provider state not found"

**Symptom**:
```
Error: Provider state 'product exists' was not found
```

**Solution**:
Add the missing provider state to `ProviderStateHandler.SetupAsync()`:

```csharp
case "product exists":
    await SetupProductExists(parameters);
    break;
```

---

### Issue 2: Type Mismatch in Contract

**Symptom**:
```
Expected field 'productId' to be string, got object
```

**Solution**:
Check that consumer test uses correct matcher:

```typescript
// ❌ Wrong
body: {
  productId: 'f8e7d6c5-b4a3-9281-7069-584736251abc',
}

// ✅ Correct
body: {
  productId: uuid('f8e7d6c5-b4a3-9281-7069-584736251abc'),
}
```

---

### Issue 3: Pact Broker Connection Fails

**Symptom**:
```
Error: Unable to connect to Pact Broker at http://localhost:9292
```

**Solution**:
1. Verify Pact Broker is running:
   ```bash
   docker ps | grep pact-broker
   ```

2. Check environment variables:
   ```bash
   echo $PACT_BROKER_URL
   echo $PACT_BROKER_TOKEN
   ```

3. Test connection:
   ```bash
   curl -H "Authorization: Bearer $PACT_BROKER_TOKEN" $PACT_BROKER_URL
   ```

---

### Issue 4: Generated Types Don't Match API

**Symptom**:
TypeScript errors when using generated types.

**Solution**:
Regenerate types after backend changes:

```bash
# Pull latest backend changes
git pull

# Restart backend
cd KbStore.ApiService
dotnet run

# Regenerate types
cd ../KbClient
npm run generate:types

# Verify types
tsc --noEmit
```

---

## Best Practices

### 1. Provider State Naming

**Good**:
- `"product exists"` (simple, clear)
- `"product does not exist"` (explicit negative case)
- `"multiple products exist"` (plural indicates collection)

**Bad**:
- `"there is a product"` (verbose)
- `"setup product"` (ambiguous)
- `"productExists"` (camelCase - use natural language)

---

### 2. Contract Test Coverage

**Prioritize**:
1. Happy path scenarios (200 OK)
2. Common error cases (400, 404, 409)
3. Edge cases (empty lists, null fields)
4. Authentication/authorization (401, 403)

**Skip**:
- Internal server errors (500) - hard to reproduce
- Network timeouts - infrastructure concern
- Extreme edge cases - diminishing returns

---

### 3. Matcher Selection

**Use flexible matchers**:
```typescript
// ✅ Good - allows backend to change values
body: {
  productId: uuid(),
  name: like('Widget'),
  quantity: integer(100),
}
```

**Avoid exact matching unless necessary**:
```typescript
// ❌ Brittle - breaks if backend changes mock data
body: {
  productId: 'f8e7d6c5-b4a3-9281-7069-584736251abc',
  name: 'Test Widget',
  quantity: 100,
}
```

---

### 4. Contract Evolution

When making breaking changes:

1. **Add new field (non-breaking)**:
   - Backend: Add field to response
   - Frontend: Regenerate types, use new field
   - Contract: Auto-updates, backward compatible

2. **Remove field (breaking)**:
   - Option A: Deprecate first, remove later
   - Option B: Version API (`/v2/products`)
   - Option C: Coordinate deployment

3. **Rename field (breaking)**:
   - Add new field first
   - Populate both old and new
   - Frontend migrates to new field
   - Remove old field after grace period

---

### 5. Test Organization

**File structure**:
```
tests/contract/
├── setup.ts                      # Shared Pact configuration
├── products.pact.test.ts         # Product endpoints
├── sellableItems.pact.test.ts    # SellableItem endpoints
├── inventory.pact.test.ts        # Inventory endpoints
├── helpers/
│   ├── matchers.ts               # Custom matchers
│   └── fixtures.ts               # Test data
├── pacts/                        # Generated contracts (gitignored)
│   └── KbClient-KbStore.json
└── logs/                         # Logs (gitignored)
    └── pact.log
```

---

## Summary

This implementation guide provides:

1. **Complete setup steps** for backend (PactNet) and frontend (Pact JS)
2. **Working code examples** for provider states, verification, and consumer tests
3. **Common patterns** for pagination, errors, and optional fields
4. **Troubleshooting** for typical issues
5. **Best practices** for contract evolution and test organization

**Next Steps**:
1. Follow Phase 1 setup checklist
2. Implement proof-of-concept (1-2 endpoints)
3. Review with team
4. Begin incremental migration

**Related Documents**:
- CONTRACT-TESTING-STRATEGY.md (architecture overview)
- CONTRACT-TESTING-WORKFLOWS.md (workflow diagrams)

---

**Document Version**: 1.0
**Last Updated**: 2025-01-19
