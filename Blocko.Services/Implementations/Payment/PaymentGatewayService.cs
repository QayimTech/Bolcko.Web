using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blocko.Services.Interfaces.Payment;
using Bolcko.Domain.Entities.Payment;
using Bolcko.Domain.Entities.Setting;
using Bolcko.Domain.Interfaces;

namespace Blocko.Services.Implementations.Payment
{
    public class PaymentGatewayService : IPaymentGatewayService
    {
        private readonly IUnitOfWork _uow;

        public PaymentGatewayService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<PaymentGatewaySettingsDto> GetSettingsAsync()
        {
            var dto = new PaymentGatewaySettingsDto();

            // 1. COD
            var codEnabled = await _uow.AppSettings.GetByKeyAsync("Payment_COD_Enabled");
            if (codEnabled != null && bool.TryParse(codEnabled.Value, out bool ce)) dto.CodEnabled = ce;
            else dto.CodEnabled = true; // COD default active

            var codTitleAr = await _uow.AppSettings.GetByKeyAsync("Payment_COD_TitleAr");
            if (codTitleAr != null && !string.IsNullOrWhiteSpace(codTitleAr.Value)) dto.CodTitleAr = codTitleAr.Value;

            var codTitleEn = await _uow.AppSettings.GetByKeyAsync("Payment_COD_TitleEn");
            if (codTitleEn != null && !string.IsNullOrWhiteSpace(codTitleEn.Value)) dto.CodTitleEn = codTitleEn.Value;

            var codDescAr = await _uow.AppSettings.GetByKeyAsync("Payment_COD_DescAr");
            if (codDescAr != null && !string.IsNullOrWhiteSpace(codDescAr.Value)) dto.CodDescAr = codDescAr.Value;

            var codDescEn = await _uow.AppSettings.GetByKeyAsync("Payment_COD_DescEn");
            if (codDescEn != null && !string.IsNullOrWhiteSpace(codDescEn.Value)) dto.CodDescEn = codDescEn.Value;

            var codExtraFee = await _uow.AppSettings.GetByKeyAsync("Payment_COD_ExtraFee");
            if (codExtraFee != null && decimal.TryParse(codExtraFee.Value, out decimal cf)) dto.CodExtraFee = cf;

            // 2. CliQ
            var cliqEnabled = await _uow.AppSettings.GetByKeyAsync("Payment_CliQ_Enabled");
            if (cliqEnabled != null && bool.TryParse(cliqEnabled.Value, out bool cle)) dto.CliqEnabled = cle;
            else dto.CliqEnabled = false; // Default disabled until merchant config

            var cliqAlias = await _uow.AppSettings.GetByKeyAsync("Payment_CliQ_Alias");
            if (cliqAlias != null) dto.CliqAlias = cliqAlias.Value;

            var cliqBank = await _uow.AppSettings.GetByKeyAsync("Payment_CliQ_BankName");
            if (cliqBank != null && !string.IsNullOrWhiteSpace(cliqBank.Value)) dto.CliqBankName = cliqBank.Value;

            var cliqIban = await _uow.AppSettings.GetByKeyAsync("Payment_CliQ_Iban");
            if (cliqIban != null) dto.CliqIban = cliqIban.Value;

            var cliqEscrow = await _uow.AppSettings.GetByKeyAsync("Payment_CliQ_EscrowProtected");
            if (cliqEscrow != null && bool.TryParse(cliqEscrow.Value, out bool ces)) dto.CliqIsEscrowProtected = ces;

            var cliqTitleAr = await _uow.AppSettings.GetByKeyAsync("Payment_CliQ_TitleAr");
            if (cliqTitleAr != null && !string.IsNullOrWhiteSpace(cliqTitleAr.Value)) dto.CliqTitleAr = cliqTitleAr.Value;

            var cliqTitleEn = await _uow.AppSettings.GetByKeyAsync("Payment_CliQ_TitleEn");
            if (cliqTitleEn != null && !string.IsNullOrWhiteSpace(cliqTitleEn.Value)) dto.CliqTitleEn = cliqTitleEn.Value;

            var cliqDescAr = await _uow.AppSettings.GetByKeyAsync("Payment_CliQ_DescAr");
            if (cliqDescAr != null && !string.IsNullOrWhiteSpace(cliqDescAr.Value)) dto.CliqDescAr = cliqDescAr.Value;

            var cliqDescEn = await _uow.AppSettings.GetByKeyAsync("Payment_CliQ_DescEn");
            if (cliqDescEn != null && !string.IsNullOrWhiteSpace(cliqDescEn.Value)) dto.CliqDescEn = cliqDescEn.Value;

            // 3. eFAWATEERcom
            var efEnabled = await _uow.AppSettings.GetByKeyAsync("Payment_eFAWATEERcom_Enabled");
            if (efEnabled != null && bool.TryParse(efEnabled.Value, out bool efe)) dto.EfawateercomEnabled = efe;
            else dto.EfawateercomEnabled = false; // Default disabled until merchant config

            var efBillerCode = await _uow.AppSettings.GetByKeyAsync("Payment_eFAWATEERcom_BillerCode");
            if (efBillerCode != null) dto.EfawateercomBillerCode = efBillerCode.Value;

            var efService = await _uow.AppSettings.GetByKeyAsync("Payment_eFAWATEERcom_ServiceName");
            if (efService != null && !string.IsNullOrWhiteSpace(efService.Value)) dto.EfawateercomServiceName = efService.Value;

            var efTitleAr = await _uow.AppSettings.GetByKeyAsync("Payment_eFAWATEERcom_TitleAr");
            if (efTitleAr != null && !string.IsNullOrWhiteSpace(efTitleAr.Value)) dto.EfawateercomTitleAr = efTitleAr.Value;

            var efTitleEn = await _uow.AppSettings.GetByKeyAsync("Payment_eFAWATEERcom_TitleEn");
            if (efTitleEn != null && !string.IsNullOrWhiteSpace(efTitleEn.Value)) dto.EfawateercomTitleEn = efTitleEn.Value;

            var efDescAr = await _uow.AppSettings.GetByKeyAsync("Payment_eFAWATEERcom_DescAr");
            if (efDescAr != null && !string.IsNullOrWhiteSpace(efDescAr.Value)) dto.EfawateercomDescAr = efDescAr.Value;

            var efDescEn = await _uow.AppSettings.GetByKeyAsync("Payment_eFAWATEERcom_DescEn");
            if (efDescEn != null && !string.IsNullOrWhiteSpace(efDescEn.Value)) dto.EfawateercomDescEn = efDescEn.Value;

            // 4. Online Card Gateway
            var cardEnabled = await _uow.AppSettings.GetByKeyAsync("Payment_CardGateway_Enabled");
            if (cardEnabled != null && bool.TryParse(cardEnabled.Value, out bool cge)) dto.CardGatewayEnabled = cge;
            else dto.CardGatewayEnabled = false; // Default disabled until merchant config

            var cardProvider = await _uow.AppSettings.GetByKeyAsync("Payment_CardGateway_Provider");
            if (cardProvider != null && !string.IsNullOrWhiteSpace(cardProvider.Value)) dto.CardGatewayProvider = cardProvider.Value;

            var cardEnv = await _uow.AppSettings.GetByKeyAsync("Payment_CardGateway_Environment");
            if (cardEnv != null && !string.IsNullOrWhiteSpace(cardEnv.Value)) dto.CardGatewayEnvironment = cardEnv.Value;

            var cardMerchant = await _uow.AppSettings.GetByKeyAsync("Payment_CardGateway_MerchantId");
            if (cardMerchant != null) dto.CardGatewayMerchantId = cardMerchant.Value;

            var cardApiKey = await _uow.AppSettings.GetByKeyAsync("Payment_CardGateway_ApiKey");
            if (cardApiKey != null) dto.CardGatewayApiKey = cardApiKey.Value;

            var cardSecretKey = await _uow.AppSettings.GetByKeyAsync("Payment_CardGateway_SecretKey");
            if (cardSecretKey != null) dto.CardGatewaySecretKey = cardSecretKey.Value;

            var cardPublicKey = await _uow.AppSettings.GetByKeyAsync("Payment_CardGateway_PublicKey");
            if (cardPublicKey != null) dto.CardGatewayPublicKey = cardPublicKey.Value;

            var cardWebhook = await _uow.AppSettings.GetByKeyAsync("Payment_CardGateway_WebhookSecret");
            if (cardWebhook != null) dto.CardGatewayWebhookSecret = cardWebhook.Value;

            var cardBaseUrl = await _uow.AppSettings.GetByKeyAsync("Payment_CardGateway_BaseUrl");
            if (cardBaseUrl != null) dto.CardGatewayBaseUrl = cardBaseUrl.Value;

            var cardCurrency = await _uow.AppSettings.GetByKeyAsync("Payment_CardGateway_Currency");
            if (cardCurrency != null && !string.IsNullOrWhiteSpace(cardCurrency.Value)) dto.CardGatewayCurrency = cardCurrency.Value;

            var cardTitleAr = await _uow.AppSettings.GetByKeyAsync("Payment_CardGateway_TitleAr");
            if (cardTitleAr != null && !string.IsNullOrWhiteSpace(cardTitleAr.Value)) dto.CardGatewayTitleAr = cardTitleAr.Value;

            var cardTitleEn = await _uow.AppSettings.GetByKeyAsync("Payment_CardGateway_TitleEn");
            if (cardTitleEn != null && !string.IsNullOrWhiteSpace(cardTitleEn.Value)) dto.CardGatewayTitleEn = cardTitleEn.Value;

            var cardDescAr = await _uow.AppSettings.GetByKeyAsync("Payment_CardGateway_DescAr");
            if (cardDescAr != null && !string.IsNullOrWhiteSpace(cardDescAr.Value)) dto.CardGatewayDescAr = cardDescAr.Value;

            var cardDescEn = await _uow.AppSettings.GetByKeyAsync("Payment_CardGateway_DescEn");
            if (cardDescEn != null && !string.IsNullOrWhiteSpace(cardDescEn.Value)) dto.CardGatewayDescEn = cardDescEn.Value;

            return dto;
        }

