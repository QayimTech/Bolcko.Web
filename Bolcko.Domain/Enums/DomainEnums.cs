namespace Bolcko.Domain.Enums
{
    public enum UserType
    {
        Customer = 1,
        DashboardUser = 2,
        Admin = 3 // Super Admin
    }

    public enum OrderStatus
    {
        Pending = 1,
        Processing = 2,
        Shipped = 3,
        Delivered = 4,
        Cancelled = 5
    }

    public enum ProjectStatus
    {
        Active = 1,
        Completed = 2,
        OnHold = 3
    }

    public enum TenderStatus
    {
        Open = 1,
        UnderReview = 2,
        Awarded = 3,
        Closed = 4
    }

    public enum ProductStatus
    {
        InStock = 1,
        OutOfStock = 2,
        PreOrder = 3,
        Discontinued = 4
    }

    public enum AddressType
    {
        Shipping = 1,
        Billing = 2,
        ProjectLocation = 3
    }
}