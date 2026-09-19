using Bolcko.Domain.Entities.Webhook;
using System.Collections.Generic;

namespace Bolcko.Web.App.Areas.Vendor.Models
{
    public class VendorWebhooksViewModel
    {
        public VendorWebhookConfig Config { get; set; } = new();
        public IEnumerable<WebhookDeliveryLog> DeliveryLogs { get; set; } = new List<WebhookDeliveryLog>();
        
        public string InboundWebhookEndpoint { get; set; } = string.Empty;
        public string SampleInboundPayload { get; set; } = string.Empty;

        public Dictionary<string, (string Title, string Description)> AvailableEvents { get; set; } = new()
        {
            ["order.placed"] = ("تم إنشاء طلب جديد (Order Placed)", "يتم إرسال الحدث فور اعتماد طلب العميل أو المقاول للشروع في التجهيز"),
            ["order.escrow_funded"] = ("تم إيداع وحجز أموال المرابحة (Escrow Funded)", "إشعار رسمي بضمان أموال التوريد في حساب الضمان للمباشرة بالتحميل"),
            ["order.dispatch_ready"] = ("جاهز للإرسال وتعيين السائق (Dispatch Ready)", "إشعار اكتمال وزن القبان وإصدار بيان الحمولة المعتمد"),
            ["shipment.status_updated"] = ("تحديث حالة الشحنة الميدانية (Status Updated)", "إشعار بوصول الشاحنة للموقع، بدء التفريغ، أو التوقيع الرقمي"),
            ["order.cancelled"] = ("إلغاء الطلبية واسترجاع المخزون (Order Cancelled)", "إشعار بإلغاء الطلب وتحرير المواد المحجوزة في المستودع")
        };
    }
}