        public async Task SaveSettingsAsync(PaymentGatewaySettingsDto s)
        {
            // 1. COD
            await SaveSettingAsync("Payment_COD_Enabled", s.CodEnabled ? "true" : "false", "تفعيل الدفع عند الاستلام");
            await SaveSettingAsync("Payment_COD_TitleAr", s.CodTitleAr, "عنوان الدفع عند الاستلام - عربي");
            await SaveSettingAsync("Payment_COD_TitleEn", s.CodTitleEn, "عنوان الدفع عند الاستلام - إنجليزي");
            await SaveSettingAsync("Payment_COD_DescAr", s.CodDescAr, "وصف الدفع عند الاستلام - عربي");
            await SaveSettingAsync("Payment_COD_DescEn", s.CodDescEn, "وصف الدفع عند الاستلام - إنجليزي");
            await SaveSettingAsync("Payment_COD_ExtraFee", s.CodExtraFee.ToString("F2"), "رسوم إضافية على الدفع عند الاستلام");

            // 2. CliQ
            await SaveSettingAsync("Payment_CliQ_Enabled", s.CliqEnabled ? "true" : "false", "تفعيل كليك CliQ");
            await SaveSettingAsync("Payment_CliQ_Alias", s.CliqAlias ?? "", "اسم مستعار كليك (CliQ Alias)");
            await SaveSettingAsync("Payment_CliQ_BankName", s.CliqBankName ?? "", "اسم البنك لكليك");
            await SaveSettingAsync("Payment_CliQ_Iban", s.CliqIban ?? "", "رقم الآيبان IBAN لكليك");
            await SaveSettingAsync("Payment_CliQ_EscrowProtected", s.CliqIsEscrowProtected ? "true" : "false", "تفعيل حماية حساب الضمان لكليك");
            await SaveSettingAsync("Payment_CliQ_TitleAr", s.CliqTitleAr, "عنوان كليك - عربي");
            await SaveSettingAsync("Payment_CliQ_TitleEn", s.CliqTitleEn, "عنوان كليك - إنجليزي");
            await SaveSettingAsync("Payment_CliQ_DescAr", s.CliqDescAr, "وصف كليك - عربي");
            await SaveSettingAsync("Payment_CliQ_DescEn", s.CliqDescEn, "وصف كليك - إنجليزي");

            // 3. eFAWATEERcom
            await SaveSettingAsync("Payment_eFAWATEERcom_Enabled", s.EfawateercomEnabled ? "true" : "false", "تفعيل إي فواتيركم");
            await SaveSettingAsync("Payment_eFAWATEERcom_BillerCode", s.EfawateercomBillerCode ?? "", "رمز المفوتر إي فواتيركم");
            await SaveSettingAsync("Payment_eFAWATEERcom_ServiceName", s.EfawateercomServiceName ?? "", "اسم الخدمة إي فواتيركم");
            await SaveSettingAsync("Payment_eFAWATEERcom_TitleAr", s.EfawateercomTitleAr, "عنوان إي فواتيركم - عربي");
            await SaveSettingAsync("Payment_eFAWATEERcom_TitleEn", s.EfawateercomTitleEn, "عنوان إي فواتيركم - إنجليزي");
            await SaveSettingAsync("Payment_eFAWATEERcom_DescAr", s.EfawateercomDescAr, "وصف إي فواتيركم - عربي");
            await SaveSettingAsync("Payment_eFAWATEERcom_DescEn", s.EfawateercomDescEn, "وصف إي فواتيركم - إنجليزي");

            // 4. Online Card Gateway
            await SaveSettingAsync("Payment_CardGateway_Enabled", s.CardGatewayEnabled ? "true" : "false", "تفعيل بوابة بطاقات الدفع الإلكتروني");
            await SaveSettingAsync("Payment_CardGateway_Provider", s.CardGatewayProvider ?? "Generic", "مزود بوابة الدفع");
            await SaveSettingAsync("Payment_CardGateway_Environment", s.CardGatewayEnvironment ?? "Sandbox", "بيئة تشغيل بوابة الدفع");
            await SaveSettingAsync("Payment_CardGateway_MerchantId", s.CardGatewayMerchantId ?? "", "معرف التاجر Merchant ID");
            await SaveSettingAsync("Payment_CardGateway_ApiKey", s.CardGatewayApiKey ?? "", "مفتاح الـ API Key");
            await SaveSettingAsync("Payment_CardGateway_SecretKey", s.CardGatewaySecretKey ?? "", "المفتاح السري Secret Key");
            await SaveSettingAsync("Payment_CardGateway_PublicKey", s.CardGatewayPublicKey ?? "", "المفتاح العام Public/Client Key");
            await SaveSettingAsync("Payment_CardGateway_WebhookSecret", s.CardGatewayWebhookSecret ?? "", "Webhook Secret Key");
            await SaveSettingAsync("Payment_CardGateway_BaseUrl", s.CardGatewayBaseUrl ?? "", "رابط الـ API الأساسي Base URL");
            await SaveSettingAsync("Payment_CardGateway_Currency", s.CardGatewayCurrency ?? "JOD", "عملة بوابة الدفع");
            await SaveSettingAsync("Payment_CardGateway_TitleAr", s.CardGatewayTitleAr, "عنوان بوابة البطاقات - عربي");
            await SaveSettingAsync("Payment_CardGateway_TitleEn", s.CardGatewayTitleEn, "عنوان بوابة البطاقات - إنجليزي");
            await SaveSettingAsync("Payment_CardGateway_DescAr", s.CardGatewayDescAr, "وصف بوابة البطاقات - عربي");
            await SaveSettingAsync("Payment_CardGateway_DescEn", s.CardGatewayDescEn, "وصف بوابة البطاقات - إنجليزي");

            await _uow.CompleteAsync();
        }

