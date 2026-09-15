# Bolcko Architecture & Technical Design

## 1. Architectural Philosophy
Bolcko follows the **Clean Architecture** pattern to guarantee:
- **Framework Independence:** Core business rules do not depend on external UI or ORM libraries.
- **Testability:** Domain logic can be tested without databases, UI servers, or third-party web services.
- **Flexibility:** Swapping database providers, external courier APIs, or caching engines requires changes only in the respective infrastructure layer.

---

## 2. Layer Breakdown

### Layer 1: Domain (`Bolcko.Domain`)
The core enterprise layer containing:
- **Entities:** `Product`, `ProductVariant`, `ProductImage`, `Order`, `OrderItem`, `Category`, `SupplierProviderConfig`, `QuoteRequest`, `ApplicationUser`.
- **DTOs:** Lightweight, typed data contracts passed between controllers and services.
- **Enums:** `OrderStatus`, `PaymentStatus`, `SourcingStatus`, `SeoStatus`.

### Layer 2: Persistence (`Blocko.Persistence`)
Responsible for data storage and persistence mechanisms:
- `ApplicationDbContext`: Configures Entity Framework Core with PostgreSQL provider (`Npgsql.EntityFrameworkCore.PostgreSQL`).
- Schema Configurations & Fluent API mapping.
- Idempotent Database Migrations.

### Layer 3: Services (`Blocko.Services`)
The application business logic layer:
- `ISupplierApiService` & `SupplierApiService`: Handles dynamic wholesaler authentication, token caching, order dispatching, and status synchronization.
- `IDeliveryApiService` & `DeliveryApiService`: Integrates with GLC Courier, dynamic pickup routing, and shipping calculation.
- `IOrderService` & `OrderService`: Order creation, inventory deduction, and status transitions.
- `IShoppingCartService`: Session-based and user-bound shopping carts.
- `IQuoteService`: RFQ submission, pricing negotiation, and quotation lifecycle.

### Layer 4: Presentation (`Bolcko.Web.App`)
The ASP.NET Core 8 Web layer:
- **Areas:**
  - `Shop`: Public storefront, product catalog, cart, checkout, customer portal.
  - `Admin`: Back-office dashboard for managing products, categories, orders, suppliers, and SEO.
- **Controllers & API Controllers:**
  - `HealthController`: AWS ALB and container orchestrator health probes.
  - `AuthController`, `ProductController`, `OrderController`.
- **Middlewares:**
  - `SecurityAndTrafficMiddleware`: Rate limiting, XSS protection, and immediate bypass for health probes.
  - `FirstRunSetupMiddleware`: Auto-seeds default administrative credentials and roles.\n