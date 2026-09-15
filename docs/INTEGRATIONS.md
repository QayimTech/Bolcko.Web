# Third-Party Integrations Guide

## 1. Dynamic Wholesaler Integration (Qannas API)

### Overview
Bolcko supports automated, just-in-time order dispatching to external supplier APIs. This removes the need for physical warehousing for supported supplier SKUs.

### Key Entities
- `SupplierProviderConfig`: Stores API base URL, API credentials, Customer Username/Password, and active status in the database.
- `SourcingStatus`: Enum tracking order dispatch (`PendingSourcing`, `SentToSupplier`, `ConfirmedBySupplier`, `FailedToSource`).

### Integration Flow
```mermaid
sequenceDiagram
    participant User as Customer
    participant App as Bolcko Store
    participant Supplier as Qannas Wholesaler API
    participant GLC as GLC Courier Logistics

    User->>App: Places Order (Product with SupplierKey)
    App->>Supplier: POST /ar/api/Auth/CustomerLogin (Obtain Bearer Token)
    Supplier-->>App: Return JWT Token (Cached for 12 hours)
    App->>Supplier: POST /ar/api/Orders/Add (Order Items, Customer Details)
    Supplier-->>App: Return Supplier Order Number (e.g. QN-8921)
    App->>App: Set SourcingStatus = ConfirmedBySupplier
    App->>GLC: Book Pickup: "عمان - رأس العين - مستودع القناص"
    GLC-->>App: Tracking Code Generated
```

### Admin Configuration
Navigate to `Admin Area -> Supplier Settings` to configure supplier credentials, test authentication, and view dispatch logs.

---

## 2. Logistics & Delivery Integration (GLC Logistics)

### Dynamic Pickup Routing
When an order contains items fulfilled by external suppliers:
- Pickup Address: `عمان - رأس العين - مستودع القناص`
- Delivery Address: Customer Shipping Address
- Courier: GLC Express Delivery\n