        public async Task<List<PaymentMethodOptionDto>> GetActivePaymentMethodsAsync(bool isArabic = true)
        {
            var s = await GetSettingsAsync();
            var list = new List<PaymentMethodOptionDto>();

            // 1. COD
            if (s.CodEnabled)
            {
                list.Add(new PaymentMethodOptionDto
                {
                    Code = "COD",
                    Title = isArabic ? s.CodTitleAr : s.CodTitleEn,
                    Description = isArabic ? s.CodDescAr : s.CodDescEn,
                    Icon = "local_atm",
                    ColorClass = "amber",
                    IsSelectedDefault = list.Count == 0,
                    BadgeText = isArabic ? "الدفع عند الاستلام" : "Cash on Delivery"
                });
            }

            // 2. CliQ
            if (s.CliqEnabled)
            {
                var extra = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(s.CliqAlias)) extra["Alias"] = s.CliqAlias;
                if (!string.IsNullOrEmpty(s.CliqIban)) extra["IBAN"] = s.CliqIban;

                list.Add(new PaymentMethodOptionDto
                {
                    Code = "CLIQ_ESCROW",
                    Title = isArabic ? s.CliqTitleAr : s.CliqTitleEn,
                    Description = isArabic ? s.CliqDescAr : s.CliqDescEn,
                    Icon = "security",
                    ColorClass = "emerald",
                    IsSelectedDefault = list.Count == 0,
                    BadgeText = isArabic ? "كليك - محمي بالضمان" : "CliQ Escrow",
                    ExtraData = extra
                });
            }

