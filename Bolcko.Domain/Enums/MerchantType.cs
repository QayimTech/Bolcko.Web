namespace Bolcko.Domain.Enums
{
    public enum MerchantType
    {
        Manufacturer = 1,              // مصنع مباشر - خالي من وسيط البيع
        AuthorizedAgent = 2,           // وكيل رسمي معتمد
        SoleExclusiveDistributor = 3,  // موزع حصري وحيد
        VerifiedStockist = 4           // تاجر ومورد معتمد
    }
}
