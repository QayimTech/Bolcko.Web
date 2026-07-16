using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Setting;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, DashboardUser")]
    public class SettingsController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly Bolcko.Domain.Interfaces.IUnitOfWork _uow;
        private readonly IWebHostEnvironment _env;

        public SettingsController(IServiceManager serviceManager, Bolcko.Domain.Interfaces.IUnitOfWork uow, IWebHostEnvironment env)
        {
            _serviceManager = serviceManager;
            _uow = uow;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var shippingFee = await _uow.AppSettings.GetByKeyAsync("ShippingFee");
            var contactEmail = await _uow.AppSettings.GetByKeyAsync("ContactEmail");
            var contactPhone = await _uow.AppSettings.GetByKeyAsync("ContactPhone");
            var contactAddress = await _uow.AppSettings.GetByKeyAsync("ContactAddress");

            // Load Notification Settings
            var soundEnabled = await _uow.AppSettings.GetByKeyAsync("NotificationSoundEnabled");
            var soundUrl = await _uow.AppSettings.GetByKeyAsync("NotificationSoundUrl");
            var emailEnabled = await _uow.AppSettings.GetByKeyAsync("NotificationEmailEnabled");

            // Load Home Hero Text Settings
            var heroTitleAr = await _uow.AppSettings.GetByKeyAsync("HomeHeroTitleAr");
            var heroDescAr = await _uow.AppSettings.GetByKeyAsync("HomeHeroDescAr");
            var heroTitleEn = await _uow.AppSettings.GetByKeyAsync("HomeHeroTitleEn");
            var heroDescEn = await _uow.AppSettings.GetByKeyAsync("HomeHeroDescEn");

            ViewBag.ShippingFee = shippingFee?.Value ?? "5.00";
            ViewBag.ContactEmail = contactEmail?.Value ?? "info@bolcko.com";
            ViewBag.ContactPhone = contactPhone?.Value ?? "+962 6 555 5555";
            ViewBag.ContactAddress = contactAddress?.Value ?? "عمان، الأردن";

            ViewBag.NotificationSoundEnabled = soundEnabled?.Value ?? "true";
            ViewBag.NotificationSoundUrl = soundUrl?.Value ?? "https://assets.mixkit.co/active_storage/sfx/2869/2869-600.wav";
            ViewBag.NotificationEmailEnabled = emailEnabled?.Value ?? "true";

            ViewBag.HomeHeroTitleAr = heroTitleAr?.Value ?? "الأساس المتين لكل مشروع عظيم.";
            ViewBag.HomeHeroDescAr = heroDescAr?.Value ?? "توريدات صناعية وإنشائية عالية الدقة للمقاولين المحترفين. من الخرسانة والمواد الخام إلى الأدوات الدقيقة، نوفر لك كل ما تحتاجه.";
            ViewBag.HomeHeroTitleEn = heroTitleEn?.Value ?? "Your Digital Platform for Construction Supplies in Jordan.";
            ViewBag.HomeHeroDescEn = heroDescEn?.Value ?? "A digital platform connecting contractors and engineers with high-quality building materials — transparent pricing and direct delivery to your job site.";

            // Load Legal Pages Contents
            var legalPrivacyAr = await _uow.AppSettings.GetByKeyAsync("LegalPrivacyContentAr");
            var legalPrivacyEn = await _uow.AppSettings.GetByKeyAsync("LegalPrivacyContentEn");
            var legalTermsAr = await _uow.AppSettings.GetByKeyAsync("LegalTermsContentAr");
            var legalTermsEn = await _uow.AppSettings.GetByKeyAsync("LegalTermsContentEn");
            var legalSupportAr = await _uow.AppSettings.GetByKeyAsync("LegalSupportContentAr");
            var legalSupportEn = await _uow.AppSettings.GetByKeyAsync("LegalSupportContentEn");

            ViewBag.LegalPrivacyContentAr = legalPrivacyAr?.Value ?? "نحن في BLOCKO نقدر خصوصيتك ونلتزم بحماية بياناتك الشخصية...";
            ViewBag.LegalPrivacyContentEn = legalPrivacyEn?.Value ?? "We at BLOCKO value your privacy and commit to protecting your personal data...";
            ViewBag.LegalTermsContentAr = legalTermsAr?.Value ?? "باستخدامك لمنصة BLOCKO، فإنك توافق على الشروط والأحكام التالية...";
            ViewBag.LegalTermsContentEn = legalTermsEn?.Value ?? "By using BLOCKO platform, you agree to the following terms and conditions...";
            ViewBag.LegalSupportContentAr = legalSupportAr?.Value ?? "فريق الدعم الفني لدينا متاح للإجابة على جميع استفساراتك المتعلقة بالموقع...";
            ViewBag.LegalSupportContentEn = legalSupportEn?.Value ?? "Our technical support team is available to answer all your inquiries regarding the site...";

            // Load Market Info Settings
            var mCityAr = await _uow.AppSettings.GetByKeyAsync("MarketCityAr");
            var mCityEn = await _uow.AppSettings.GetByKeyAsync("MarketCityEn");
            var mCountryAr = await _uow.AppSettings.GetByKeyAsync("MarketCountryAr");
            var mCountryEn = await _uow.AppSettings.GetByKeyAsync("MarketCountryEn");
            var mIsOnlineOnly = await _uow.AppSettings.GetByKeyAsync("MarketIsOnlineOnly");
            var mCurrencyAr = await _uow.AppSettings.GetByKeyAsync("MarketCurrencyAr");
            var mCurrencyEn = await _uow.AppSettings.GetByKeyAsync("MarketCurrencyEn");

            ViewBag.MarketCityAr = mCityAr?.Value ?? "عمان";
            ViewBag.MarketCityEn = mCityEn?.Value ?? "Amman";
            ViewBag.MarketCountryAr = mCountryAr?.Value ?? "الأردن";
            ViewBag.MarketCountryEn = mCountryEn?.Value ?? "Jordan";
            ViewBag.MarketIsOnlineOnly = mIsOnlineOnly?.Value ?? "true";
            ViewBag.MarketCurrencyAr = mCurrencyAr?.Value ?? "دينار أردني";
            ViewBag.MarketCurrencyEn = mCurrencyEn?.Value ?? "JOD";

            var rates = await _uow.ShippingRates.GetAllAsync();
            
            // Seed defaults if table is empty
            if (!rates.Any())
            {
                var amman = new ShippingRate { CityName = "عمان", Rate = 3.00m };
                var zarqa = new ShippingRate { CityName = "الزرقاء", Rate = 4.00m };
                var irbid = new ShippingRate { CityName = "إربد", Rate = 5.00m };
                await _uow.ShippingRates.AddAsync(amman);
                await _uow.ShippingRates.AddAsync(zarqa);
                await _uow.ShippingRates.AddAsync(irbid);
                await _uow.CompleteAsync();
                rates = await _uow.ShippingRates.GetAllAsync();
            }

            ViewBag.ShippingRates = rates.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(
            string shippingFee, 
            string contactEmail, 
            string contactPhone, 
            string contactAddress, 
            bool notificationSoundEnabled,
            string notificationSoundUrl,
            IFormFile? notificationSoundFile,
            bool notificationEmailEnabled,
            string homeHeroTitleAr,
            string homeHeroDescAr,
            string homeHeroTitleEn,
            string homeHeroDescEn,
            string legalPrivacyContentAr,
            string legalPrivacyContentEn,
            string legalTermsContentAr,
            string legalTermsContentEn,
            string legalSupportContentAr,
            string legalSupportContentEn,
            string marketCityAr,
            string marketCityEn,
            string marketCountryAr,
            string marketCountryEn,
            bool marketIsOnlineOnly,
            string marketCurrencyAr,
            string marketCurrencyEn,
            Dictionary<string, decimal> cityRates)
        {
            await SaveSettingAsync("ShippingFee", shippingFee, "رسوم الشحن والتوصيل المقدرة");
            await SaveSettingAsync("ContactEmail", contactEmail, "بريد التواصل الإلكتروني الأساسي");
            await SaveSettingAsync("ContactPhone", contactPhone, "رقم هاتف التواصل الأساسي");
            await SaveSettingAsync("ContactAddress", contactAddress, "عنوان المقر الأساسي للتواصل");

            // Save Home Hero Text
            await SaveSettingAsync("HomeHeroTitleAr", homeHeroTitleAr, "عنوان الهيرو الرئيسي - عربي");
            await SaveSettingAsync("HomeHeroDescAr", homeHeroDescAr, "وصف الهيرو الرئيسي - عربي");
            await SaveSettingAsync("HomeHeroTitleEn", homeHeroTitleEn, "عنوان الهيرو الرئيسي - إنجليزي");
            await SaveSettingAsync("HomeHeroDescEn", homeHeroDescEn, "وصف الهيرو الرئيسي - إنجليزي");

            // Save Legal Content
            await SaveSettingAsync("LegalPrivacyContentAr", legalPrivacyContentAr, "محتوى صفحة سياسة الخصوصية - عربي");
            await SaveSettingAsync("LegalPrivacyContentEn", legalPrivacyContentEn, "محتوى صفحة سياسة الخصوصية - إنجليزي");
            await SaveSettingAsync("LegalTermsContentAr", legalTermsContentAr, "محتوى صفحة شروط الخدمة - عربي");
            await SaveSettingAsync("LegalTermsContentEn", legalTermsContentEn, "محتوى صفحة شروط الخدمة - إنجليزي");
            await SaveSettingAsync("LegalSupportContentAr", legalSupportContentAr, "محتوى صفحة الدعم الفني - عربي");
            await SaveSettingAsync("LegalSupportContentEn", legalSupportContentEn, "محتوى صفحة الدعم الفني - إنجليزي");

            // Save Market Info
            await SaveSettingAsync("MarketCityAr", marketCityAr, "المدينة - عربي");
            await SaveSettingAsync("MarketCityEn", marketCityEn, "المدينة - إنجليزي");
            await SaveSettingAsync("MarketCountryAr", marketCountryAr, "البلد - عربي");
            await SaveSettingAsync("MarketCountryEn", marketCountryEn, "البلد - إنجليزي");
            await SaveSettingAsync("MarketIsOnlineOnly", marketIsOnlineOnly ? "true" : "false", "طريقة العرض (منصة إلكترونية فقط)");
            await SaveSettingAsync("MarketCurrencyAr", marketCurrencyAr, "العملة المعتمدة - عربي");
            await SaveSettingAsync("MarketCurrencyEn", marketCurrencyEn, "العملة المعتمدة - إنجليزي");

            string finalSoundUrl = notificationSoundUrl ?? "/sounds/default-notification.wav";

            // Handle file upload
            if (notificationSoundFile != null && notificationSoundFile.Length > 0)
            {
                var soundsFolder = Path.Combine(_env.WebRootPath, "sounds");
                if (!Directory.Exists(soundsFolder))
                {
                    Directory.CreateDirectory(soundsFolder);
                }

                var ext = Path.GetExtension(notificationSoundFile.FileName);
                var fileName = $"notification-sound-{Guid.NewGuid().ToString().Substring(0, 8)}{ext}";
                var filePath = Path.Combine(soundsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await notificationSoundFile.CopyToAsync(stream);
                }

                finalSoundUrl = $"/sounds/{fileName}";
            }

            // Save Notification Settings
            await SaveSettingAsync("NotificationSoundEnabled", notificationSoundEnabled ? "true" : "false", "تشغيل التنبيه الصوتي للإشعارات");
            await SaveSettingAsync("NotificationSoundUrl", finalSoundUrl, "رابط ملف الصوت للتنبيهات");
            await SaveSettingAsync("NotificationEmailEnabled", notificationEmailEnabled ? "true" : "false", "إرسال إشعارات البريد لشركات التوصيل");

            // Sync dynamic shipping rates
            var existingRates = await _uow.ShippingRates.GetAllAsync();
            var incomingCities = cityRates != null ? cityRates.Keys.ToList() : new List<string>();

            // 1. Delete removed cities
            foreach (var existing in existingRates)
            {
                if (!incomingCities.Contains(existing.CityName, StringComparer.OrdinalIgnoreCase))
                {
                    _uow.ShippingRates.Remove(existing);
                }
            }

            // 2. Add or Update
            if (cityRates != null)
            {
                foreach (var r in cityRates)
                {
                    var rateObj = await _uow.ShippingRates.GetByCityNameAsync(r.Key);
                    if (rateObj != null)
                    {
                        rateObj.Rate = r.Value;
                        _uow.ShippingRates.Update(rateObj);
                    }
                    else
                    {
                        var newRateObj = new ShippingRate { CityName = r.Key, Rate = r.Value };
                        await _uow.ShippingRates.AddAsync(newRateObj);
                    }
                }
            }
            await _uow.CompleteAsync();

            TempData["SuccessMessage"] = "تم حفظ الإعدادات وتحديث تكاليف شحن المحافظات بنجاح!";
            return RedirectToAction(nameof(Index));
        }

        private async Task SaveSettingAsync(string key, string value, string description)
        {
            var setting = await _uow.AppSettings.GetByKeyAsync(key);
            if (setting == null)
            {
                setting = new AppSetting
                {
                    Key = key,
                    Value = value,
                    Description = description,
                    LastUpdated = DateTime.UtcNow
                };
                await _uow.AppSettings.AddAsync(setting);
            }
            else
            {
                setting.Value = value;
                setting.LastUpdated = DateTime.UtcNow;
                _uow.AppSettings.Update(setting);
            }
            await _uow.CompleteAsync();
        }
    }
}
