using Bolcko.Domain.Entities.Product.DTOs;
using System.Collections.Generic;

namespace Bolcko.Web.App.Areas.Shop.Models
{
    public class VendorShowroomViewModel
    {
        public string Slug { get; set; } = "al-qannas";
        public string Name { get; set; } = "مجموعة القنّاص لتوريد مواد البناء";
        public string NameEn { get; set; } = "Al-Qannas Building Materials Group";
        public string Tagline { get; set; } = "الموزع والوكيل المعتمد لكبرى مصانع الإسمنت والحديد والخرسانة في الأردن";
        public string Description { get; set; } = "نقدم حلول توريد متكاملة للمشاريع الإنشائية والتجارية والسكنية بأسعار الجملة المباشرة من المصنع، مع أسطول شاحنات مجهز بأوناش تفريغ هيدروليكية وفحص قبان معتمد.";
        
        public string LogoUrl { get; set; } = "/images/suppliers/qannas-logo.png";
        public string BannerUrl { get; set; } = "/images/suppliers/qannas-banner.jpg";
        
        public string CommercialRegistrationNumber { get; set; } = "CR-19948201";
        public string NationalTaxNumber { get; set; } = "TAX-99482010";
        public string City { get; set; } = "عمّان - سحاب الصناعية";
        public string WarehouseAddress { get; set; } = "مدينة الملك عبدالله الثاني ابن الحسين الصناعية - سحاب، الشارع الرئيسي";
        public double Latitude { get; set; } = 31.8756;
        public double Longitude { get; set; } = 35.9861;

        public double Rating { get; set; } = 4.95;
        public int ReviewCount { get; set; } = 342;
        public int CompletedOrders { get; set; } = 1420;
        public double OnTimeDeliveryRate { get; set; } = 99.4;
        
        public bool IsGoldSupplier { get; set; } = true;
        public bool IsFactoryDirect { get; set; } = true;
        public string FulfillmentMode { get; set; } = "أسطول نقل خاص + ونش تفريغ (Own Fleet)";
        
        public string ContactPhone { get; set; } = "0790000000";
        public string ContactWhatsApp { get; set; } = "962790000000";
        public string ContactEmail { get; set; } = "sales@alqannas-jo.com";

        public IEnumerable<ProductDto> Products { get; set; } = new List<ProductDto>();
        public List<string> AvailableCategories { get; set; } = new();
        public string? SelectedCategory { get; set; }
    }
}
