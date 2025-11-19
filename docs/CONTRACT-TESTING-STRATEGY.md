# Contract Testing Strategy for KbStore & KbClient

## Executive Summary

This document outlines a comprehensive consumer-driven contract testing strategy using Pact to prevent drift between the KbStore backend API and KbClient frontend application. The strategy serves as an automated quality gate integrated into CI/CD pipelines.

**Key Decision**: Generate TypeScript types/interfaces only (Approach B), allowing frontend to build their own service layer while maintaining strong contract guarantees.

---

## Table of Contents

1. [Pact Consumer-Driven Contract Testing Overview](#pact-consumer-driven-contract-testing-overview)
2. [Architecture & Integration Points](#architecture--integration-points)
3. [Approach Comparison: Full SDK vs Types Only](#approach-comparison-full-sdk-vs-types-only)
4. [Implementation Guide](#implementation-guide)
5. [CI/CD Integration Strategy](#cicd-integration-strategy)
6. [Developer Workflow](#developer-workflow)
7. [Migration Strategy](#migration-strategy)
8. [Versioning & Breaking Changes](#versioning--breaking-changes)

---

## Pact Consumer-Driven Contract Testing Overview

### What is Pact?

Pact is a contract testing framework that enables consumer-driven contract testing between services. Unlike traditional integration tests that require a running backend, Pact allows the frontend (consumer) to define expectations and the backend (provider) to verify it meets those expectations.

### Core Concepts

```mermaid
graph LR
    A[Frontend Consumer] -->|1. Defines Expectations| B[Pact Contract]
    B -->|2. Publishes to| C[Pact Broker]
    C -->|3. Retrieves| D[Backend Provider]
    D -->|4. Verifies Against| C
    C -->|5. Fails Build if| E[Contract Violated]
```

**Key Components:**

1. **Consumer (KbClient)**: Defines what it expects from the API
2. **Provider (KbStore)**: Verifies it can fulfill consumer expectations
3. **Pact Broker**: Central repository for contracts (versioned)
4. **Contract**: JSON file describing HTTP interactions (request/response pairs)

### Why Pact vs Traditional Testing?

| Aspect | Traditional Mocks | Pact Contracts |
|--------|------------------|----------------|
| **Drift Detection** | None - mocks diverge silently | Automatic - provider tests fail |
| **Documentation** | Scattered, often outdated | Self-documenting, always current |
| **Breaking Changes** | Discovered late (production) | Discovered early (CI build) |
| **Test Speed** | Fast (in-memory) | Fast (in-memory on consumer side) |
| **Backend Required** | No | No (for consumer tests) |
| **Contract Verification** | Manual | Automated |

---

## Architecture & Integration Points

### System Architecture

```mermaid
graph TB
    subgraph "KbClient Frontend"
        A[React Components] --> B[Service Layer]
        B --> C[Pact Consumer Tests]
        C --> D[Generated Types]
    end

    subgraph "Pact Broker"
        E[Contract Repository]
        F[Version Management]
        G[Compatibility Matrix]
    end

    subgraph "KbStore Backend"
        H[ASP.NET Minimal APIs] --> I[Pact Provider Tests]
        I --> J[OpenAPI Spec]
    end

    C -->|Publishes Contracts| E
    I -->|Verifies Against| E
    J -->|Generates| D

    style C fill:#e1f5ff
    style I fill:#ffe1e1
    style E fill:#e1ffe1
```

### Integration Points

#### 1. **KbClient Consumer Side**

**Location**: `KbClient/tests/contract/`

**Responsibilities**:
- Define expected API interactions via Pact tests
- Generate contract JSON files
- Publish contracts to Pact Broker
- Consume generated TypeScript types

**Tools**:
- `@pact-foundation/pact` (TypeScript/JavaScript)
- Jest or Vitest for test runner
- `openapi-typescript` for type generation

#### 2. **Pact Broker**

**Hosting Options**:
- **Self-hosted**: Docker container (PactFlow OSS)
- **Cloud**: PactFlow SaaS (paid, recommended for production)
- **CI Artifact Storage**: Temporary solution for small teams

**Configuration**:
- URL: `https://pact-broker.your-domain.com` (or localhost for dev)
- Authentication: API tokens for publish/retrieve
- Webhooks: Trigger provider verification on contract publish

#### 3. **KbStore Provider Side**

**Location**: `KbStore.ApiService.Tests/Contract/`

**Responsibilities**:
- Retrieve contracts from Pact Broker
- Verify API implementation meets all consumer expectations
- Report verification results back to broker
- Publish OpenAPI spec for type generation

**Tools**:
- `PactNet` (C# library for .NET)
- NUnit or xUnit for test runner
- `NSwag.MSBuild` or `Swashbuckle.AspNetCore` for OpenAPI generation

---

## Approach Comparison: Full SDK vs Types Only

### Approach A: Full Client SDK Generation

**What it provides**: Complete client library with pre-built HTTP methods.

**Example (openapi-generator)**:

```typescript
// Generated SDK
import { ProductsApi, SellableItemsApi } from '@kbstore/client-sdk';

const productsApi = new ProductsApi({ basePath: 'https://api.kbstore.com' });

// Usage - no manual fetch/axios
const product = await productsApi.createProduct({
  createProductPayload: {
    sku: 'WIDGET-001',
    name: 'Widget',
    quantity: 100,
    basePrice: 9.99
  }
});
```

**Pros**:
- Zero boilerplate - just call methods
- Automatic serialization/deserialization
- Built-in error handling patterns
- Consistent API across all endpoints
- Auto-generated documentation

**Cons**:
- Opinionated HTTP client (fetch/axios/xhr)
- Additional bundle size (~50-200KB)
- Less flexibility in request handling
- Harder to customize (retries, interceptors, auth)
- Regeneration required for any API change
- May not align with existing frontend patterns

### Approach B: Types/Interfaces Only (RECOMMENDED)

**What it provides**: TypeScript type definitions for requests, responses, and models.

**Example (openapi-typescript)**:

```typescript
// Generated types only
import type { paths, components } from '@kbstore/api-types';

type CreateProductPayload = components['schemas']['CreateProductPayload'];
type ProductModel = components['schemas']['ProductModel'];

// Your own service layer
export class ProductService {
  constructor(private httpClient: HttpClient) {}

  async createProduct(payload: CreateProductPayload): Promise<ProductModel> {
    return this.httpClient.post<ProductModel>('/products', payload);
  }
}
```

**Pros**:
- Minimal bundle impact (~5-10KB types)
- Full control over HTTP implementation
- Matches existing service layer patterns
- Easy to add middleware (auth, logging, retries)
- Type safety without runtime overhead
- Flexible error handling
- Works with any HTTP client

**Cons**:
- More boilerplate code initially
- Team must write service layer
- Requires disciplined typing

### Decision Matrix

| Criteria | Full SDK (A) | Types Only (B) | Winner |
|----------|--------------|----------------|--------|
| Bundle Size | 50-200KB | 5-10KB | **B** |
| Type Safety | ✅ | ✅ | Tie |
| Flexibility | ❌ | ✅ | **B** |
| Boilerplate | ✅ | ❌ | A |
| Custom Auth | ⚠️ | ✅ | **B** |
| Existing Patterns | ⚠️ | ✅ | **B** |
| Maintainability | ⚠️ | ✅ | **B** |

**Recommendation**: **Approach B (Types Only)**

**Rationale**:
1. Frontend already building service layer - SDK would be redundant
2. Need custom auth/retry/interceptor logic
3. Bundle size matters for performance
4. Types provide safety without dictating implementation
5. Easier to integrate with existing frontend architecture

---

## Implementation Guide

### Phase 1: Backend - OpenAPI Spec Generation

**Goal**: Produce accurate OpenAPI 3.0 spec from KbStore.ApiService.

#### Step 1.1: Enhance OpenAPI Configuration

**File**: `/KbStore.ApiService/Program.cs`

```csharp
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<OpenApiDocumentTransformer>();
});

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

// Add endpoint to serve spec as file
app.MapGet("/openapi/v1.json", async (IOpenApiDocumentService openApiService) =>
{
    var document = await openApiService.GetDocumentAsync("v1");
    return Results.Content(document, "application/json");
}).ExcludeFromDescription();

await app.RunAsync();
```

**File**: `/KbStore.ApiService/OpenApiDocumentTransformer.cs`

```csharp
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

public class OpenApiDocumentTransformer : IOpenApiDocumentTransformer
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
                Name = "KbStore Team",
                Email = "api@kbstore.com"
            }
        };

        document.Servers = new List<OpenApiServer>
        {
            new() { Url = "https://api.kbstore.com", Description = "Production" },
            new() { Url = "https://api-staging.kbstore.com", Description = "Staging" },
            new() { Url = "http://localhost:5000", Description = "Local Development" }
        };

        return Task.CompletedTask;
    }
}
```

#### Step 1.2: Add OpenAPI Annotations to Endpoints

**File**: `/KbStore.ApiService/Endpoints/Catalog/ProductEndpoints.cs`

```csharp
public static class ProductEndpoints
{
    public static void MapTo(WebApplication app)
    {
        var group = app.MapGroup("/products")
            .WithTags("Products")
            .WithOpenApi();

        group.MapPost("", Create)
            .WithName("CreateProduct")
            .WithSummary("Create a new product")
            .WithDescription("Creates a new product in the catalog with inventory tracking")
            .Produces<ProductModel>(200)
            .ProducesProblem(400)
            .ProducesProblem(409)
            .ProducesProblem(500);

        group.MapGet("{id:guid}", GetById)
            .WithName("GetProductById")
            .WithSummary("Retrieve product by ID")
            .Produces<ProductModel>(200)
            .ProducesProblem(404);

        // ... continue for all endpoints
    }
}
```

#### Step 1.3: Test OpenAPI Output

```bash
# Run API locally
dotnet run --project KbStore.ApiService

# Fetch OpenAPI spec
curl http://localhost:5000/openapi/v1.json > openapi.json

# Validate spec
npx @redocly/cli lint openapi.json
```

---

### Phase 2: Frontend - TypeScript Type Generation

**Goal**: Generate TypeScript types from OpenAPI spec.

#### Step 2.1: Add Type Generation Script

**File**: `/KbClient/package.json`

```json
{
  "scripts": {
    "generate:types": "openapi-typescript http://localhost:5000/openapi/v1.json -o src/types/api.d.ts",
    "generate:types:prod": "openapi-typescript https://api.kbstore.com/openapi/v1.json -o src/types/api.d.ts"
  },
  "devDependencies": {
    "openapi-typescript": "^7.0.0"
  }
}
```

#### Step 2.2: Generate Types

```bash
cd KbClient
npm run generate:types
```

**Generated Output**: `/KbClient/src/types/api.d.ts`

```typescript
// Auto-generated - DO NOT EDIT
export interface paths {
  "/products": {
    get: operations["GetAllProducts"];
    post: operations["CreateProduct"];
  };
  "/products/{id}": {
    get: operations["GetProductById"];
    delete: operations["DeleteProduct"];
  };
  // ... all other paths
}

export interface components {
  schemas: {
    ProductModel: {
      productId: string;
      sku: string;
      name?: string | null;
      dimensions?: ProductDimensions | null;
      quantity: number;
      inventoryId?: string | null;
      stockThreshold?: number | null;
      leadTime?: string | null; // ISO 8601 duration
      isStocked: boolean;
      isEnabled: boolean;
      isAvailable: boolean;
      createdOn: string; // ISO 8601 datetime
      updatedOn: string;
    };
    CreateProductPayload: {
      sku?: string | null;
      name?: string | null;
      dimensions?: ProductDimensions | null;
      quantity: number;
      inventoryId?: string | null;
      stockThreshold?: number | null;
      leadTime?: string | null;
    };
    ProductDimensions: {
      width?: number | null;
      length?: number | null;
      height?: number | null;
      weight?: number | null;
    };
    // ... all other schemas
  };
}

export interface operations {
  CreateProduct: {
    requestBody: {
      content: {
        "application/json": components["schemas"]["CreateProductPayload"];
      };
    };
    responses: {
      200: {
        content: {
          "application/json": components["schemas"]["ProductModel"];
        };
      };
      400: { /* ProblemDetails */ };
      409: { /* ProblemDetails */ };
      500: { /* ProblemDetails */ };
    };
  };
  // ... all other operations
}
```

#### Step 2.3: Use Types in Service Layer

**File**: `/KbClient/src/services/ProductService.ts`

```typescript
import type { components, operations } from '@/types/api';
import { httpClient } from '@/lib/httpClient';

type ProductModel = components['schemas']['ProductModel'];
type CreateProductPayload = components['schemas']['CreateProductPayload'];
type UpdateNamePayload = { name: string };

export class ProductService {
  private readonly basePath = '/products';

  async create(payload: CreateProductPayload): Promise<ProductModel> {
    const response = await httpClient.post<ProductModel>(
      this.basePath,
      payload
    );
    return response.data;
  }

  async getById(id: string): Promise<ProductModel> {
    const response = await httpClient.get<ProductModel>(
      `${this.basePath}/${id}`
    );
    return response.data;
  }

  async updateName(id: string, name: string): Promise<ProductModel> {
    const response = await httpClient.patch<ProductModel>(
      `${this.basePath}/${id}/name`,
      name, // Note: backend expects raw string, not JSON object
      { headers: { 'Content-Type': 'application/json' } }
    );
    return response.data;
  }

  // ... other methods
}
```

---

### Phase 3: Pact Broker Setup

**Goal**: Deploy Pact Broker for contract storage.

#### Option 1: Docker Compose (Development/Staging)

**File**: `/infrastructure/pact-broker/docker-compose.yml`

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: pact_broker
      POSTGRES_PASSWORD: ${PACT_DB_PASSWORD}
      POSTGRES_DB: pact_broker
    volumes:
      - pact-db:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U pact_broker"]
      interval: 10s
      timeout: 5s
      retries: 5

  pact-broker:
    image: pactfoundation/pact-broker:latest
    ports:
      - "9292:9292"
    environment:
      PACT_BROKER_DATABASE_URL: "postgres://pact_broker:${PACT_DB_PASSWORD}@postgres/pact_broker"
      PACT_BROKER_BASIC_AUTH_USERNAME: ${PACT_BROKER_USERNAME}
      PACT_BROKER_BASIC_AUTH_PASSWORD: ${PACT_BROKER_PASSWORD}
      PACT_BROKER_ALLOW_PUBLIC_READ: "false"
      PACT_BROKER_BASE_URL: "https://pact.kbstore.com"
    depends_on:
      postgres:
        condition: service_healthy

volumes:
  pact-db:
```

**Deploy**:

```bash
cd infrastructure/pact-broker
export PACT_DB_PASSWORD=$(openssl rand -base64 32)
export PACT_BROKER_USERNAME=kbstore
export PACT_BROKER_PASSWORD=$(openssl rand -base64 32)

docker-compose up -d

# Verify
curl -u kbstore:$PACT_BROKER_PASSWORD http://localhost:9292
```

#### Option 2: PactFlow SaaS (Production)

```bash
# Sign up at https://pactflow.io
# Get API token from account settings
export PACT_BROKER_BASE_URL="https://your-org.pactflow.io"
export PACT_BROKER_TOKEN="your-api-token-here"
```

---

### Phase 4: Consumer-Side Pact Tests (KbClient)

**Goal**: Define expected API interactions via Pact tests.

#### Step 4.1: Install Pact Dependencies

```bash
cd KbClient
npm install --save-dev @pact-foundation/pact
```

#### Step 4.2: Configure Pact Test Setup

**File**: `/KbClient/tests/contract/setup.ts`

```typescript
import { Pact } from '@pact-foundation/pact';
import path from 'path';

export const provider = new Pact({
  consumer: 'KbClient',
  provider: 'KbStore',
  port: 9000, // Mock server port
  log: path.resolve(process.cwd(), 'tests/contract/logs', 'pact.log'),
  dir: path.resolve(process.cwd(), 'tests/contract/pacts'),
  logLevel: 'info',
  spec: 3, // Pact specification version
});

export const setupPact = async () => {
  await provider.setup();
};

export const teardownPact = async () => {
  await provider.finalize();
};

export const verifyPact = async () => {
  await provider.verify();
};
```

#### Step 4.3: Write Consumer Contract Tests

**File**: `/KbClient/tests/contract/products.pact.test.ts`

```typescript
import { provider, setupPact, teardownPact, verifyPact } from './setup';
import { ProductService } from '@/services/ProductService';
import { Matchers } from '@pact-foundation/pact';
import type { components } from '@/types/api';

const { like, uuid, iso8601DateTime, integer, decimal, boolean } = Matchers;

describe('Product API Contract', () => {
  const productService = new ProductService({
    baseURL: 'http://localhost:9000', // Pact mock server
  });

  beforeAll(setupPact);
  afterAll(teardownPact);
  afterEach(verifyPact);

  describe('POST /products', () => {
    it('creates a product successfully', async () => {
      const requestPayload: components['schemas']['CreateProductPayload'] = {
        sku: 'WIDGET-001',
        name: 'Test Widget',
        quantity: 100,
        stockThreshold: 10,
      };

      const expectedResponse: components['schemas']['ProductModel'] = {
        productId: uuid('f8e7d6c5-b4a3-9281-7069-584736251abc'),
        sku: 'WIDGET-001',
        name: 'Test Widget',
        quantity: 100,
        stockThreshold: 10,
        isStocked: true,
        isEnabled: true,
        isAvailable: true,
        createdOn: iso8601DateTime('2025-01-01T00:00:00Z'),
        updatedOn: iso8601DateTime('2025-01-01T00:00:00Z'),
      };

      await provider.addInteraction({
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
          body: like(expectedResponse),
        },
      });

      const result = await productService.create(requestPayload);

      expect(result.sku).toBe('WIDGET-001');
      expect(result.productId).toBeDefined();
    });

    it('returns 400 for invalid SKU', async () => {
      const invalidPayload = {
        sku: '', // Invalid - empty SKU
        name: 'Test Widget',
        quantity: 100,
      };

      await provider.addInteraction({
        state: 'products require valid SKU',
        uponReceiving: 'a request to create product with empty SKU',
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

      await expect(productService.create(invalidPayload)).rejects.toThrow();
    });
  });

  describe('GET /products/{id}', () => {
    it('retrieves a product by ID', async () => {
      const productId = 'f8e7d6c5-b4a3-9281-7069-584736251abc';

      await provider.addInteraction({
        state: 'product with ID f8e7d6c5-b4a3-9281-7069-584736251abc exists',
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
    });

    it('returns 404 for non-existent product', async () => {
      const nonExistentId = '00000000-0000-0000-0000-000000000000';

      await provider.addInteraction({
        state: 'product with ID 00000000-0000-0000-0000-000000000000 does not exist',
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

  // Add tests for PATCH, DELETE, etc.
});
```

#### Step 4.4: Run Consumer Tests & Generate Contracts

```bash
cd KbClient
npm run test:contract

# This generates:
# tests/contract/pacts/KbClient-KbStore.json
```

**Generated Contract**: `/KbClient/tests/contract/pacts/KbClient-KbStore.json`

```json
{
  "consumer": {
    "name": "KbClient"
  },
  "provider": {
    "name": "KbStore"
  },
  "interactions": [
    {
      "description": "a request to create a product",
      "providerState": "products can be created",
      "request": {
        "method": "POST",
        "path": "/products",
        "headers": {
          "Content-Type": "application/json"
        },
        "body": {
          "sku": "WIDGET-001",
          "name": "Test Widget",
          "quantity": 100,
          "stockThreshold": 10
        }
      },
      "response": {
        "status": 200,
        "headers": {
          "Content-Type": "application/json"
        },
        "body": {
          "productId": "f8e7d6c5-b4a3-9281-7069-584736251abc",
          "sku": "WIDGET-001",
          "name": "Test Widget",
          "quantity": 100,
          "stockThreshold": 10,
          "isStocked": true,
          "isEnabled": true,
          "isAvailable": true,
          "createdOn": "2025-01-01T00:00:00Z",
          "updatedOn": "2025-01-01T00:00:00Z"
        }
      }
    }
  ],
  "metadata": {
    "pactSpecification": {
      "version": "3.0.0"
    }
  }
}
```

#### Step 4.5: Publish Contracts to Pact Broker

**File**: `/KbClient/scripts/publish-pacts.sh`

```bash
#!/bin/bash
set -e

PACT_BROKER_URL="${PACT_BROKER_URL:-http://localhost:9292}"
PACT_BROKER_TOKEN="${PACT_BROKER_TOKEN}"
CONSUMER_VERSION=$(git rev-parse --short HEAD)
BRANCH=$(git rev-parse --abbrev-ref HEAD)

npx pact-broker publish \
  tests/contract/pacts \
  --consumer-app-version="$CONSUMER_VERSION" \
  --branch="$BRANCH" \
  --broker-base-url="$PACT_BROKER_URL" \
  --broker-token="$PACT_BROKER_TOKEN"

echo "✅ Published contracts for KbClient@$CONSUMER_VERSION"
```

```bash
chmod +x scripts/publish-pacts.sh
./scripts/publish-pacts.sh
```

---

### Phase 5: Provider-Side Pact Verification (KbStore)

**Goal**: Verify backend implementation meets consumer contracts.

#### Step 5.1: Install PactNet

```bash
cd KbStore.ApiService.Tests
dotnet add package PactNet --version 5.0.0
```

#### Step 5.2: Create Provider State Setup

**File**: `/KbStore.ApiService.Tests/Contract/ProviderStates.cs`

```csharp
using KbStore.Catalog.Abstractions.Services;
using KbStore.Catalog.Abstractions.Contracts;

namespace KbStore.ApiService.Tests.Contract;

/// <summary>
/// Provider states define preconditions for contract verification.
/// Each state corresponds to a "providerState" in consumer contracts.
/// </summary>
public class ProviderStates
{
    private readonly IProductCommandService _productCommandService;

    public ProviderStates(IProductCommandService productCommandService)
    {
        _productCommandService = productCommandService;
    }

    /// <summary>
    /// Setup for: "products can be created"
    /// </summary>
    public async Task ProductsCanBeCreated()
    {
        // No precondition needed - creation is always available
        await Task.CompletedTask;
    }

    /// <summary>
    /// Setup for: "product with ID f8e7d6c5-b4a3-9281-7069-584736251abc exists"
    /// </summary>
    public async Task ProductExists(Guid productId)
    {
        // Seed the database with a known product
        await _productCommandService.CreateAsync(
            sku: "WIDGET-001",
            name: "Test Widget",
            dimensions: null,
            quantity: 100,
            inventoryId: null,
            stockThreshold: 10,
            leadTime: null,
            cancellationToken: CancellationToken.None
        );
    }

    /// <summary>
    /// Setup for: "product with ID 00000000-0000-0000-0000-000000000000 does not exist"
    /// </summary>
    public async Task ProductDoesNotExist(Guid productId)
    {
        // Ensure the product does NOT exist (cleanup if needed)
        try
        {
            await _productCommandService.DeleteAsync(productId, CancellationToken.None);
        }
        catch
        {
            // Already doesn't exist - that's fine
        }
    }
}
```

#### Step 5.3: Create Provider Verification Test

**File**: `/KbStore.ApiService.Tests/Contract/KbStoreProviderTests.cs`

```csharp
using NUnit.Framework;
using PactNet;
using PactNet.Infrastructure.Outputters;
using PactNet.Verifier;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace KbStore.ApiService.Tests.Contract;

[TestFixture]
public class KbStoreProviderTests
{
    private TestServer? _server;
    private string _pactBrokerUrl = Environment.GetEnvironmentVariable("PACT_BROKER_URL")
        ?? "http://localhost:9292";
    private string? _pactBrokerToken = Environment.GetEnvironmentVariable("PACT_BROKER_TOKEN");

    [OneTimeSetUp]
    public void Setup()
    {
        // Start the API in test mode
        var builder = new WebHostBuilder()
            .UseStartup<Program>()
            .UseEnvironment("Testing")
            .ConfigureTestServices(services =>
            {
                // Use in-memory database for contract tests
                services.AddSingleton<ProviderStates>();
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
                new XUnitOutput(TestContext.Progress)
            },
            LogLevel = PactLogLevel.Information
        };

        var verifier = new PactVerifier(config);

        verifier
            .ServiceProvider("KbStore", _server!.BaseAddress)
            .WithPactBrokerSource(new Uri(_pactBrokerUrl), options =>
            {
                if (!string.IsNullOrEmpty(_pactBrokerToken))
                {
                    options.TokenAuthentication(_pactBrokerToken);
                }
                options.ConsumerVersionSelectors(
                    // Verify against main branch
                    new ConsumerVersionSelector { Branch = "main" },
                    // Verify against deployed versions
                    new ConsumerVersionSelector { Deployed = true },
                    // Verify against latest on each branch
                    new ConsumerVersionSelector { LatestOnBranch = true }
                );
                options.EnablePending();
                options.IncludeWipPactsSince(DateTime.Now.AddDays(-7));
                options.PublishVerificationResults(
                    providerVersion: GetProviderVersion(),
                    providerBranch: GetBranch()
                );
            })
            .WithProviderStateUrl(new Uri(_server.BaseAddress, "/pact-provider-states"))
            .Verify();
    }

    private string GetProviderVersion()
    {
        // Use git commit SHA as version
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
        return process.StandardOutput.ReadToEnd().Trim();
    }

    private string GetBranch()
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
        return process.StandardOutput.ReadToEnd().Trim();
    }
}

public class XUnitOutput : IOutput
{
    private readonly TestContext.ProgressWriter _output;

    public XUnitOutput(TestContext.ProgressWriter output)
    {
        _output = output;
    }

    public void WriteLine(string line)
    {
        _output.WriteLine(line);
    }
}
```

#### Step 5.4: Create Provider State Middleware

**File**: `/KbStore.ApiService/Middleware/ProviderStateMiddleware.cs`

```csharp
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using KbStore.ApiService.Tests.Contract;

namespace KbStore.ApiService.Middleware;

/// <summary>
/// Middleware to handle provider state setup during Pact verification.
/// Only active in Testing environment.
/// </summary>
public class ProviderStateMiddleware
{
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
        if (!context.Request.Path.StartsWithSegments("/pact-provider-states"))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = 200;

        if (context.Request.Method == HttpMethods.Post)
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            var state = JsonSerializer.Deserialize<ProviderStateRequest>(body);

            if (state?.State != null)
            {
                await SetupProviderState(state.State, state.Params);
            }
        }

        await context.Response.WriteAsync(string.Empty);
    }

    private async Task SetupProviderState(string stateName, Dictionary<string, object>? parameters)
    {
        using var scope = _serviceProvider.CreateScope();
        var providerStates = scope.ServiceProvider.GetRequiredService<ProviderStates>();

        switch (stateName)
        {
            case "products can be created":
                await providerStates.ProductsCanBeCreated();
                break;

            case "product with ID f8e7d6c5-b4a3-9281-7069-584736251abc exists":
                await providerStates.ProductExists(
                    Guid.Parse("f8e7d6c5-b4a3-9281-7069-584736251abc")
                );
                break;

            case "product with ID 00000000-0000-0000-0000-000000000000 does not exist":
                await providerStates.ProductDoesNotExist(Guid.Empty);
                break;

            default:
                throw new InvalidOperationException(
                    $"Unknown provider state: {stateName}"
                );
        }
    }
}

public class ProviderStateRequest
{
    public string? State { get; set; }
    public Dictionary<string, object>? Params { get; set; }
}
```

**Register Middleware** (Testing environment only):

```csharp
// In Program.cs
if (app.Environment.IsTesting())
{
    app.UseMiddleware<ProviderStateMiddleware>();
}
```

#### Step 5.5: Run Provider Verification

```bash
cd KbStore.ApiService.Tests
dotnet test --filter "FullyQualifiedName~KbStoreProviderTests"
```

**Expected Output**:

```
Verifying a pact between KbClient and KbStore

  a request to create a product
    with POST /products
      returns a response which
        has status code 200 (OK)
        includes headers
          "Content-Type" with value "application/json" (OK)
        has a matching body (OK)

  a request to get product by ID
    with GET /products/f8e7d6c5-b4a3-9281-7069-584736251abc
      Given product with ID f8e7d6c5-b4a3-9281-7069-584736251abc exists
      returns a response which
        has status code 200 (OK)
        has a matching body (OK)

✅ 2 interactions, 0 failures
```

---

## CI/CD Integration Strategy

### Pipeline Architecture

```mermaid
graph TB
    subgraph "Frontend Pipeline (KbClient)"
        A1[Checkout Code] --> A2[Install Deps]
        A2 --> A3[Run Unit Tests]
        A3 --> A4[Run Pact Consumer Tests]
        A4 --> A5[Publish Contracts to Broker]
        A5 --> A6{Can Deploy?}
        A6 -->|Yes| A7[Deploy to Env]
        A6 -->|No| A8[Block Deploy]
        A7 --> A9[Record Deployment]
    end

    subgraph "Backend Pipeline (KbStore)"
        B1[Checkout Code] --> B2[Build Solution]
        B2 --> B3[Run Unit Tests]
        B3 --> B4[Start Test API]
        B4 --> B5[Run Pact Provider Tests]
        B5 --> B6[Publish Verification Results]
        B6 --> B7{All Contracts Valid?}
        B7 -->|Yes| B8[Deploy to Env]
        B7 -->|No| B9[Block Deploy]
        B8 --> B10[Record Deployment]
    end

    subgraph "Pact Broker"
        C1[Contract Storage]
        C2[Verification Matrix]
        C3[Deployment Records]
        C4[Webhook Triggers]
    end

    A5 -.->|Publish| C1
    C1 -.->|Trigger| C4
    C4 -.->|Start| B5
    B6 -.->|Results| C2
    A9 -.->|Record| C3
    B10 -.->|Record| C3
    C2 -.->|Check| A6

    style A6 fill:#ffe1e1
    style B7 fill:#ffe1e1
    style C2 fill:#e1ffe1
```

### GitHub Actions: Frontend

**File**: `/KbClient/.github/workflows/contract-tests.yml`

```yaml
name: Contract Tests (Consumer)

on:
  pull_request:
    branches: [main, develop]
  push:
    branches: [main, develop]

env:
  PACT_BROKER_URL: ${{ secrets.PACT_BROKER_URL }}
  PACT_BROKER_TOKEN: ${{ secrets.PACT_BROKER_TOKEN }}

jobs:
  contract-tests:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout code
        uses: actions/checkout@v4
        with:
          fetch-depth: 0 # Full history for versioning

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'

      - name: Install dependencies
        run: npm ci

      - name: Run Pact consumer tests
        run: npm run test:contract

      - name: Publish contracts to broker
        if: github.event_name == 'push'
        run: |
          npx pact-broker publish \
            tests/contract/pacts \
            --consumer-app-version="$GITHUB_SHA" \
            --branch="$GITHUB_REF_NAME" \
            --broker-base-url="$PACT_BROKER_URL" \
            --broker-token="$PACT_BROKER_TOKEN"

      - name: Can I deploy to production?
        if: github.ref == 'refs/heads/main'
        run: |
          npx pact-broker can-i-deploy \
            --pacticipant=KbClient \
            --version="$GITHUB_SHA" \
            --to-environment=production \
            --broker-base-url="$PACT_BROKER_URL" \
            --broker-token="$PACT_BROKER_TOKEN" \
            --retry-while-unknown=12 \
            --retry-interval=10

      - name: Upload pact files (artifact)
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: pact-contracts
          path: tests/contract/pacts/*.json
```

### GitHub Actions: Backend

**File**: `/KbStore/.github/workflows/contract-verification.yml`

```yaml
name: Contract Verification (Provider)

on:
  pull_request:
    branches: [main, develop]
  push:
    branches: [main, develop]
  repository_dispatch:
    types: [pact_changed]
  workflow_dispatch:

env:
  PACT_BROKER_URL: ${{ secrets.PACT_BROKER_URL }}
  PACT_BROKER_TOKEN: ${{ secrets.PACT_BROKER_TOKEN }}

jobs:
  verify-contracts:
    runs-on: ubuntu-latest

    services:
      postgres:
        image: postgres:16
        env:
          POSTGRES_USER: postgres
          POSTGRES_PASSWORD: postgres
          POSTGRES_DB: kbstore_test
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 5432:5432

      rabbitmq:
        image: rabbitmq:3-management
        ports:
          - 5672:5672
          - 15672:15672

    steps:
      - name: Checkout code
        uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build solution
        run: dotnet build --no-restore

      - name: Run Pact provider verification
        run: |
          dotnet test \
            --filter "FullyQualifiedName~KbStoreProviderTests" \
            --logger "trx;LogFileName=pact-verification.trx" \
            --no-build
        env:
          PACT_BROKER_URL: ${{ env.PACT_BROKER_URL }}
          PACT_BROKER_TOKEN: ${{ env.PACT_BROKER_TOKEN }}
          ConnectionStrings__DefaultConnection: "Host=localhost;Database=kbstore_test;Username=postgres;Password=postgres"

      - name: Can I deploy to production?
        if: github.ref == 'refs/heads/main'
        run: |
          docker run --rm \
            -e PACT_BROKER_BASE_URL="$PACT_BROKER_URL" \
            -e PACT_BROKER_TOKEN="$PACT_BROKER_TOKEN" \
            pactfoundation/pact-cli:latest \
            broker can-i-deploy \
              --pacticipant=KbStore \
              --version="$GITHUB_SHA" \
              --to-environment=production \
              --retry-while-unknown=12 \
              --retry-interval=10

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: pact-verification-results
          path: '**/TestResults/*.trx'
```

### Pact Broker Webhooks

Configure webhooks to trigger provider verification when contracts change:

```bash
# Create webhook (run once during setup)
npx pact-broker create-webhook \
  "https://api.github.com/repos/your-org/KbStore/dispatches" \
  --request=POST \
  --header="Content-Type: application/json" \
  --header="Authorization: token $GITHUB_TOKEN" \
  --data='{"event_type": "pact_changed", "client_payload": {"pact_url": "${pactbroker.pactUrl}"}}' \
  --consumer=KbClient \
  --provider=KbStore \
  --contract-published \
  --broker-base-url="$PACT_BROKER_URL" \
  --broker-token="$PACT_BROKER_TOKEN" \
  --description="Trigger KbStore provider verification on contract change"
```

### Deployment Gating

**Before deploying either service**, verify compatibility:

```bash
# Frontend deployment script
#!/bin/bash
set -e

VERSION=$1
ENVIRONMENT=$2

# Check if this version can be safely deployed
npx pact-broker can-i-deploy \
  --pacticipant=KbClient \
  --version="$VERSION" \
  --to-environment="$ENVIRONMENT" \
  --broker-base-url="$PACT_BROKER_URL" \
  --broker-token="$PACT_BROKER_TOKEN"

# If check passes, proceed with deployment
deploy_frontend "$VERSION" "$ENVIRONMENT"

# Record deployment
npx pact-broker record-deployment \
  --pacticipant=KbClient \
  --version="$VERSION" \
  --environment="$ENVIRONMENT" \
  --broker-base-url="$PACT_BROKER_URL" \
  --broker-token="$PACT_BROKER_TOKEN"
```

---

## Developer Workflow

### Day-to-Day Development

#### Scenario 1: Frontend Adds New Feature (Requires New API Endpoint)

```mermaid
sequenceDiagram
    participant FE as Frontend Dev
    participant Pact as Pact Tests
    participant Broker as Pact Broker
    participant BE as Backend Dev
    participant CI as CI/CD

    FE->>FE: Write new React component
    FE->>Pact: Add Pact consumer test for new API
    Pact->>Pact: Generate contract (fails locally - no backend yet)
    FE->>Broker: Publish contract to broker
    Broker->>CI: Webhook triggers backend verification
    CI->>BE: Provider tests fail (endpoint not implemented)
    BE->>BE: Implement new endpoint
    BE->>CI: Push changes, run provider tests
    CI->>Broker: Verification passes ✅
    FE->>FE: Pull latest types from OpenAPI
    FE->>FE: Implement service layer with types
```

**Steps**:

1. **Frontend**: Write consumer test describing expected API behavior
   ```bash
   npm run test:contract
   npm run pact:publish
   ```

2. **Backend receives notification**: Contract published via webhook

3. **Backend**: Implement endpoint to satisfy contract
   ```bash
   dotnet test --filter "KbStoreProviderTests"
   ```

4. **Frontend**: Regenerate types
   ```bash
   npm run generate:types
   ```

#### Scenario 2: Backend Changes Existing API (Potential Breaking Change)

```mermaid
sequenceDiagram
    participant BE as Backend Dev
    participant CI as Backend CI
    participant Broker as Pact Broker
    participant FE as Frontend Team

    BE->>BE: Modify endpoint (e.g., rename field)
    BE->>CI: Push changes
    CI->>Broker: Fetch existing contracts
    Broker->>CI: Return contracts from all branches
    CI->>CI: Run provider verification
    CI->>CI: ❌ Verification fails (breaking change)
    CI->>Broker: Publish failure result
    Broker->>FE: Notify via webhook/email
    FE->>BE: Discuss migration path
    BE->>BE: Implement versioned endpoint (v1 + v2)
    BE->>CI: Push changes
    CI->>CI: ✅ Verification passes for all versions
```

**Steps**:

1. **Backend**: Modify endpoint
   ```bash
   dotnet test --filter "KbStoreProviderTests"
   # FAILS - breaking change detected
   ```

2. **Options**:
   - **Option A**: Revert change (too disruptive)
   - **Option B**: Create v2 endpoint, deprecate v1
   - **Option C**: Coordinate with frontend for synchronized deployment

3. **Implement versioning** (Option B):
   ```csharp
   // Keep v1 for backward compatibility
   group.MapPost("/v1/products", CreateV1);

   // New v2 endpoint with breaking changes
   group.MapPost("/v2/products", CreateV2);
   ```

4. **Update consumer contracts** to use v2 when ready

#### Scenario 3: Frontend Discovers Mock Drift

**Problem**: Frontend's mock data doesn't match real API.

**Solution with Pact**:

1. Run consumer tests against Pact mock server (always up-to-date)
2. Pact mock is generated from contracts verified by backend
3. If frontend mock differs, consumer test fails immediately

**Before Pact**:
```typescript
// Mock is manually maintained - might be wrong!
const mockProduct = {
  id: '123', // ❌ Wrong - backend uses 'productId'
  name: 'Widget',
};
```

**With Pact**:
```typescript
// Pact test enforces correct structure
await provider.addInteraction({
  // ...
  body: {
    productId: uuid(), // ✅ Correct - verified by backend
    name: like('Widget'),
  }
});
```

### Local Development Workflow

#### Frontend Developer

```bash
# 1. Start Pact mock server
npm run pact:mock

# 2. Develop against mock server (localhost:9000)
npm run dev

# 3. Write Pact tests for new features
npm run test:contract

# 4. Verify types are up-to-date
npm run generate:types

# 5. Commit changes (contracts auto-published in CI)
git add .
git commit -m "feat: Add product search functionality"
git push
```

#### Backend Developer

```bash
# 1. Fetch latest contracts
dotnet test --filter "KbStoreProviderTests"

# 2. Implement features to satisfy contracts
# ... code changes ...

# 3. Verify locally
dotnet test --filter "KbStoreProviderTests"

# 4. Publish updated OpenAPI spec
dotnet run --project KbStore.ApiService
curl http://localhost:5000/openapi/v1.json > openapi.json
# Commit openapi.json to repo (or serve dynamically)

# 5. Commit changes
git add .
git commit -m "feat: Implement product search endpoint"
git push
```

### Team Collaboration

**Weekly Contract Review**:
1. Review Pact Broker dashboard
2. Identify pending breaking changes
3. Plan coordinated deployments
4. Discuss API evolution strategy

**Dashboard Metrics**:
- Contract coverage (% of endpoints tested)
- Verification success rate
- Pending breaking changes
- Deployment compatibility matrix

---

## Migration Strategy

### Current State

**KbClient**: Using manual mocks for backend API.

**Example**:
```typescript
// Current mock service
export class MockProductService {
  async create(payload: any): Promise<any> {
    return {
      id: '123', // Hand-crafted mock - might be wrong
      name: payload.name,
    };
  }
}
```

### Migration Phases

#### Phase 1: Parallel Track (Weeks 1-2)

**Goal**: Set up infrastructure without disrupting current development.

**Backend**:
- ✅ Configure OpenAPI generation (already done)
- ✅ Deploy Pact Broker
- ✅ Add PactNet dependency
- ✅ Write provider state setup
- ⏳ Create initial provider verification test

**Frontend**:
- ✅ Install Pact dependencies
- ✅ Generate initial TypeScript types
- ⏳ Create Pact test infrastructure (setup/teardown)
- ⏳ Write 1-2 sample consumer tests (non-critical endpoints)

**Outcome**: Infrastructure ready, proof-of-concept working.

---

#### Phase 2: Gradual Conversion (Weeks 3-6)

**Goal**: Convert existing mocks to Pact contracts incrementally.

**Frontend** (per endpoint):
1. Identify mocked endpoint (e.g., `POST /products`)
2. Write Pact consumer test matching current mock behavior
3. Run test → generates contract
4. Publish contract
5. Verify backend passes (should already work if mock was accurate)
6. Replace mock with real service (using generated types)
7. Remove old mock code

**Example**:

```typescript
// OLD: Manual mock
export class MockProductService {
  async create(payload: any): Promise<any> {
    return { id: '123', name: payload.name };
  }
}

// STEP 1: Write Pact test
it('creates a product', async () => {
  await provider.addInteraction({ /* ... */ });
  const result = await productService.create(payload);
  expect(result.productId).toBeDefined();
});

// STEP 2: Replace with real service
import type { ProductModel, CreateProductPayload } from '@/types/api';

export class ProductService {
  async create(payload: CreateProductPayload): Promise<ProductModel> {
    return httpClient.post('/products', payload);
  }
}
```

**Backend**:
- Monitor Pact Broker for new contracts
- Ensure provider tests pass for all published contracts
- Fix any discrepancies (mocks vs reality)

**Outcome**: 50% of endpoints converted by end of week 6.

---

#### Phase 3: Full Coverage (Weeks 7-10)

**Goal**: All endpoints covered by contracts, mocks retired.

**Frontend**:
- Convert remaining endpoints
- Delete all manual mock services
- Enforce Pact tests in CI (required for PR merge)

**Backend**:
- Add provider verification to CI/CD pipeline
- Block deployments if verification fails
- Document all provider states

**Outcome**: Contract testing fully operational.

---

#### Phase 4: Optimization & Governance (Weeks 11-12)

**Goal**: Streamline workflow, establish governance.

**Actions**:
1. **Performance**: Optimize Pact test execution (parallel runs)
2. **Documentation**: Create team playbooks
3. **Versioning**: Establish API versioning strategy
4. **Breaking Changes**: Define process for coordinated rollouts
5. **Monitoring**: Set up Pact Broker alerts and dashboards

**Outcome**: Self-sustaining contract testing culture.

---

## Versioning & Breaking Changes

### Semantic Versioning for Contracts

Use consumer version tags to track compatibility:

```bash
# Tag stable releases
git tag -a v1.2.3 -m "Release 1.2.3"

# Publish with version tag
npx pact-broker publish \
  --consumer-app-version="1.2.3" \
  --tag="v1.2.3" \
  --tag="prod"
```

### Breaking Change Detection

Pact automatically detects breaking changes:

| Change Type | Breaking? | Pact Behavior |
|-------------|-----------|---------------|
| Add optional field | ❌ No | ✅ Passes |
| Remove field | ✅ Yes | ❌ Fails |
| Rename field | ✅ Yes | ❌ Fails |
| Change field type | ✅ Yes | ❌ Fails |
| Add required field | ✅ Yes | ❌ Fails |
| Change status code | ✅ Yes | ❌ Fails |

### Handling Breaking Changes

#### Option 1: Coordinated Deployment

```mermaid
gantt
    title Coordinated Breaking Change Rollout
    dateFormat  YYYY-MM-DD
    section Backend
    Deploy v2 API (backward compatible)   :2025-01-01, 7d
    section Frontend
    Update to use v2                       :2025-01-03, 5d
    section Backend
    Remove v1 API (after grace period)    :2025-01-15, 1d
```

**Steps**:
1. Backend deploys v2 endpoint alongside v1
2. Frontend updates consumer contracts to v2
3. Frontend deploys using v2
4. Backend deprecates v1 after grace period (30 days)

#### Option 2: API Versioning

```csharp
// Backend: Support multiple versions
app.MapGroup("/v1/products").MapTo<ProductEndpointsV1>();
app.MapGroup("/v2/products").MapTo<ProductEndpointsV2>();
```

```typescript
// Frontend: Gradually migrate
const productService = new ProductServiceV2(); // New code
const legacyService = new ProductServiceV1(); // Old code (deprecated)
```

#### Option 3: Feature Flags

```typescript
// Frontend: Toggle between old/new API
const useV2Api = featureFlags.get('use-v2-product-api');
const service = useV2Api ? new ProductServiceV2() : new ProductServiceV1();
```

### Pact Broker Compatibility Matrix

Example matrix showing deployment safety:

| KbClient Version | KbStore v1.0.0 | KbStore v1.1.0 | KbStore v2.0.0 |
|------------------|----------------|----------------|----------------|
| v1.2.3           | ✅ Compatible  | ✅ Compatible  | ❌ Breaking    |
| v1.3.0           | ✅ Compatible  | ✅ Compatible  | ⚠️  Pending    |
| v2.0.0           | ❌ Breaking    | ❌ Breaking    | ✅ Compatible  |

**Key**:
- ✅ Compatible: Safe to deploy together
- ❌ Breaking: Deployment blocked
- ⚠️ Pending: Verification in progress

---

## Appendix: Complete File Structure

### KbClient (Frontend)

```
KbClient/
├── src/
│   ├── services/
│   │   ├── ProductService.ts         # Uses generated types
│   │   └── SellableItemService.ts
│   └── types/
│       └── api.d.ts                  # Generated from OpenAPI
├── tests/
│   └── contract/
│       ├── setup.ts                  # Pact configuration
│       ├── products.pact.test.ts     # Consumer tests
│       ├── sellableItems.pact.test.ts
│       ├── pacts/                    # Generated contracts
│       │   └── KbClient-KbStore.json
│       └── logs/
│           └── pact.log
├── scripts/
│   └── publish-pacts.sh              # CI helper script
├── package.json                      # Includes pact scripts
└── .github/
    └── workflows/
        └── contract-tests.yml        # CI/CD pipeline
```

### KbStore (Backend)

```
KbStore/
├── KbStore.ApiService/
│   ├── Program.cs                    # OpenAPI config
│   ├── OpenApiDocumentTransformer.cs
│   ├── Endpoints/
│   │   ├── Catalog/
│   │   │   ├── ProductEndpoints.cs   # Annotated for OpenAPI
│   │   │   └── InventoryEndpoints.cs
│   │   └── Storefront/
│   │       └── SellableItemEndpoints.cs
│   └── Middleware/
│       └── ProviderStateMiddleware.cs
├── KbStore.ApiService.Tests/
│   └── Contract/
│       ├── KbStoreProviderTests.cs   # Provider verification
│       ├── ProviderStates.cs         # State setup
│       └── TestResults/
│           └── pact-verification.trx
├── openapi.json                      # Published spec (optional)
└── .github/
    └── workflows/
        └── contract-verification.yml # CI/CD pipeline
```

### Infrastructure

```
infrastructure/
└── pact-broker/
    ├── docker-compose.yml            # Self-hosted Pact Broker
    └── .env.example                  # Configuration template
```

---

## Conclusion

### Summary of Recommendations

1. **Approach**: Use **Types Only (Approach B)** for maximum flexibility
2. **Testing**: Consumer-driven contracts via Pact for drift prevention
3. **CI/CD**: Automated verification gates for all deployments
4. **Migration**: Gradual conversion over 10-12 weeks
5. **Versioning**: Semantic versioning with compatibility matrix

### Benefits Achieved

- **Zero Drift**: Backend changes verified against frontend expectations
- **Fast Feedback**: Breaking changes caught in CI, not production
- **Living Documentation**: Contracts always reflect current state
- **Independent Development**: Frontend/backend teams work in parallel
- **Type Safety**: Full TypeScript coverage without SDK lock-in
- **Deployment Confidence**: Compatibility verified before release

### Next Steps

1. **Week 1**: Deploy Pact Broker, configure OpenAPI generation
2. **Week 2**: Write first Pact consumer/provider tests (proof-of-concept)
3. **Weeks 3-6**: Incremental conversion of existing mocks
4. **Weeks 7-10**: Full coverage, retire manual mocks
5. **Weeks 11-12**: Governance, optimization, team training

### Key Resources

- **Pact Documentation**: https://docs.pact.io
- **PactNet (C#)**: https://github.com/pact-foundation/pact-net
- **Pact JS**: https://github.com/pact-foundation/pact-js
- **OpenAPI TypeScript**: https://github.com/drwpow/openapi-typescript
- **Pact Broker**: https://github.com/pact-foundation/pact_broker

---

**Document Version**: 1.0
**Last Updated**: 2025-01-19
**Authors**: Claude (AI Assistant)
**Review Status**: Draft - Pending Team Review
