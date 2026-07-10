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

        public SettingsController(IServiceManager serviceManager, Bolcko.Domain.Interfaces.IUnitOfWork uow)
        {
            _serviceManager = serviceManager;
            _uow = uow;
        }

        public async Task<IActionResult> Index()
        {
            var shippingFee = await _uow.AppSettings.GetByKeyAsync("ShippingFee");
            var contactEmail = await _uow.AppSettings.GetByKeyAsync("ContactEmail");
            var contactPhone = await _uow.AppSettings.GetByKeyAsync("ContactPhone");
            var contactAddress = await _uow.AppSettings.GetByKeyAsync("ContactAddress");

            ViewBag.ShippingFee = shippingFee?.Value ?? "5.00";
            ViewBag.ContactEmail = contactEmail?.Value ?? "info@bolcko.com";
            ViewBag.ContactPhone = contactPhone?.Value ?? "+962 6 555 5555";
            ViewBag.ContactAddress = contactAddress?.Value ?? "عمان، الأردن";

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
        public async Task<IActionResult> Save(string shippingFee, string contactEmail, string contactPhone, string contactAddress, Dictionary<string, decimal> cityRates)
        {
            await SaveSettingAsync("ShippingFee", shippingFee, "رسوم الشحن والتوصيل المقدرة");
            await SaveSettingAsync("ContactEmail", contactEmail, "بريد التواصل الإلكتروني الأساسي");
            await SaveSettingAsync("ContactPhone", contactPhone, "رقم هاتف التواصل الأساسي");
            await SaveSettingAsync("ContactAddress", contactAddress, "عنوان المقر الأساسي للتواصل");

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
