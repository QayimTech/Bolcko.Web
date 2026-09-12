using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Interfaces;
using Bolcko.Domain.Entities.Catalog.DTOs;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    public class CalculatorController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly IUnitOfWork _uow;

        public CalculatorController(IServiceManager serviceManager, IUnitOfWork uow)
        {
            _serviceManager = serviceManager;
            _uow = uow;
        }

        [HttpGet]
        [Route("calculator")]
        [Route("Shop/Calculator")]
        [Route("Shop/Calculator/Index")]
        public async Task<IActionResult> Index()
        {
            var defaultReq = new ConstructionEstimateRequestDto
            {
                BuiltUpAreaSquareMeters = 250,
                NumberOfFloors = 2,
                BuildingType = "Residential",
                FoundationType = "IsolatedFootings",
                City = "Amman",
                ColumnBarsCount = "6Bars",
                RebarDensityGrade = "Standard",
                SlabSystem = "Ribbed",
                EnableStoneModule = true,
                StoneFacadesCount = 4,
                StoneType = "Natural_Ruwaished",
                StoneFinish = "Mufajjar",
                IncludeCorniceBelt = true,
                WindowFramesCount = 8,
                EntranceColumnsCount = 2,
                HybridArtificialTrim = false,
                IncludeStoneInstallation = true
            };

            var estimate = await _serviceManager.MarketPriceService.CalculateEstimateAsync(defaultReq);
            ViewBag.Prices = await _serviceManager.MarketPriceService.GetMarketPricesDtoAsync();
            ViewBag.FAQs = await _serviceManager.FAQService.GetActiveFAQsByPageAsync("Calculator");
            ViewBag.DefaultReq = defaultReq;
            ViewBag.CalculatorConfig = await _serviceManager.MarketPriceService.GetCalculatorConfigurationAsync();

            return View(estimate);
        }

        [HttpPost]
        [Route("Shop/Calculator/OrderSampleBox")]
        public async Task<IActionResult> OrderSampleBox([FromBody] SampleBoxRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Phone))
            {
                return Json(new { success = false, message = "يرجى تعبئة الاسم ورقم الهاتف وموقع التوصيل للمتابعة." });
            }

            var trackingCode = "SMP-" + DateTime.UtcNow.ToString("yyMMdd") + "-" + Random.Shared.Next(1000, 9999);

            var tender = new Bolcko.Domain.Entities.Tender.Tender
            {
                TenderTitle = $"طلب صندوق عينة حجر معتمد ({dto.StoneType}) - {trackingCode}",
                TenderDescription = $"طلب صندوق عينة حجر 15×15 سم معتمد مع باركود فحص جودة.\n" +
                                  $"نوع الحجر: {dto.StoneType}\n" +
                                  $"النقشة: {dto.StoneFinish}\n" +
                                  $"المستلم: {dto.FullName} ({dto.Phone})\n" +
                                  $"الموقع: {dto.City} - {dto.DeliveryAddress}\n" +
                                  $"كلفة العينة: {dto.SamplePriceJod:N2} د.أ (مستردة بالكامل وتُخصم من الفاتورة النهائية)\n" +
                                  $"نظام الحماية: BLOCKO Escrow Quality Protection",
                GuestName = dto.FullName,
                GuestPhone = dto.Phone,
                GuestCity = dto.City,
                RequestDate = DateTime.UtcNow,
                Status = Bolcko.Domain.Enums.TenderStatus.Pending,
                TotalQuotedAmount = dto.SamplePriceJod,
                Notes = $"TrackingCode: {trackingCode} | Type: StoneSampleBox | EscrowProtected: True"
            };

            tender.Items.Add(new Bolcko.Domain.Entities.Tender.TenderItem
            {
                ProductName = $"صندوق عينة حجر معتمد ({dto.StoneType} - {dto.StoneFinish})",
                RequestedQuantity = 1,
                Unit = "عينة 15×15 سم",
                ProposedPricePerUnit = dto.SamplePriceJod,
                SubtotalItem = dto.SamplePriceJod
            });

            await _uow.Tenders.AddAsync(tender);
            await _uow.SaveChangesAsync();

            return Json(new
            {
                success = true,
                trackingCode = trackingCode,
                stoneType = dto.StoneType,
                message = "تم تسجيل طلب صندوق العينة بنجاح! سيتواصل معك قسم الفحص والجودة لتنسيق توصيل العينة لموقعك."
            });
        }

        [HttpPost]
        [Route("Shop/Calculator/RequestShowroomPass")]
        public async Task<IActionResult> RequestShowroomPass([FromBody] ShowroomPassRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Phone))
            {
                return Json(new { success = false, message = "يرجى تعبئة الاسم ورقم الهاتف لإصدار تصريح المعاينة." });
            }

            var passNumber = "PASS-" + DateTime.UtcNow.ToString("yyMMdd") + "-" + Random.Shared.Next(1000, 9999);

            var tender = new Bolcko.Domain.Entities.Tender.Tender
            {
                TenderTitle = $"تذكرة معاينة معرض حجر مشفرة ({dto.StoneType}) - {passNumber}",
                TenderDescription = $"تصريح رسمي لمعاينة ساحات ومعارض الحجر المعتمدة لدى بلكو.\n" +
                                  $"رقم التذكرة: {passNumber}\n" +
                                  $"الاسم: {dto.FullName} ({dto.Phone})\n" +
                                  $"المدينة: {dto.City}\n" +
                                  $"تاريخ الزيارة التقديري: {dto.PreferredDate:yyyy-MM-dd}\n" +
                                  $"المساحة المتوقعة: {dto.EstimatedAreaM2} م²\n" +
                                  $"الضمان: الدفع والتعاقد يتم حصراً عبر حساب الضمان (BLOCKO Escrow) للاستفادة من الكفالة والأسعار التفضيلية.",
                GuestName = dto.FullName,
                GuestPhone = dto.Phone,
                GuestCity = dto.City,
                RequestDate = DateTime.UtcNow,
                Status = Bolcko.Domain.Enums.TenderStatus.Pending,
                Notes = $"PassNumber: {passNumber} | Type: ShowroomPass | EscrowGuaranteed: True"
            };

            await _uow.Tenders.AddAsync(tender);
            await _uow.SaveChangesAsync();

            return Json(new
            {
                success = true,
                passNumber = passNumber,
                preferredDate = dto.PreferredDate.ToString("yyyy-MM-dd"),
                message = "تم إصدار تذكرة المعاينة المشفرة بنجاح! يمكنك إبراز الـ QR Code عند زيارة المعرض."
            });
        }
    }
}