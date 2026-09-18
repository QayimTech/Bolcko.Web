using System.Threading.Tasks;
using Bolcko.Domain.Entities.Order;

namespace Blocko.Services.Interfaces.Tax
{
    public class TaxInvoiceViewModel
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;
        public string InvoiceType { get; set; } = "B2C"; // "B2C_Simplified" or "B2B_Standard"
        
        // Seller (BlockO Platform)
        public string SellerName { get; set; } = "منصة BlockO لمواد البناء المحدودة";
        public string SellerVatNumber { get; set; } = "310482910400003";
        public string SellerCrNumber { get; set; } = "1010784920";
        public string SellerAddress { get; set; } = "الرياض، المملكة العربية السعودية";

        // Buyer (Customer or Contractor)
        public string BuyerName { get; set; } = string.Empty;
        public string? BuyerCrNumber { get; set; }
        public string? BuyerVatNumber { get; set; }
        public string? JobsiteAddress { get; set; }
        public string? BuildingPermitNumber { get; set; }

        // Financials
        public decimal Subtotal { get; set; }
        public decimal VatRate { get; set; } = 0.15m;
        public decimal VatAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }

        // ZATCA TLV Base64 QR Code
        public string QrCodeBase64 { get; set; } = string.Empty;

        // Line Items
        public List<TaxInvoiceItemDto> Items { get; set; } = new List<TaxInvoiceItemDto>();
    }

    public class TaxInvoiceItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal VatAmount { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public interface ITaxInvoicingService
    {
        Task<TaxInvoiceViewModel> GenerateInvoiceForOrderAsync(int orderId, string? userRole = null);
        string GenerateZatcaTlvQrCode(string sellerName, string vatNumber, DateTime timestamp, decimal totalAmount, decimal vatAmount);
    }
}
