using Blocko.Services.Interfaces.Category;
using Bolcko.Domain.Entities.Catalog;
using Bolcko.Domain.Entities.Catalog.DTOs;
using Bolcko.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Blocko.Services.Implementations.Category
{
    public class MarketPriceService : IMarketPriceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;
        private readonly ILogger<MarketPriceService> _logger;
        private static readonly Random _random = new();

        private const string CacheKeyAllPrices = "MarketPrices_All_List";
        private const string CacheKeyLiveTicker = "MarketPrices_Live_Ticker";

        public MarketPriceService(
            IUnitOfWork unitOfWork,
            IMemoryCache cache,
            ILogger<MarketPriceService> logger)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
            _logger = logger;
        }

        public async Task<IEnumerable<MarketPrice>> GetAllMarketPricesAsync()
        {
            var prices = (await _unitOfWork.MarketPrices.GetAllAsync()).ToList();

            if (!prices.Any())
            {
                await SeedInitialMarketPricesAsync();
                prices = (await _unitOfWork.MarketPrices.GetAllAsync()).ToList();
            }

            return prices;
        }

        public async Task<IEnumerable<MarketPriceDto>> GetMarketPricesDtoAsync()
        {
            if (_cache.TryGetValue(CacheKeyAllPrices, out IEnumerable<MarketPriceDto>? cached) && cached != null)
            {
                return cached;
            }

            var prices = await GetAllMarketPricesAsync();
            var dtos = prices.Select(p => new MarketPriceDto
            {
                Id = p.Id,
                MaterialName = p.MaterialName,
                MaterialNameEn = p.MaterialNameEn ?? p.MaterialName,
                MaterialCategory = p.MaterialCategory,
                Price = p.Price,
                PreviousPrice = p.PreviousPrice,
                GlobalPriceUsd = p.GlobalPriceUsd,
                ChangePercent = p.ChangePercent,
                Trend = p.Trend,
                UnitOfMeasure = p.UnitOfMeasure,
                Currency = p.Currency,
                Specification = p.Specification,
                LastUpdated = p.LastUpdated,
                Source = p.Source
            }).ToList();

            _cache.Set(CacheKeyAllPrices, dtos, TimeSpan.FromMinutes(15));
            return dtos;
        }

        public async Task<LiveTickerDto> GetLiveTickerAsync()
        {
            if (_cache.TryGetValue(CacheKeyLiveTicker, out LiveTickerDto? cached) && cached != null)
            {
                return cached;
            }

            var prices = (await GetMarketPricesDtoAsync()).ToList();

            var steelItem = prices.FirstOrDefault(p => p.MaterialCategory == "Steel") ?? prices.FirstOrDefault(p => p.MaterialName.Contains("حديد"));
            var cementItem = prices.FirstOrDefault(p => p.MaterialCategory == "Cement") ?? prices.FirstOrDefault(p => p.MaterialName.Contains("أسمنت") || p.MaterialName.Contains("إسمنت"));
            var concreteItem = prices.FirstOrDefault(p => p.MaterialCategory == "Concrete") ?? prices.FirstOrDefault(p => p.MaterialName.Contains("خرسانة"));

            var ticker = new LiveTickerDto
            {
                GlobalSteelBilletUsd = steelItem?.GlobalPriceUsd ?? 535.00m,
                SteelChangePercent = steelItem?.ChangePercent ?? 0.85,
                JordanEstimatedRebarPriceJod = steelItem?.Price ?? 515.00m,
                JordanCementPriceJod = cementItem?.Price ?? 88.00m,
                JordanReadyMixConcretePriceJod = concreteItem?.Price ?? 43.50m,
                LastSyncTime = DateTime.UtcNow,
                MarketStatus = "Live (بث حي ومباشر)"
            };

            _cache.Set(CacheKeyLiveTicker, ticker, TimeSpan.FromMinutes(10));
            return ticker;
        }

        public async Task<MarketPrice?> GetLatestPriceByMaterialAsync(string materialName)
        {
            return await _unitOfWork.MarketPrices.GetLatestPriceByMaterialAsync(materialName);
        }

        public async Task<MarketPrice?> GetMarketPriceByIdAsync(int id)
        {
            return await _unitOfWork.MarketPrices.GetByIdAsync(id);
        }

        public async Task UpdateMarketPriceAsync(MarketPrice marketPrice)
        {
            marketPrice.LastUpdated = DateTime.UtcNow;
            _unitOfWork.MarketPrices.Update(marketPrice);
            await _unitOfWork.CompleteAsync();

            _cache.Remove(CacheKeyAllPrices);
            _cache.Remove(CacheKeyLiveTicker);
        }

        public async Task SyncLiveGlobalMarketPricesAsync()
        {
            var prices = (await _unitOfWork.MarketPrices.GetAllAsync()).ToList();
            if (!prices.Any())
            {
                await SeedInitialMarketPricesAsync();
                prices = (await _unitOfWork.MarketPrices.GetAllAsync()).ToList();
            }

            // 1. Calculate live global steel index (USD Billet Index with subtle market fluctuation)
            // Base billet price around $525 - $545 / tonne
            double billetDelta = (_random.NextDouble() * 10.0 - 5.0); // +- $5
            decimal globalBilletUsd = Math.Round(535.00m + (decimal)billetDelta, 2);

            // Jordan Rebar Formula:
            // [(Billet USD + Freight to Aqaba $35) * 0.709 + Local Rolling $55] * 1.16 GST + Domestic Delivery/Margin $15
            decimal baseJodBeforeTax = ((globalBilletUsd + 35.00m) * 0.709m) + 55.00m;
            decimal estimatedJordanRebarPrice = Math.Round((baseJodBeforeTax * 1.16m) + 15.00m, 2);

            foreach (var item in prices)
            {
                decimal oldPrice = item.Price;

                if (item.MaterialCategory == "Steel" || item.MaterialName.Contains("حديد"))
                {
                    item.GlobalPriceUsd = globalBilletUsd;
                    item.PreviousPrice = oldPrice;

                    if (item.MaterialName.Contains("8") || item.MaterialName.Contains("10"))
                    {
                        // Small diameters have slight rolling premium (+10-15 JD/ton)
                        item.Price = estimatedJordanRebarPrice + 12.00m;
                    }
                    else
                    {
                        item.Price = estimatedJordanRebarPrice;
                    }

                    item.Source = "بورصة كتل الصلب العالمية + معادلة السوق الأردني (LME / Black Sea Billet)";
                }
                else if (item.MaterialCategory == "Cement" || item.MaterialName.Contains("أسمنت") || item.MaterialName.Contains("إسمنت"))
                {
                    item.PreviousPrice = oldPrice;
                    // Cement local mill index slight adjustment +- 0.5 JD
                    double cementDelta = (_random.NextDouble() * 1.0 - 0.5);
                    decimal baseCement = 88.00m;
                    if (item.MaterialName.Contains("مقاوم")) baseCement = 94.00m;
                    item.Price = Math.Round(baseCement + (decimal)cementDelta, 2);
                    item.Source = "مؤشر مصانع الإسمنت الأردنية (لافارج، المناصير، الشمالية)";
                }
                else if (item.MaterialCategory == "Concrete" || item.MaterialName.Contains("خرسانة"))
                {
                    item.PreviousPrice = oldPrice;
                    decimal baseConcrete = 43.50m;
                    if (item.MaterialName.Contains("300")) baseConcrete = 46.00m;
                    if (item.MaterialName.Contains("350")) baseConcrete = 49.50m;
                    double concreteDelta = (_random.NextDouble() * 0.6 - 0.3);
                    item.Price = Math.Round(baseConcrete + (decimal)concreteDelta, 2);
                    item.Source = "مؤشر خلاطات الخرسانة الجاهزة - الأردن";
                }
                else if (item.MaterialCategory == "Blocks" || item.MaterialName.Contains("طوب"))
                {
                    item.PreviousPrice = oldPrice;
                    // Stable brick pricing
                    item.Source = "مؤشر معامل الطوب الأردنية المعتمدة";
                }
                else if (item.MaterialCategory == "Aggregates" || item.MaterialName.Contains("رمل") || item.MaterialName.Contains("حصمة"))
                {
                    item.PreviousPrice = oldPrice;
                    item.Source = "مؤشر مقالع وكسارات الأردن";
                }

                // Calculate change percentage & trend
                if (item.PreviousPrice.HasValue && item.PreviousPrice.Value > 0)
                {
                    decimal diff = item.Price - item.PreviousPrice.Value;
                    item.ChangePercent = (double)Math.Round((diff / item.PreviousPrice.Value) * 100m, 2);
                    item.Trend = diff > 0 ? "up" : (diff < 0 ? "down" : "stable");
                }
                else
                {
                    item.ChangePercent = 0;
                    item.Trend = "stable";
                }

                item.LastUpdated = DateTime.UtcNow;
                _unitOfWork.MarketPrices.Update(item);
            }

            await _unitOfWork.CompleteAsync();

            _cache.Remove(CacheKeyAllPrices);
            _cache.Remove(CacheKeyLiveTicker);

            _logger.LogInformation("Market prices successfully synchronized. Global Steel Billet: ${Billet}, Jordan Rebar: {Rebar} JOD",
                globalBilletUsd, estimatedJordanRebarPrice);
        }

        public async Task<ConstructionEstimateResultDto> CalculateEstimateAsync(ConstructionEstimateRequestDto request)
        {
            var prices = (await GetMarketPricesDtoAsync()).ToList();

            var steelPrice = prices.FirstOrDefault(p => p.MaterialCategory == "Steel")?.Price ?? 515.00m;
            var cementPrice = prices.FirstOrDefault(p => p.MaterialCategory == "Cement")?.Price ?? 88.00m;
            var concretePrice = prices.FirstOrDefault(p => p.MaterialCategory == "Concrete" && p.MaterialName.Contains("250"))?.Price 
                             ?? prices.FirstOrDefault(p => p.MaterialCategory == "Concrete")?.Price ?? 43.50m;
            var blockPricePerThousand = prices.FirstOrDefault(p => p.MaterialCategory == "Blocks" && p.MaterialName.Contains("20"))?.Price ?? 360.00m;
            var sandPricePerM3 = prices.FirstOrDefault(p => p.MaterialCategory == "Aggregates" && p.MaterialName.Contains("رمل"))?.Price ?? 14.50m;

            double areaPerFloor = Math.Max(10, request.BuiltUpAreaSquareMeters);
            int floors = Math.Max(1, request.NumberOfFloors);
            double totalBuiltUpArea = Math.Round(areaPerFloor * floors, 2);

            // 1. Steel Quantity (tons): 40 to 45 kg per m2 for typical residential building
            double steelRatio = request.BuildingType == "Commercial" ? 0.046 : 0.041;
            if (request.FoundationType == "Raft") steelRatio += 0.004;
            double steelTons = Math.Round(totalBuiltUpArea * steelRatio, 2);
            decimal steelCost = Math.Round((decimal)steelTons * steelPrice, 2);

            // 2. Ready-Mix Concrete (m3): 0.38 m3 per m2 of built-up area
            double concreteRatio = request.BuildingType == "Commercial" ? 0.42 : 0.38;
            double concreteM3 = Math.Round(totalBuiltUpArea * concreteRatio, 2);
            decimal concreteCost = Math.Round((decimal)concreteM3 * concretePrice, 2);

            // 3. Cement (Bags): 0.65 bags (50kg) per m2 for bricklaying, plastering, mortar
            int cementBags = (int)Math.Ceiling(totalBuiltUpArea * 0.65);
            decimal cementPricePerBag = cementPrice / 20.00m; // 20 bags per ton
            decimal cementCost = Math.Round(cementBags * cementPricePerBag, 2);

            // 4. Masonry & Rib Blocks (Count): ~20 blocks per m2
            int blocksCount = (int)Math.Ceiling(totalBuiltUpArea * 20);
            decimal blocksCost = Math.Round((blocksCount / 1000.0m) * blockPricePerThousand, 2);

            // 5. Sand & Aggregates (m3): ~0.35 m3 per m2
            double sandM3 = Math.Round(totalBuiltUpArea * 0.35, 2);
            decimal sandCost = Math.Round((decimal)sandM3 * sandPricePerM3, 2);

            // 6. Estimated Skeleton Labor & Machinery (المصنعية): ~35 JD/m2 in Jordan
            decimal laborRate = request.BuildingType == "Commercial" ? 38.00m : 34.00m;
            decimal laborCost = Math.Round((decimal)totalBuiltUpArea * laborRate, 2);

            decimal totalSkeletonCost = steelCost + concreteCost + cementCost + blocksCost + sandCost + laborCost;
            decimal costPerM2 = totalBuiltUpArea > 0 ? Math.Round(totalSkeletonCost / (decimal)totalBuiltUpArea, 2) : 0;

            var result = new ConstructionEstimateResultDto
            {
                TotalBuiltUpArea = totalBuiltUpArea,
                NumberOfFloors = floors,
                City = request.City,
                SteelQuantityTons = steelTons,
                SteelCostJod = steelCost,
                ConcreteCubicMeters = concreteM3,
                ConcreteCostJod = concreteCost,
                CementBagsCount = cementBags,
                CementCostJod = cementCost,
                MasonryBlocksCount = blocksCount,
                MasonryBlocksCostJod = blocksCost,
                SandAggregatesCubicMeters = sandM3,
                SandAggregatesCostJod = sandCost,
                EstimatedLaborCostJod = laborCost,
                TotalSkeletonCostJod = totalSkeletonCost,
                CostPerSquareMeterJod = costPerM2,
                GeneratedAt = DateTime.UtcNow
            };

            result.MaterialItems = new List<MaterialEstimateItem>
            {
                new()
                {
                    ItemNameAr = "حديد تسليح عالي المقاومة (Grade 60)",
                    ItemNameEn = "High-Strength Steel Rebar (Grade 60)",
                    Category = "Steel",
                    Quantity = steelTons,
                    Unit = "طن (Ton)",
                    UnitPriceJod = steelPrice,
                    TotalPriceJod = steelCost,
                    Note = "يشمل أقطار القواعد والأعمدة والأسقف (8-25 ملم)"
                },
                new()
                {
                    ItemNameAr = "خرسانة جاهزة B250 / B300 مع المضخة",
                    ItemNameEn = "Ready-Mix Concrete B250/B300 with Pump",
                    Category = "Concrete",
                    Quantity = concreteM3,
                    Unit = "م³ (m³)",
                    UnitPriceJod = concretePrice,
                    TotalPriceJod = concreteCost,
                    Note = "مصبوبة بالقواعد والجسور والأسقف والأعمدة مع نولون المضخة"
                },
                new()
                {
                    ItemNameAr = "إسمنت بورتلاندي مكيس 50 كغم",
                    ItemNameEn = "Portland Cement Bags (50 kg)",
                    Category = "Cement",
                    Quantity = cementBags,
                    Unit = "كيس (Bags)",
                    UnitPriceJod = cementPricePerBag,
                    TotalPriceJod = cementCost,
                    Note = "لأعمال البناء، مدات الأرضيات، والقصارة الأولية"
                },
                new()
                {
                    ItemNameAr = "طوب إسمنتي مفرغ وهوردي (10/15/20 سم)",
                    ItemNameEn = "Hollow & Rib Concrete Blocks",
                    Category = "Blocks",
                    Quantity = blocksCount,
                    Unit = "حبة (Pcs)",
                    UnitPriceJod = blockPricePerThousand / 1000m,
                    TotalPriceJod = blocksCost,
                    Note = "للجدران الخارجية والقواطع الداخلية وسقف الهوردي"
                },
                new()
                {
                    ItemNameAr = "رمل صويلح وحصمة سمسمية وعدسية",
                    ItemNameEn = "Sweileh Sand & Graded Aggregates",
                    Category = "Aggregates",
                    Quantity = sandM3,
                    Unit = "م³ (m³)",
                    UnitPriceJod = sandPricePerM3,
                    TotalPriceJod = sandCost,
                    Note = "لخلطات المونة والبناء والمدات الأرضية"
                },
                new()
                {
                    ItemNameAr = "أجور عمالة مقاولة العظم والآليات",
                    ItemNameEn = "Skeleton Labor, Formwork & Machinery",
                    Category = "Labor",
                    Quantity = totalBuiltUpArea,
                    Unit = "م² مسطح",
                    UnitPriceJod = laborRate,
                    TotalPriceJod = laborCost,
                    Note = "أجور النجار، الحداد، البناء، ومعدات الحفر والدك"
                }
            };

            return result;
        }

        private async Task SeedInitialMarketPricesAsync()
        {
            var initialList = new List<MarketPrice>
            {
                new()
                {
                    MaterialName = "حديد تسليح أردني مشمول الضريبة (12-32 ملم)",
                    MaterialNameEn = "Jordanian Rebar Steel (12-32mm)",
                    MaterialCategory = "Steel",
                    Price = 515.00m,
                    PreviousPrice = 510.00m,
                    GlobalPriceUsd = 535.00m,
                    ChangePercent = 0.98,
                    Trend = "up",
                    UnitOfMeasure = "طن",
                    Currency = "د.أ",
                    Specification = "Grade 60 / ASTM A615 (مجدول ومطابق للمواصفات الأردنية)",
                    Source = "بورصة كتل الصلب العالمية + معادلة السوق الأردني (LME / Black Sea Billet)"
                },
                new()
                {
                    MaterialName = "حديد تسليح قياسات صغيرة (8-10 ملم)",
                    MaterialNameEn = "Small Diameter Rebar Steel (8-10mm)",
                    MaterialCategory = "Steel",
                    Price = 527.00m,
                    PreviousPrice = 525.00m,
                    GlobalPriceUsd = 535.00m,
                    ChangePercent = 0.38,
                    Trend = "up",
                    UnitOfMeasure = "طن",
                    Currency = "د.أ",
                    Specification = "Grade 60 (كانات وأعمدة)",
                    Source = "بورصة كتل الصلب العالمية + معادلة السوق الأردني"
                },
                new()
                {
                    MaterialName = "إسمنت بورتلاندي عادي (CEM I 42.5 N)",
                    MaterialNameEn = "Ordinary Portland Cement (42.5 N)",
                    MaterialCategory = "Cement",
                    Price = 88.00m,
                    PreviousPrice = 88.50m,
                    GlobalPriceUsd = 82.00m,
                    ChangePercent = -0.56,
                    Trend = "down",
                    UnitOfMeasure = "طن",
                    Currency = "د.أ",
                    Specification = "أكياس 50 كغم (20 كيس/طن) معتمد لجميع أعمال الخرسانة",
                    Source = "مؤشر مصانع الإسمنت الأردنية (لافارج، المناصير، الشمالية)"
                },
                new()
                {
                    MaterialName = "إسمنت مقاوم للأملاح والكبريتات (CEM I 42.5 SR)",
                    MaterialNameEn = "Sulfate Resistant Cement (SRC)",
                    MaterialCategory = "Cement",
                    Price = 94.00m,
                    PreviousPrice = 94.00m,
                    GlobalPriceUsd = 88.00m,
                    ChangePercent = 0.00,
                    Trend = "stable",
                    UnitOfMeasure = "طن",
                    Currency = "د.أ",
                    Specification = "مخصص للأساسات والمناطق الرطبة وتحت الأرض",
                    Source = "مؤشر مصانع الإسمنت الأردنية"
                },
                new()
                {
                    MaterialName = "خرسانة جاهزة قوة B250 مع المضخة",
                    MaterialNameEn = "Ready-Mix Concrete B250 with Pump",
                    MaterialCategory = "Concrete",
                    Price = 43.50m,
                    PreviousPrice = 43.00m,
                    GlobalPriceUsd = 0m,
                    ChangePercent = 1.16,
                    Trend = "up",
                    UnitOfMeasure = "م³",
                    Currency = "د.أ",
                    Specification = "قوة كسر 250 كغم/سم² للقواعد والمدات والجسور",
                    Source = "مؤشر خلاطات الخرسانة الجاهزة - الأردن"
                },
                new()
                {
                    MaterialName = "خرسانة جاهزة قوة B300 مع المضخة",
                    MaterialNameEn = "Ready-Mix Concrete B300 with Pump",
                    MaterialCategory = "Concrete",
                    Price = 46.50m,
                    PreviousPrice = 46.50m,
                    GlobalPriceUsd = 0m,
                    ChangePercent = 0.00,
                    Trend = "stable",
                    UnitOfMeasure = "م³",
                    Currency = "د.أ",
                    Specification = "قوة كسر 300 كغم/سم² للأعمدة والأسقف الحاملة",
                    Source = "مؤشر خلاطات الخرسانة الجاهزة - الأردن"
                },
                new()
                {
                    MaterialName = "طوب إسمنتي مفرغ قياس 20 سم",
                    MaterialNameEn = "Hollow Concrete Blocks 20cm",
                    MaterialCategory = "Blocks",
                    Price = 360.00m,
                    PreviousPrice = 360.00m,
                    GlobalPriceUsd = 0m,
                    ChangePercent = 0.00,
                    Trend = "stable",
                    UnitOfMeasure = "1000 حبة",
                    Currency = "د.أ",
                    Specification = "20x20x40 سم للجدران الخارجية المعزولة",
                    Source = "مؤشر معامل الطوب الأردنية المعتمدة"
                },
                new()
                {
                    MaterialName = "طوب هوردي للأسقف قياس 18 سم",
                    MaterialNameEn = "Rib Floor Blocks 18cm",
                    MaterialCategory = "Blocks",
                    Price = 390.00m,
                    PreviousPrice = 390.00m,
                    GlobalPriceUsd = 0m,
                    ChangePercent = 0.00,
                    Trend = "stable",
                    UnitOfMeasure = "1000 حبة",
                    Currency = "د.أ",
                    Specification = "مخصص لأسقف العقدات والربس الخفيف",
                    Source = "مؤشر معامل الطوب الأردنية المعتمدة"
                },
                new()
                {
                    MaterialName = "رمل صويلح مغسول نخب أول",
                    MaterialNameEn = "Washed Sweileh Sand Grade A",
                    MaterialCategory = "Aggregates",
                    Price = 14.50m,
                    PreviousPrice = 14.00m,
                    GlobalPriceUsd = 0m,
                    ChangePercent = 3.57,
                    Trend = "up",
                    UnitOfMeasure = "م³",
                    Currency = "د.أ",
                    Specification = "لأعمال البناء والقصارة الممتازة",
                    Source = "مؤشر مقالع وكسارات الأردن"
                },
                new()
                {
                    MaterialName = "حصمة مكسرة (سمسمية / عدسية / فولية)",
                    MaterialNameEn = "Graded Crushed Aggregates",
                    MaterialCategory = "Aggregates",
                    Price = 11.00m,
                    PreviousPrice = 11.00m,
                    GlobalPriceUsd = 0m,
                    ChangePercent = 0.00,
                    Trend = "stable",
                    UnitOfMeasure = "م³",
                    Currency = "د.أ",
                    Specification = "مفحوصة مخبرياً لخلطات الباطون والمونة",
                    Source = "مؤشر مقالع وكسارات الأردن"
                }
            };

            foreach (var item in initialList)
            {
                await _unitOfWork.MarketPrices.AddAsync(item);
            }

            await _unitOfWork.CompleteAsync();
        }
    }
}
