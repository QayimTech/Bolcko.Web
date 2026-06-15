import xml.etree.ElementTree as ET
import os

ar_keys = {
    "HomeHeroBadge": "توريدات إنشائية معتمدة",
    "HomeHeroTitle": "الأساس المتين لكل مشروع عظيم.",
    "HomeHeroDesc": "توريدات صناعية وإنشائية عالية الدقة للمقاولين المحترفين. من الخرسانة والمواد الخام إلى الأدوات الدقيقة، نوفر لك كل ما تحتاجه.",
    "ShippingBadge": "شحن وتوصيل فوري",
    "ShippingTitle": "جاهز ومتاح للشحن.",
    "ShippingDesc": "98% من كتالوجنا الأساسي مخزّن وجاهز للإرسال الفوري مباشرة لموقع العمل الخاص بك.",
    "BrowseSmartly": "تصفح بذكاء",
    "FeaturedCategories": "الفئات المميزة",
    "CustomerFavorites": "المفضلة لدى العملاء",
    "BestSellingProducts": "المنتجات الأكثر مبيعاً",
    "LiveUpdated": "مباشر ومحدث",
    "LocalMarketPrices": "أسعار مواد البناء بالسوق المحلي",
    "MarketPricesDesc": "مؤشرات يومية تقريبية لأسعار المواد الإنشائية الأساسية في السوق المحلي الأردني لدعم تسعير مشاريعكم بدقة.",
    "CorporateAccounts": "حسابات الشركات والـ B2B",
    "BigProjectsTitle": "المشاريع الكبيرة تتطلب تسعيراً دقيقاً ودعماً مخصصاً.",
    "BigProjectsDesc": "قم بتوسيع نطاق أعمالك بكفاءة. تقدم بوابة B2B وخدمات الشركات لـ LOCKO خصومات كبيرة على الكميات، عمليات تسليم مجدولة للمواقع، ومديري حسابات مخصصين لشركات المقاولات الكبرى.",
    "GuaranteedSupply": "توريد مضمون ومستدام",
    "GuaranteedSupplyDesc": "تأمين وتخزين الكميات المطلوبة لمراحل مشروعك طويلة الأجل دون انقطاع.",
    "AdvancedLogistics": "دعم لوجستي متقدم",
    "AdvancedLogisticsDesc": "جداول شحن وتوصيل مخصصة متناسبة مع مواقع العمل المتعددة الخاصة بشركتك.",
    "RequestBulkQuote": "اطلب عرض سعر للكميات الكبيرة",
    "CompanyName": "اسم الشركة",
    "ContactPerson": "اسم المسؤول",
    "ProjectType": "نوع المشروع",
    "SubmitRequest": "إرسال الطلب",
    "FastDelivery": "توصيل سريع وموثوق",
    "FastDeliveryDesc": "شحن مجدول وتوصيل للمنطقة الصناعية ومختلف المواقع الإنشائية بيسر وسهولة.",
    "WarrantyMatching": "ضمان ومطابقة المواصفات",
    "WarrantyMatchingDesc": "كافة المنتجات لدينا مطابقة للمقاييس العالمية ومرفقة بشهادات الفحص المخبري.",
    "TechnicalSupport": "دعم فني واستشاري 24/7",
    "TechnicalSupportDesc": "فريق من المستشارين والمهندسين جاهز لمساعدتكم في أي وقت لتقدير الكميات.",
    "FlexiblePayments": "تسهيلات وخيارات دفع مرنة",
    "FlexiblePaymentsDesc": "خيارات دفع رقمية متعددة وآمنة، إضافة إلى خطوط الائتمان للمؤسسات المسجلة.",
    "Residential": "سكني",
    "Commercial": "تجاري",
    "Industrial": "صناعي"
}

en_keys = {
    "HomeHeroBadge": "Certified Construction Supplies",
    "HomeHeroTitle": "The Solid Foundation for Every Great Project.",
    "HomeHeroDesc": "High-precision industrial and construction supplies for professional contractors. From concrete and raw materials to precision tools, we provide everything you need.",
    "ShippingBadge": "Immediate Shipping & Delivery",
    "ShippingTitle": "Ready and Available for Shipping.",
    "ShippingDesc": "98% of our core catalog is stocked and ready for immediate dispatch directly to your job site.",
    "BrowseSmartly": "Browse Smartly",
    "FeaturedCategories": "Featured Categories",
    "CustomerFavorites": "Customer Favorites",
    "BestSellingProducts": "Best Selling Products",
    "LiveUpdated": "Live & Updated",
    "LocalMarketPrices": "Local Market Construction Material Prices",
    "MarketPricesDesc": "Approximate daily indicators for basic construction material prices in the local Jordanian market to support accurate project pricing.",
    "CorporateAccounts": "Corporate Accounts & B2B",
    "BigProjectsTitle": "Large projects require accurate pricing and dedicated support.",
    "BigProjectsDesc": "Scale your business efficiently. Bolcko's B2B portal and corporate services offer volume discounts, scheduled site deliveries, and dedicated account managers for major contracting firms.",
    "GuaranteedSupply": "Guaranteed & Sustainable Supply",
    "GuaranteedSupplyDesc": "Securing and storing required quantities for your long-term project phases without interruption.",
    "AdvancedLogistics": "Advanced Logistics Support",
    "AdvancedLogisticsDesc": "Customized shipping and delivery schedules tailored to your company's multiple job sites.",
    "RequestBulkQuote": "Request a Quote for Bulk Quantities",
    "CompanyName": "Company Name",
    "ContactPerson": "Contact Person",
    "ProjectType": "Project Type",
    "SubmitRequest": "Submit Request",
    "FastDelivery": "Fast & Reliable Delivery",
    "FastDeliveryDesc": "Scheduled shipping and delivery to industrial zones and various construction sites with ease.",
    "WarrantyMatching": "Warranty & Specification Compliance",
    "WarrantyMatchingDesc": "All our products comply with international standards and are accompanied by laboratory test certificates.",
    "TechnicalSupport": "24/7 Technical & Consulting Support",
    "TechnicalSupportDesc": "A team of consultants and engineers is ready to help you at any time to estimate quantities.",
    "FlexiblePayments": "Flexible Payment Facilities & Options",
    "FlexiblePaymentsDesc": "Multiple secure digital payment options, in addition to credit lines for registered establishments.",
    "Residential": "Residential",
    "Commercial": "Commercial",
    "Industrial": "Industrial"
}

def update_resx(file_path, keys_dict):
    tree = ET.parse(file_path)
    root = tree.getroot()
    
    existing_keys = {data.get("name") for data in root.findall("data")}
    
    for key, val in keys_dict.items():
        if key not in existing_keys:
            data = ET.SubElement(root, "data", attrib={"name": key, "xml:space": "preserve"})
            value = ET.SubElement(data, "value")
            value.text = val
            
    tree.write(file_path, encoding="utf-8", xml_declaration=True)

update_resx(r"c:\Users\hamza\source\repos\Bolcko.Web\Bolcko.Web.App\Resources\SharedResource.ar.resx", ar_keys)
update_resx(r"c:\Users\hamza\source\repos\Bolcko.Web\Bolcko.Web.App\Resources\SharedResource.en.resx", en_keys)
