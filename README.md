# KingStore - Clean Architecture & DDD Core Sample

Welcome to the backend core of **KingStore**, a high-performance, production-ready E-commerce solution built using **.NET 10**. This repository serves as a professional technical showcase demonstrating advanced software engineering principles, strict architectural isolation, and real-time state management.

---

## Architectural Overview

The project is strictly designed around **Clean Architecture** and **Domain-Driven Design (DDD)** principles. It isolates the enterprise business logic from external frameworks, database ORMs, and transport layers.



### Project Structure & Layers
* **`src/KingStore.Domain`**: The core layer, completely free of external dependencies (Pure C#). Contains rich domain entities, aggregates, enums, and domain exceptions. Enforces invariants and business validation internally via encapsulation.
* **`src/KingStore.Application`**: Implements the **CQRS (Command Query Responsibility Segregation)** pattern using **MediatR**. Handles application use cases, validation, DTOs, and interfaces for external dependencies.
* **`src/KingStore.Infrastructure`**: Contains implementation details such as database access (**EF Core** with Fluent API configurations), identity management (`IdentityDbContext`), background workers, and real-time signaling.
* **`src/KingStore.WebApi`**: The presentation layer containing thin RESTful controllers responsible solely for handling HTTP requests/responses and routing them via MediatR.
* **`tests/KingStore.UnitTests`**: Comprehensive testing suite split into state-based testing for domain models and behavioral/isolated testing for application handlers.

---

## Key Technical Highlights (Showcase Features)

### 1. Rich Domain Model & Encapsulation
Unlike anemic domain models, the `Order` and `Shoe` entities govern their own state boundaries. Public setters are banned (`private set`). State mutations—such as dual currency processing (`SubmitDualPayment`), restocking, and order cancellation—occur strictly through domain methods that prevent invalid transitions.

### 2. High-Performance CQRS & N+1 Loop Prevention
Application handlers are heavily optimized for performance. In operations like `UpdateStatusHandler` (Order Rejection), database round-trips within loops are strictly eliminated. All targeted records are pulled atomically using batch IDs (`GetByIdsAsync`), tracked in memory, and pushed via a single transactional commit.

### 3. Atomic Database Transactions (Unit of Work)
Data consistency across multiple repositories is guaranteed by implementing the **Unit of Work** pattern. Changes made to the `OrderRepository` and `ShoeRepository` are committed inside a unified atomic transaction (`SaveChangesAsync`), ensuring that if stock updates fail, the entire operation rolls back.

### 4. Enterprise Background Workers via `PeriodicTimer`
Instead of using legacy `Task.Delay` patterns that introduce execution drift, the `OrderCleanupService` runs on a high-precision **`PeriodicTimer`** introduced in modern .NET. It runs asynchronously in the background every minute to release expired shoe reservations (15-minute unpaid timeouts) and ensure global data consistency.

### 5. Memory-Optimized Real-Time Signaling (SignalR)
Real-time user notifications use SignalR hubs with optimized tracking. Instead of manually mapping connection groups for individual users—which consumes server memory—the system utilizes SignalR’s built-in `Clients.User(userId)` abstraction mapped directly from claims tokens. Centralized authorization isolates the `"Admin"` group automatically.

### 6. Strict Architecture & Domain Isolation
To maintain architectural purity, all `System.ComponentModel.DataAnnotations` (like `[ForeignKey]`) are stripped from the Domain layer. Data relations, decimal precisions, and shadow properties are entirely managed using the **Fluent API** inside the Infrastructure persistence layer (`KingStoreDbContext`), bypassing virtual lazy-loading loops in favor of strict Eager Loading.

---

## Testing Strategy

The solution features automated unit and integration tests using **xUnit**, **FluentAssertions**, and **Moq**:
* **Domain State Verification**: Validates entity invariants, business exceptions, image switching bounds, and time-sensitive state checks utilizing flaky-safe precision testing (`BeCloseTo`).
* **Application Behavior Mocks**: Isolates handlers by mocking external database infrastructures and tracking behavioral calls (`Verify`) to guarantee database updates are committed only when preconditions succeed.

To execute tests run:
```bash
dotnet test
 
```

## Technology Stack
* **Backend Framework**: .NET 10 (ASP.NET Core)
* **Design Patterns**: Domain-Driven Design (DDD), CQRS, Repository Pattern, Unit of Work
* **Libraries & ORM**: MediatR, Entity Framework Core (EF Core), ASP.NET Core Identity, SignalR
* **Testing Suite**: xUnit, Moq, FluentAssertions