            // 3. eFAWATEERcom
            if (s.EfawateercomEnabled)
            {
                var extra = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(s.EfawateercomBillerCode)) extra["BillerCode"] = s.EfawateercomBillerCode;

                list.Add(new PaymentMethodOptionDto
                {
                    Code = "EFAWATEERCOM",
                    Title = isArabic ? s.EfawateercomTitleAr : s.EfawateercomTitleEn,
                    Description = isArabic ? s.EfawateercomDescAr : s.EfawateercomDescEn,
                    Icon = "receipt_long",
                    ColorClass = "blue",
                    IsSelectedDefault = list.Count == 0,
                    BadgeText = isArabic ? "إي فواتيركم" : "eFAWATEERcom",
                    ExtraData = extra
                });
            }

            // 4. Online Card Gateway
            if (s.CardGatewayEnabled)
            {
                list.Add(new PaymentMethodOptionDto
                {
                    Code = "ONLINE_CARD",
                    Title = isArabic ? s.CardGatewayTitleAr : s.CardGatewayTitleEn,
                    Description = isArabic ? s.CardGatewayDescAr : s.CardGatewayDescEn,
                    Icon = "credit_card",
                    ColorClass = "indigo",
                    IsSelectedDefault = list.Count == 0,
                    BadgeText = isArabic ? "بطاقة بنكية / أبل باي" : "Card / Apple Pay"
                });
            }

