using System.Collections.Generic;

namespace Bolcko.Domain.Common
{
    public static class AppPermissions
    {        
        public static class Catalog
        {
            public const string View = "Permissions.Catalog.View";
            public const string CreateEdit = "Permissions.Catalog.CreateEdit";
            public const string Delete = "Permissions.Catalog.Delete";
        }

        public static class Orders
        {
            public const string View = "Permissions.Orders.View";
            public const string ManageStatus = "Permissions.Orders.ManageStatus";
            public const string CancelRefund = "Permissions.Orders.CancelRefund";
        }

        public static class Delivery
        {
            public const string View = "Permissions.Delivery.View";
            public const string Dispatch = "Permissions.Delivery.Dispatch";
            public const string ImpersonateDelivery = "Permissions.Delivery.Impersonate";
        }

        public static class Security
        {
            public const string ViewAnalytics = "Permissions.Security.ViewAnalytics";
            public const string ManageBlacklist = "Permissions.Security.ManageBlacklist";
        }

        public static class UserManagement
        {
            public const string View = "Permissions.UserManagement.View";
            public const string ManageRoles = "Permissions.UserManagement.ManageRoles";
            public const string Impersonate = "Permissions.UserManagement.Impersonate";
        }

        public static class MarketPrices
        {
            public const string View = "Permissions.MarketPrices.View";
            public const string Manage = "Permissions.MarketPrices.Manage";
        }

        public static class Settings
        {
            public const string View = "Permissions.Settings.View";
            public const string Manage = "Permissions.Settings.Manage";
        }

        public static List<PermissionGroupDto> GetAllPermissionGroups()
        {
            return new List<PermissionGroupDto>
            {
                new PermissionGroupDto
                {
                    GroupName = "🏷️ إدارة المنتجات والفئات (Catalog)",
                    Permissions = new List<PermissionItemDto>
                    {
                        new PermissionItemDto { Key = Catalog.View, Name = "عرض قائمة المنتجات والفئات" },
                        new PermissionItemDto { Key = Catalog.CreateEdit, Name = "إضافة وتعديل المنتجات والمتغيرات" },
                        new PermissionItemDto { Key = Catalog.Delete, Name = "حذف المنتجات" }
                    }
                },
                new PermissionGroupDto
                {
                    GroupName = "🛍️ إدارة الطلبات والمبيعات (Orders)",
                    Permissions = new List<PermissionItemDto>
                    {
                        new PermissionItemDto { Key = Orders.View, Name = "عرض قائمة الطلبات" },
                        new PermissionItemDto { Key = Orders.ManageStatus, Name = "تغيير حالة الشحن والطلب" },
                        new PermissionItemDto { Key = Orders.CancelRefund, Name = "إلغاء الطلبات وإرجاع المبالغ" }
                    }
                },
                new PermissionGroupDto
                {
                    GroupName = "🚚 مركز التوصيل واللوجستيات (Delivery)",
                    Permissions = new List<PermissionItemDto>
                    {
                        new PermissionItemDto { Key = Delivery.View, Name = "عرض شركات وسائقي التوصيل" },
                        new PermissionItemDto { Key = Delivery.Dispatch, Name = "توزيع الطلبات وتعيين السائقين" },
                        new PermissionItemDto { Key = Delivery.ImpersonateDelivery, Name = "محاكاة تصفح حساب شركة التوصيل" }
                    }
                },
                new PermissionGroupDto
                {
                    GroupName = "🛡️ مركز الأمان والزيارات (Security & Analytics)",
                    Permissions = new List<PermissionItemDto>
                    {
                        new PermissionItemDto { Key = Security.ViewAnalytics, Name = "مشاهدة إحصائيات الزيارات والترافيك" },
                        new PermissionItemDto { Key = Security.ManageBlacklist, Name = "إدارة القائمة السوداء وحظر الـ IPs" }
                    }
                },
                new PermissionGroupDto
                {
                    GroupName = "👥 إدارة المستخدمين والأدوار (Users & Roles)",
                    Permissions = new List<PermissionItemDto>
                    {
                        new PermissionItemDto { Key = UserManagement.View, Name = "عرض قائمة المستخدمين والأدوار" },
                        new PermissionItemDto { Key = UserManagement.ManageRoles, Name = "إنشاء وتعديل الأدوار والسويتشات" },
                        new PermissionItemDto { Key = UserManagement.Impersonate, Name = "محاكاة تصفح أي حساب (User Impersonation)" }
                    }
                },
                new PermissionGroupDto
                {
                    GroupName = "💳 أسعار السوق (Market Prices)",
                    Permissions = new List<PermissionItemDto>
                    {
                        new PermissionItemDto { Key = MarketPrices.View, Name = "عرض أسعار السوق" },
                        new PermissionItemDto { Key = MarketPrices.Manage, Name = "إضافة وتعديل أسعار السوق" }
                    }
                },
                new PermissionGroupDto
                {
                    GroupName = "⚙️ إعدادات النظام (Settings)",
                    Permissions = new List<PermissionItemDto>
                    {
                        new PermissionItemDto { Key = Settings.View, Name = "عرض إعدادات النظام" },
                        new PermissionItemDto { Key = Settings.Manage, Name = "تعديل إعدادات المتجر العامة" }
                    }
                }
            };
        }
    }

    public class PermissionGroupDto
    {
        public string GroupName { get; set; } = string.Empty;
        public List<PermissionItemDto> Permissions { get; set; } = new();
    }

    public class PermissionItemDto
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
