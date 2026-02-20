# Instructions for candidates

This is the .NET version of the Payment Gateway challenge. If you haven't already read this [README.md](https://github.com/cko-recruitment/) on the details of this exercise, please do so now. 

## Template structure
```
src/
    PaymentGateway.Api - a skeleton ASP.NET Core Web API
test/
    PaymentGateway.Api.Tests - an empty xUnit test project
imposters/ - contains the bank simulator configuration. Don't change this

.editorconfig - don't change this. It ensures a consistent set of rules for submissions when reformatting code
docker-compose.yml - configures the bank simulator
PaymentGateway.sln
```

Feel free to change the structure of the solution, use a different test library etc.

## This solution has the following implementation

### Core Implementation
- ✅ **Process Payment API** - POST `/api/payments`
- ✅ **Retrieve Payment API** - GET `/api/payments/{id}`
- ✅ **Comprehensive Validation** - All fields validated per requirements
- ✅ **Bank Integration** - Full integration with simulator
- ✅ **Card Security** - Masked card numbers (PCI compliance)
### Code Quality
- ✅ **Clean Architecture** - Separation of concerns (API → Application → Domain)
- ✅ **SOLID Principles** - Dependency injection, interface abstractions
- ✅ **Automated Tests** - Unit tests + Component tests
- ✅ **Production-Ready Code** - Error handling, logging, async/await, threadsafe

### Key Design Patterns
1. **Repository Pattern** - Abstracted data access
2. **Result Pattern** - Explicit success/failure handling
3. **Factory Pattern** - Payment entity creation and acquiring bank creation
4. **Typed HttpClient** - Bank client integration
5. **Dependency Injection** - Loosely coupled components

### Recommendations for Improvement
- Organise solution to have separate projects for API, Domain/Services, Tests given more time
- Use a distributed cache (e.g., Redis) for idempotency in production
- No authentication implemented - would add API key or OAuth in production
- Acquiring bank name is supplied via the request for simplicity - in production would determine this via config

## Architecture Choices

### 1. Clean Architecture / Layered Design

**Decision**: Implement a three-layer architecture (API → Application → Domain)

**Rationale**:
- **Separation of Concerns**: Each layer has a single, well-defined responsibility
- **Testability**: Dependencies flow inward, making unit testing easier with mocks
- **Maintainability**: Changes to one layer don't cascade to others
- **Future-proofing**: Easy to swap infrastructure (e.g., move from in-memory to SQL database)

**Trade-offs**:
- ✅ Better long-term maintainability
- ✅ Easier to test in isolation
- ❌ More initial boilerplate code
- ❌ Slight performance overhead from layer transitions



### 2. Repository Pattern

**Decision**: Abstract data access behind `IPaymentRepository` interface

**Rationale**:
- Enables testing service layer without real database
- Allows swapping storage implementations (in-memory → SQL → NoSQL)
- Follows dependency inversion principle

**Alternative Considered**: Direct data access in service layer
- Would be simpler for this small project
- Would couple business logic to storage mechanism
- Would make unit testing harder

### 3. Result Pattern vs Exceptions

**Decision**: Use `PaymentResult` class to wrap success/failure outcomes

**Rationale**:
- Exceptions should be exceptional - validation failures are expected
- Makes success/failure paths explicit in code
- Better performance (no exception throwing)
- Clear API contracts
### 4. Validation Strategy

**Decision**: Centralized validation in `PaymentValidator` before calling bank

**Rationale**:
- **Fail fast**: Reject invalid requests before external API calls
- **Cost reduction**: Avoid bank API charges for invalid requests
- **Better UX**: Immediate feedback to merchants
- **Single source of truth**: One place to maintain validation rules

### 5. Domain Model Design

**Decision**: Rich domain model with encapsulation

**Rationale**:
- Business rules enforced at domain level (not just validation layer)
- Impossible states are impossible (can't have authorized payment without auth code)
- Self-documenting code
### 6. Security: Card Number Handling

**Decision**:
- Store masked card number in entity only after processing
- Immediately extract and store last 4 digits
- Only expose last 4 digits in responses

**Rationale**:
- PCI DSS requires masking card numbers
- Full number needed for bank processing
- Separation allows future tokenization

### 7. HTTP Client Management

**Decision**: Use `IHttpClientFactory` with typed client pattern

**Rationale**:
- Prevents socket exhaustion
- Follows .NET best practices
- Enables testing with mock HTTP handlers
- Automatic disposal and lifecycle management
- Retries for transient failures

## Testing Strategy

### Unit Tests

**Scope**: Individual components in isolation
**Tools**: xUnit, Moq
**Coverage**:
- `PaymentValidator`: tests covering all validation rules
- `PaymentRepository`: tests covering idempotency

### Component (Integration) Tests
**Scope**: Full request/response cycle through API.

**Tools**: xUnit, WebApplicationFactory, WireMock

**Coverage**: End-to-end Payment Gateway API behavior. Integration test for me would then cover the E2E flow 
from the Shopper -> Merchant -> Payment Gateway

## Concurrency & Thread Safety

### Repository Thread Safety

**Decision**: Used `ConcurrentDictionary` for thread-safe in-memory storage. It has
built in idempotency check via `GetOrAdd` method.

**Recommendation**: Suggest use a distributed cache like Redis to do the idempotency check.