using System;
using System.Collections.Generic;

namespace Bolcko.Domain.Entities.Payment
{
    public class PaymentGatewaySettingsDto
    {
        // 1. Cash On Delivery (COD)
        public bool CodEnabled { get; set; } = true;
        public string CodTitleAr { get; set; } = "الدفع عند الاستلام (Cash On Delivery - COD)";
        public string CodTitleEn { get; set; } = "Cash On Delivery (COD)";
        public string CodDescAr { get; set; } = "ادفع نقداً أو عبر جهاز الدفع الإلكتروني (POS) عند استلام المواد ومعاينتها في موقعك.";
        public string CodDescEn { get; set; } = "Pay cash or via mobile POS machine upon receiving and inspecting materials at your site.";
        public decimal CodExtraFee { get; set; } = 0.00m;

        // 2. CliQ Instant Payment (Escrow & Direct)
        public bool CliqEnabled { get; set; } = false;
        public string CliqTitleAr { get; set; } = "دفع فوري عبر كليك CliQ (محمي بحساب الضمان)";
        public string CliqTitleEn { get; set; } = "CliQ Instant Payment (Escrow Protected)";
        public string CliqDescAr { get; set; } = "يُحجز المبلغ في حساب الضمان الآمن ولا يُحرر للمورد إلا بعد استلام المواد بالموقع وفحصها.";
        public string CliqDescEn { get; set; } = "Funds held securely in escrow until materials arrive and pass site inspection.";
        public string CliqAlias { get; set; } = string.Empty;
        public string CliqBankName { get; set; } = "البنك العربي - Arab Bank";
        public string CliqIban { get; set; } = string.Empty;
        public bool CliqIsEscrowProtected { get; set; } = true;

        // 3. eFAWATEERcom (إي فواتيركم)
        public bool EfawateercomEnabled { get; set; } = false;
        public string EfawateercomTitleAr { get; set; } = "إي فواتيركم (eFAWATEERcom)";
        public string EfawateercomTitleEn { get; set; } = "eFAWATEERcom Bill Payment";
        public string EfawateercomDescAr { get; set; } = "تسديد مباشر عبر تطبيق البنك الأردني الخاص بك أو المحافظ الإلكترونية برقم الفاتورة.";
        public string EfawateercomDescEn { get; set; } = "Pay directly via your Jordanian bank app or digital wallets using the bill reference number.";
        public string EfawateercomBillerCode { get; set; } = string.Empty;
        public string EfawateercomServiceName { get; set; } = "توريدات بلوكو الإنشائية";

        // 4. Online Card Gateway (Visa / MasterCard / Apple Pay / ZainCash / MPGS / HyperPay / Tap)
        public bool CardGatewayEnabled { get; set; } = false;
        public string CardGatewayTitleAr { get; set; } = "بطاقات الدفع والمحافظ الإلكترونية (Visa / Master / Apple Pay)";
        public string CardGatewayTitleEn { get; set; } = "Online Card & Wallet Payment (Visa / Master / Apple Pay)";
        public string CardGatewayDescAr { get; set; } = "دفع إلكتروني آمن ومشفر 100% عبر بوابات الدفع البنكية المعتمدة بالدينار الأردني.";
        public string CardGatewayDescEn { get; set; } = "100% secure and encrypted online card payment in Jordanian Dinars (JOD).";
        
        public string CardGatewayProvider { get; set; } = "Generic"; // Generic, Tap, HyperPay, Stripe, PayTabs, ZainCash, MPGS
        public string CardGatewayEnvironment { get; set; } = "Sandbox"; // Sandbox, Live
        public string CardGatewayMerchantId { get; set; } = string.Empty;
        public string CardGatewayApiKey { get; set; } = string.Empty;
        public string CardGatewaySecretKey { get; set; } = string.Empty;
        public string CardGatewayPublicKey { get; set; } = string.Empty;
        public string CardGatewayWebhookSecret { get; set; } = string.Empty;
        public string CardGatewayBaseUrl { get; set; } = string.Empty;
        public string CardGatewayCurrency { get; set; } = "JOD";
    }

    public class PaymentMethodOptionDto
    {
        public string Code { get; set; } = string.Empty; // COD, CLIQ_ESCROW, EFAWATEERCOM, ONLINE_CARD
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "payments";
        public string BadgeText { get; set; } = string.Empty;
        public string ColorClass { get; set; } = "amber";
        public bool IsSelectedDefault { get; set; }
        public Dictionary<string, string> ExtraData { get; set; } = new();
    }
}
