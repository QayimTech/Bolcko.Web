namespace Bolcko.Domain.Enums
{
    public enum SourcingStatus
    {
        PendingSourcing = 0,    // بانتظار الرفع للمورد
        SentToSupplier = 1,     // تم إرسال أمر الشراء للمورد (قيد التجهيز في مستودع المورد)
        ReadyForPickup = 2,     // تم التبكيج وجاهز لاستلام شركة التوصيل (GLC)
        PickedUpByCourier = 3,  // تم الاستلام من قبل سيارة شركة التوصيل
        Completed = 4,          // مكتمل
        FailedOrCancelled = 5   // ملغي أو تعذر التوريد
    }
}