            // Defensive fallback: If admin disabled all, ensure COD is available
            if (list.Count == 0)
            {
                list.Add(new PaymentMethodOptionDto
                {
                    Code = "COD",
                    Title = isArabic ? s.CodTitleAr : s.CodTitleEn,
                    Description = isArabic ? s.CodDescAr : s.CodDescEn,
                    Icon = "local_atm",
                    ColorClass = "amber",
                    IsSelectedDefault = true,
                    BadgeText = isArabic ? "الدفع عند الاستلام" : "Cash on Delivery"
                });
            }

            return list;
        }

        public async Task<bool> IsPaymentMethodActiveAsync(string methodCode)
        {
            var s = await GetSettingsAsync();
            return methodCode switch
            {
                "COD" => s.CodEnabled,
                "CLIQ_ESCROW" => s.CliqEnabled,
                "EFAWATEERCOM" => s.EfawateercomEnabled,
                "ONLINE_CARD" => s.CardGatewayEnabled,
                _ => false
            };
        }

        private async Task SaveSettingAsync(string key, string value, string description)
        {
            var setting = await _uow.AppSettings.GetByKeyAsync(key);
            if (setting == null)
            {
                setting = new AppSetting
                {
                    Key = key,
                    Value = value ?? "",
                    Description = description,
                    LastUpdated = DateTime.UtcNow
                };
                await _uow.AppSettings.AddAsync(setting);
            }
            else
            {
                setting.Value = value ?? "";
                setting.LastUpdated = DateTime.UtcNow;
                _uow.AppSettings.Update(setting);
            }
        }
    }
}
