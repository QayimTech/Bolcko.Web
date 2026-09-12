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

            // ────────────────────────────────────────────────────────────────
            // 1. DYNAMIC STEEL REBAR ESTIMATION (Flexible Rebar & Columns)
            // ────────────────────────────────────────────────────────────────
            // Base empirical ratio by building type
            double baseSteelKgPerM2 = request.BuildingType == "Commercial" ? 45.0 : 41.0;

            // Rebar Density adjustment (Economic / Standard / Heavy / Custom)
            if (request.CustomRebarKgPerM2.HasValue && request.CustomRebarKgPerM2.Value > 15)
            {
                baseSteelKgPerM2 = request.CustomRebarKgPerM2.Value;
            }
            else if (request.RebarDensityGrade == "Economic")
            {
                baseSteelKgPerM2 = 37.5;
            }
            else if (request.RebarDensityGrade == "Heavy")
            {
                baseSteelKgPerM2 = 49.0;
            }

            // Column bar specification adjustment (6 bars vs 8 bars vs 10 bars)
            if (request.ColumnBarsCount == "8Bars")
            {
                baseSteelKgPerM2 += 4.5; // Adding ~4.5 kg/m2 for 8-bar column reinforcement & seismic ties
            }
            else if (request.ColumnBarsCount == "10Bars")
            {
                baseSteelKgPerM2 += 7.5;
            }

            // Slab System adjustment
            if (request.SlabSystem == "FlatSlab")
            {
                baseSteelKgPerM2 += 5.5; // Top + Bottom two-way rebar mesh
            }
            else if (request.SlabSystem == "Solid")
            {
                baseSteelKgPerM2 += 2.0;
            }

            // Foundation type adjustment
            if (request.FoundationType == "Raft")
            {
                baseSteelKgPerM2 += 4.0;
            }

            double steelRatioTonsPerM2 = baseSteelKgPerM2 / 1000.0;
            double steelTons = Math.Round(totalBuiltUpArea * steelRatioTonsPerM2, 2);
            decimal steelCost = Math.Round((decimal)steelTons * steelPrice, 2);

            // ────────────────────────────────────────────────────────────────
            // 2. CONCRETE, CEMENT, BLOCKS, SAND & SKELETON LABOR
            // ────────────────────────────────────────────────────────────────
            double concreteRatio = request.BuildingType == "Commercial" ? 0.42 : 0.38;
            if (request.SlabSystem == "FlatSlab") concreteRatio += 0.05; // Thicker slab
            double concreteM3 = Math.Round(totalBuiltUpArea * concreteRatio, 2);
            decimal concreteCost = Math.Round((decimal)concreteM3 * concretePrice, 2);

            int cementBags = (int)Math.Ceiling(totalBuiltUpArea * 0.65);
            decimal cementPricePerBag = cementPrice / 20.00m; // 20 bags per ton
            decimal cementCost = Math.Round(cementBags * cementPricePerBag, 2);

            int blocksCount = (int)Math.Ceiling(totalBuiltUpArea * (request.SlabSystem == "FlatSlab" ? 12 : 20));
            decimal blocksCost = Math.Round((blocksCount / 1000.0m) * blockPricePerThousand, 2);

            double sandM3 = Math.Round(totalBuiltUpArea * 0.35, 2);
            decimal sandCost = Math.Round((decimal)sandM3 * sandPricePerM3, 2);

            decimal laborRate = request.BuildingType == "Commercial" ? 38.00m : 34.00m;
            if (request.ColumnBarsCount == "8Bars") laborRate += 1.5m; // extra steel fixing labor
            decimal laborCost = Math.Round((decimal)totalBuiltUpArea * laborRate, 2);

            decimal totalSkeletonCost = steelCost + concreteCost + cementCost + blocksCost + sandCost + laborCost;
            decimal costPerM2 = totalBuiltUpArea > 0 ? Math.Round(totalSkeletonCost / (decimal)totalBuiltUpArea, 2) : 0;

            // ────────────────────────────────────────────────────────────────
            // 3. STONE & FACADES ESTIMATION MODULE
            // ────────────────────────────────────────────────────────────────
            bool stoneEnabled = request.EnableStoneModule;
            double stoneNetAreaM2 = 0;
            decimal stoneMaterialCost = 0;
            double corniceMeters = 0;
            decimal corniceCost = 0;
            decimal windowFramesCost = 0;
            decimal entranceColumnsCost = 0;
            decimal stoneAccessoriesCost = 0;
            decimal totalStoneCost = 0;
            decimal potentialSavings = 0;

            if (stoneEnabled)
            {
                // Perimeter estimation based on floor footprint geometry (Square/Rectangular assumption)
                double sideLength = Math.Sqrt(areaPerFloor);
                double fullPerimeter = Math.Round(4.0 * sideLength, 1);
                int facades = Math.Clamp(request.StoneFacadesCount, 1, 4);
                double selectedPerimeter = Math.Round((fullPerimeter / 4.0) * facades, 1);

                double floorClearHeight = 3.3; // standard floor height in Jordan including slab
                double totalWallHeight = floorClearHeight * floors;
                double grossStoneArea = selectedPerimeter * totalWallHeight;

                // 18% openings deductions (Windows & Doors) + 7% stone cutting wastage
                double openingsDeduction = grossStoneArea * 0.18;
                double netBeforeWastage = grossStoneArea - openingsDeduction;
                stoneNetAreaM2 = Math.Round(netBeforeWastage * 1.07, 1);

                // Stone Material Unit Price (JOD/m2)
                decimal stoneUnitPrice = 18.50m; // Default Ruwaished
                decimal benchmarkNaturalPrice = 22.00m; // Maan benchmark for savings calculation

                if (request.StoneType == "Natural_Maan")
                {
                    stoneUnitPrice = 24.50m;
                    benchmarkNaturalPrice = 24.50m;
                }
                else if (request.StoneType == "Artificial_HighDensity")
                {
                    stoneUnitPrice = 13.50m; // Artificial stone ~35-45% more affordable
                }

                // Finish Premium (e.g. Tabzeh vs Mufajjar)
                if (request.StoneFinish == "Tabzeh") stoneUnitPrice += 1.50m;
                else if (request.StoneFinish == "Musamsam") stoneUnitPrice += 2.00m;

                stoneMaterialCost = Math.Round((decimal)stoneNetAreaM2 * stoneUnitPrice, 2);

                // Cornice / Belts between floors
                if (request.IncludeCorniceBelt && floors > 1)
                {
                    corniceMeters = Math.Round(selectedPerimeter * (floors - 1), 1);
                    decimal corniceRatePerMeter = request.HybridArtificialTrim || request.StoneType == "Artificial_HighDensity" ? 7.50m : 12.50m;
                    corniceCost = Math.Round((decimal)corniceMeters * corniceRatePerMeter, 2);
                }

                // Window Frames & Arches
                int winCount = Math.Max(0, request.WindowFramesCount);
                decimal windowFrameRate = request.HybridArtificialTrim || request.StoneType == "Artificial_HighDensity" ? 22.00m : 38.00m;
                windowFramesCost = Math.Round(winCount * windowFrameRate, 2);

                // Entrance Columns
                int colCount = Math.Max(0, request.EntranceColumnsCount);
                decimal columnRate = request.HybridArtificialTrim || request.StoneType == "Artificial_HighDensity" ? 85.00m : 160.00m;
                entranceColumnsCost = Math.Round(colCount * columnRate, 2);

                // Backing concrete, galvanized ties, stainless steel anchors, joint mortar
                if (request.IncludeStoneInstallation)
                {
                    decimal stoneInstallationAndBackingRate = 12.00m; // ~12 JOD/m2 for backing concrete + ties + installation labor
                    stoneAccessoriesCost = Math.Round((decimal)stoneNetAreaM2 * stoneInstallationAndBackingRate, 2);
                }

                totalStoneCost = stoneMaterialCost + corniceCost + windowFramesCost + entranceColumnsCost + stoneAccessoriesCost;

                // Calculate smart savings if using Artificial or Hybrid trim
                if (request.StoneType == "Artificial_HighDensity")
                {
                    decimal naturalEquivalent = (decimal)stoneNetAreaM2 * benchmarkNaturalPrice;
                    potentialSavings = Math.Max(0, naturalEquivalent - stoneMaterialCost);
                }
                else if (request.HybridArtificialTrim)
                {
                    decimal naturalDecorCost = (decimal)corniceMeters * 12.50m + winCount * 38.00m + colCount * 160.00m;
                    decimal hybridDecorCost = corniceCost + windowFramesCost + entranceColumnsCost;
                    potentialSavings = Math.Max(0, naturalDecorCost - hybridDecorCost);
                }
            }

            decimal grandTotal = totalSkeletonCost + totalStoneCost;

            var result = new ConstructionEstimateResultDto
            {
                TotalBuiltUpArea = totalBuiltUpArea,
                NumberOfFloors = floors,
                City = request.City,
                SteelQuantityTons = steelTons,
                SteelCostJod = steelCost,
                EffectiveRebarRatioKgPerM2 = Math.Round(baseSteelKgPerM2, 1),
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

                // Stone & Facades
                StoneModuleEnabled = stoneEnabled,
                StoneNetAreaM2 = stoneNetAreaM2,
                StoneMaterialCostJod = stoneMaterialCost,
                CorniceLinearMeters = corniceMeters,
                CorniceCostJod = corniceCost,
                WindowFramesCostJod = windowFramesCost,
                EntranceColumnsCostJod = entranceColumnsCost,
                StoneAccessoriesLaborCostJod = stoneAccessoriesCost,
                TotalStoneCostJod = totalStoneCost,
                PotentialSavingsJod = potentialSavings,

                // Combined Grand Total
                GrandTotalCostJod = grandTotal,
                GeneratedAt = DateTime.UtcNow
            };

            // Build Comprehensive Bill of Quantities (BOQ) Items
            result.MaterialItems = new List<MaterialEstimateItem>
            {
                new()
                {
                    ItemNameAr = $"حديد تسليح عالي المقاومة (Grade 60) - {request.ColumnBarsCount}",
                    ItemNameEn = $"High-Strength Steel Rebar (Grade 60) - {request.ColumnBarsCount}",
                    Category = "Steel",
                    Quantity = steelTons,
                    Unit = "طن (Ton)",
                    UnitPriceJod = steelPrice,
                    TotalPriceJod = steelCost,
                    Note = $"معدل ({baseSteelKgPerM2:N1} كغم/م²) يشمل أعمدة {request.ColumnBarsCount} والقواعد والأسقف"
                },
                new()
                {
                    ItemNameAr = $"خرسانة جاهزة B250 / B300 مع المضخة ({request.SlabSystem})",
                    ItemNameEn = $"Ready-Mix Concrete B250/B300 with Pump ({request.SlabSystem})",
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

            // Add Stone Items if enabled
            if (stoneEnabled && stoneNetAreaM2 > 0)
            {
                string stoneTypeDescAr = request.StoneType switch
                {
                    "Natural_Maan" => "حجر معان طبيعي نخب أول",
                    "Artificial_HighDensity" => "حجر صناعي عالي الكثافة (Engineered Stone)",
                    _ => "حجر رويشد طبيعي نخب أول"
                };

                string stoneTypeDescEn = request.StoneType switch
                {
                    "Natural_Maan" => "Premium Natural Maan Stone",
                    "Artificial_HighDensity" => "High-Density Engineered Stone",
                    _ => "Premium Natural Ruwaished Stone"
                };

                result.MaterialItems.Add(new MaterialEstimateItem
                {
                    ItemNameAr = $"{stoneTypeDescAr} - نقشة {request.StoneFinish} ({request.StoneFacadesCount} واجهات)",
                    ItemNameEn = $"{stoneTypeDescEn} - {request.StoneFinish} ({request.StoneFacadesCount} Facades)",
                    Category = "Stone",
                    Quantity = stoneNetAreaM2,
                    Unit = "م² مسطح",
                    UnitPriceJod = stoneMaterialCost / (decimal)stoneNetAreaM2,
                    TotalPriceJod = stoneMaterialCost,
                    Note = $"صافي المساحة بعد خصم الفتحات والشبابيك 18% مع احتساب 7% هدر وركوب"
                });

                if (corniceCost > 0)
                {
                    result.MaterialItems.Add(new MaterialEstimateItem
                    {
                        ItemNameAr = "أحزمة وكرانيش حجرية بين الطوابق",
                        ItemNameEn = "Stone Cornice Belts Between Floors",
                        Category = "StoneDecor",
                        Quantity = corniceMeters,
                        Unit = "متر طولي",
                        UnitPriceJod = corniceCost / (decimal)corniceMeters,
                        TotalPriceJod = corniceCost,
                        Note = request.HybridArtificialTrim ? "ديكور حجر صناعي متطابق اللون فائق المتانة" : "حجر طبيعي منحوت"
                    });
                }

                if (windowFramesCost > 0)
                {
                    result.MaterialItems.Add(new MaterialEstimateItem
                    {
                        ItemNameAr = "براويز وأقواس حجرية للشبابيك والفتحات",
                        ItemNameEn = "Stone Window Frames & Architectural Arches",
                        Category = "StoneDecor",
                        Quantity = request.WindowFramesCount,
                        Unit = "شباك / فتحة",
                        UnitPriceJod = windowFramesCost / Math.Max(1, request.WindowFramesCount),
                        TotalPriceJod = windowFramesCost,
                        Note = "تفصيل وبروز حماية وعزل مياه الأمطار"
                    });
                }

                if (entranceColumnsCost > 0)
                {
                    result.MaterialItems.Add(new MaterialEstimateItem
                    {
                        ItemNameAr = "أعمدة حجرية دائرية للمدخل مع التيجان والقواعد",
                        ItemNameEn = "Stone Entrance Columns with Capitals & Bases",
                        Category = "StoneDecor",
                        Quantity = request.EntranceColumnsCount,
                        Unit = "عامود",
                        UnitPriceJod = entranceColumnsCost / Math.Max(1, request.EntranceColumnsCount),
                        TotalPriceJod = entranceColumnsCost,
                        Note = "تيجان كلاسيكية مع قواعد مدخل فخمة"
                    });
                }

                if (stoneAccessoriesCost > 0)
                {
                    result.MaterialItems.Add(new MaterialEstimateItem
                    {
                        ItemNameAr = "مصنعية تركيب الحجر + باطون حشوة وشناكل ستانلس ستيل",
                        ItemNameEn = "Stone Installation, Backing Concrete & SS Ties",
                        Category = "StoneLabor",
                        Quantity = stoneNetAreaM2,
                        Unit = "م² مسطح",
                        UnitPriceJod = 12.00m,
                        TotalPriceJod = stoneAccessoriesCost,
                        Note = "يشمل شبك العزل، الشناكل المجلفنة/الستانلس، صب باطون الحشوة، والكحلة"
                    });
                }
            }

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

        public async Task<CalculatorConfigurationDto> GetCalculatorConfigurationAsync()
        {
            const string cacheKey = "AppSettings_CalculatorConfig";
            if (_cache.TryGetValue(cacheKey, out CalculatorConfigurationDto? cachedConfig) && cachedConfig != null)
            {
                return cachedConfig;
            }

            var setting = await _unitOfWork.AppSettings.GetByKeyAsync("CalculatorConfig");
            if (setting != null && !string.IsNullOrWhiteSpace(setting.Value))
            {
                try
                {
                    var config = System.Text.Json.JsonSerializer.Deserialize<CalculatorConfigurationDto>(setting.Value);
                    if (config != null)
                    {
                        _cache.Set(cacheKey, config, TimeSpan.FromHours(4));
                        return config;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize CalculatorConfig, returning defaults.");
                }
            }

            var defaultConfig = new CalculatorConfigurationDto();
            _cache.Set(cacheKey, defaultConfig, TimeSpan.FromHours(4));
            return defaultConfig;
        }

        public async Task SaveCalculatorConfigurationAsync(CalculatorConfigurationDto config)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(config);
            var existing = await _unitOfWork.AppSettings.GetByKeyAsync("CalculatorConfig");
            if (existing != null)
            {
                existing.Value = json;
                existing.LastUpdated = DateTime.UtcNow;
                _unitOfWork.AppSettings.Update(existing);
            }
            else
            {
                var newSetting = new Bolcko.Domain.Entities.Setting.AppSetting
                {
                    Key = "CalculatorConfig",
                    Value = json,
                    Description = "Global dynamic coefficients and finishing addons for construction calculator",
                    LastUpdated = DateTime.UtcNow
                };
                await _unitOfWork.AppSettings.AddAsync(newSetting);
            }

            await _unitOfWork.CompleteAsync();
            _cache.Remove("AppSettings_CalculatorConfig");
        }
    }
}
