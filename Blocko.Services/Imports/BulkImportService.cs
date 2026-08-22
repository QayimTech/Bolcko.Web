using Bolcko.Domain.Interfaces;
using Bolcko.Domain.Entities.Product.DTOs;
using Bolcko.Domain.Entities.Catalog.DTOs;
using ClosedXML.Excel;
using FluentValidation;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text.Json;
using Bolcko.Domain.Entities.Product;
using Bolcko.Domain.Entities.Catalog;
using Blocko.Services.Interfaces.Image;
using Microsoft.Extensions.Caching.Memory;

namespace Blocko.Services.Imports
{
    public class BulkImportService : IBulkImportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<ProductImportDto> _productValidator;
        private readonly IValidator<CategoryImportDto> _categoryValidator;
        private readonly ILogger<BulkImportService> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly IImageService _imageService;
        private readonly string _contentRootPath;
        private readonly Dictionary<string, Category> _categoryCache = new(StringComparer.OrdinalIgnoreCase);

        // ─── Arabic ↔ English column name map ───────────────────────────────
        private static readonly Dictionary<string, string> _colMap = new(StringComparer.OrdinalIgnoreCase)
        {
            // ── Product name ─────────────────────────────────────────────────
            ["الاسم"]                        = "Name",
            ["اسم المنتج"]                   = "Name",
            ["اسم_المنتج"]                   = "Name",
            ["اسم المنتج (عربي)"]            = "Name",
            ["اسم المنتج (العربي)"]          = "Name",
            ["اسم المنتج (العربية)"]         = "Name",
            ["اسم المنتج (Arabic)"]          = "Name",
            ["product_name_ar"]              = "Name",
            ["Product Name (English)"]       = "NameEn",
            ["product name english"]         = "NameEn",
            ["name_en"]                      = "NameEn",
            ["الاسم الانجليزي"]              = "NameEn",
            ["product_name_en"]              = "NameEn",
            // ── Description ──────────────────────────────────────────────────
            ["الوصف"]                        = "Description",
            ["الوصف (العربي)"]               = "Description",
            ["الوصف (العربية)"]              = "Description",
            ["الوصف (Arabic)"]               = "Description",
            ["description_ar"]               = "Description",
            ["short_description_ar"]         = "Description",
            ["Description (English)"]        = "DescriptionEn",
            ["description english"]          = "DescriptionEn",
            ["description_en"]               = "DescriptionEn",
            ["الوصف الانجليزي"]              = "DescriptionEn",
            // ── Category ─────────────────────────────────────────────────────
            ["التصنيف"]                      = "CategoryName",
            ["التصنيف الرئيسي"]             = "CategoryName",
            ["الفئة"]                        = "CategoryName",
            ["اسم الفئة للمنتج"]             = "CategoryName",
            ["اسم_الفئة"]                    = "CategoryName",
            ["categoryname"]                 = "CategoryName",
            ["category_hierarchy"]           = "CategoryName",
            ["categoryid"]                   = "CategoryName",   // sheet alias

            // ── Parent category (the real parent/root) ────────────────────────
            ["التصنيف الفرعي"]              = "ParentCategoryName",
            ["الفئة الأم"]                   = "ParentCategoryName",
            ["اسم الفئة الأم"]               = "ParentCategoryName",
            ["parentcategoryname"]           = "ParentCategoryName",
            ["parentcat"]                    = "ParentCategoryName",  // sheet alias
            ["parent_category"]              = "ParentCategoryName",
            ["parent cat"]                   = "ParentCategoryName",
            ["parent category"]              = "ParentCategoryName",  // Python pipeline alias

            // ── Micro / leaf category ─────────────────────────────────────────
            ["التصنيف الدقيق (Micro)"]       = "MicroCategoryName",
            ["التصنيف الدقيق"]               = "MicroCategoryName",
            ["microcategoryname"]            = "MicroCategoryName",
            ["microcat"]                     = "MicroCategoryName",  // sheet alias
            ["micro_category"]               = "MicroCategoryName",
            // ── SKU / Product code ────────────────────────────────────────────
            ["كود المنتج"]                   = "Sku",
            ["كود_المنتج"]                   = "Sku",
            ["sku"]                          = "Sku",
            ["product code"]                 = "Sku",
            // ── Price ─────────────────────────────────────────────────────────
            ["السعر"]                        = "Price",
            ["price"]                        = "Price",
            ["retailprice"]                  = "Price",
            ["sale_price"]                   = "Price",
            // ── Stock ─────────────────────────────────────────────────────────
            ["الكمية"]                       = "Stock",
            ["stock"]                        = "Stock",
            ["stockquantity"]                = "Stock",
            ["stock_quantity"]               = "Stock",
            ["stock quantity"]               = "Stock",
            ["quantity"]                     = "Stock",
            ["المخزون"]                      = "Stock",
            ["كمية المخزون"]                 = "Stock",
            ["الكمية المتوفرة"]              = "Stock",
            // ── Unit of measure ───────────────────────────────────────────────
            ["وحدة القياس"]                  = "UnitOfMeasure",
            ["الوحدة"]                       = "UnitOfMeasure",
            ["unitofmeasure"]                = "UnitOfMeasure",
            ["unit"]                         = "UnitOfMeasure",
            // ── Weight ────────────────────────────────────────────────────────
            ["الوزن"]                        = "Weight",
            ["weight"]                       = "Weight",
            // ── Dimensions / Size ─────────────────────────────────────────────
            ["الأبعاد"]                      = "Dimensions",
            ["المقاس"]                       = "Dimensions",
            ["المقاس (Meters)"]              = "Dimensions",
            ["المقاس (mm)"]                  = "Dimensions",
            ["الحجم"]                        = "Dimensions",
            ["dimensions"]                   = "Dimensions",
            // ── Status ────────────────────────────────────────────────────────
            ["الحالة"]                       = "Status",
            ["صلاح المنتج"]                  = "Status",
            ["صلاحية المنتج"]                = "Status",
            ["متاح للبيع"]                   = "Status",
            ["status"]                       = "Status",
            // ── Image ─────────────────────────────────────────────────────────
            ["صورة"]                         = "Image",
            ["الصورة"]                       = "Image",
            ["image"]                        = "Image",
            ["image_path"]                   = "Image",
            // ── Brand / Supplier ──────────────────────────────────────────────
            ["البراند"]                      = "Brand",
            ["brand"]                        = "Brand",
            ["اسم المورد"]                   = "Brand",
            ["المورد"]                       = "Brand",
            // ── Country of origin ─────────────────────────────────────────────────
            ["بلد المنشأ"]                   = "CountryOfOrigin",
            ["countryoforigin"]              = "CountryOfOrigin",
            ["origin"]                        = "CountryOfOrigin",
            // ── Category metadata ─────────────────────────────────────────────
            ["أيقونة الفئة"]                 = "CategoryIcon",
            ["الايكون"]                      = "CategoryIcon",
            ["category icon"]                = "CategoryIcon",
            // ── Category sheet columns ─────────────────────────────────────────
            ["اسم الفئة"]                    = "Name",
            ["ترتيب العرض"]                  = "DisplayOrder",
            ["displayorder"]                 = "DisplayOrder",
            ["description"]                  = "Description",
            ["name"]                         = "Name",

            // ── SEO Metadata ───────────────────────────────────────────────────
            ["عنوان seo"]                    = "MetaTitle",
            ["seo title"]                    = "MetaTitle",
            ["metatitle"]                    = "MetaTitle",
            ["عنوان الصفحة"]                 = "MetaTitle",
            ["وصف seo"]                     = "MetaDescription",
            ["seo description"]              = "MetaDescription",
            ["metadescription"]              = "MetaDescription",
            ["وصف الميتا"]                   = "MetaDescription",
            ["الكلمات الدلالية seo"]         = "MetaKeywords",
            ["seo keywords"]                 = "MetaKeywords",
            ["metakeywords"]                 = "MetaKeywords",
            ["الكلمات المفتاحية"]             = "MetaKeywords",

            // Python Pipeline SEO Aliases
            ["seo meta title (ar)"]          = "MetaTitle",
            ["seo meta description (ar)"]    = "MetaDescription",
            ["seo meta keywords (ar)"]       = "MetaKeywords",
            ["seo meta title (en)"]          = "MetaTitle",
            ["seo meta description (en)"]    = "MetaDescription",
            ["seo meta keywords (en)"]       = "MetaKeywords",
            ["seo meta title"]               = "MetaTitle",
            ["seo meta description"]         = "MetaDescription",
            ["seo meta keywords"]            = "MetaKeywords",

            // ── Oversized & Heavy Logistics ─────────────────────────────────────
            ["بضاعة ضخمة"]                   = "IsOversized",
            ["شحن ثقيل"]                     = "IsOversized",
            ["حجم كبير"]                     = "IsOversized",
            ["oversized"]                    = "IsOversized",
            ["is_oversized"]                 = "IsOversized",
            ["isoversized"]                  = "IsOversized",
            ["is oversized"]                 = "IsOversized",

            // ── Product Variants (Parent-Child) ─────────────────────────────────
            ["كود المنتج الأساسي"]           = "ParentSku",
            ["كود_المنتج_الأساسي"]           = "ParentSku",
            ["parent sku"]                   = "ParentSku",
            ["parent_sku"]                   = "ParentSku",
            ["parentsku"]                    = "ParentSku",

            ["كود الفارينت"]                 = "VariantSku",
            ["كود_الفارينت"]                 = "VariantSku",
            ["variant sku"]                  = "VariantSku",
            ["variant_sku"]                  = "VariantSku",
            ["variantsku"]                   = "VariantSku",

            ["اللون"]                        = "Color",
            ["color"]                        = "Color",
            ["option 2 value (color)"]       = "Color",
            ["option 2 value"]               = "Color",

            ["المقاس / الحجم"]               = "Size",
            ["size"]                         = "Size",
            ["option 1 value (size/model)"]  = "Size",
            ["option 1 value (size)"]        = "Size",
            ["option 1 value"]               = "Size",

            ["وحدة التعبئة"]                 = "PackagingUnit",
            ["packaging unit"]               = "PackagingUnit",
            ["packaging_unit"]               = "PackagingUnit",

            ["سعر الفارينت"]                 = "VariantPrice",
            ["سعر_الفارينت"]                 = "VariantPrice",
            ["variant price"]                = "VariantPrice",
            ["variant_price"]                = "VariantPrice",
            ["variantprice"]                 = "VariantPrice",
            ["variant price (jd)"]           = "VariantPrice",

            ["مخزون الفارينت"]               = "VariantStock",
            ["كمية الفارينت"]                = "VariantStock",
            ["variant stock"]                = "VariantStock",
            ["variant_stock"]                = "VariantStock",
            ["variantstock"]                 = "VariantStock",

            ["صورة الفارينت"]                = "VariantImage",
            ["variant image"]                = "VariantImage",
            ["variant_image"]                = "VariantImage",
            ["variantimage"]                 = "VariantImage",
            ["variant image file"]           = "VariantImage",

            // ── Additional Python Pipeline Aliases ──────────────────────────────
            ["product code / sku"]           = "Sku",
            ["product code/sku"]             = "Sku",
            ["product name (ar)"]            = "Name",
            ["product name (en)"]            = "NameEn",
            ["sub category"]                 = "CategoryName",
            ["subcategory"]                  = "CategoryName",
            ["sub_category"]                 = "CategoryName",
            ["description (ar)"]             = "Description",
            ["description (en)"]             = "DescriptionEn",
            ["sale price (jd)"]              = "Price",
            ["sale price"]                   = "Price",
            ["cost price (jd)"]              = "CostPrice",
            ["cost price"]                   = "CostPrice",
            ["compare at price (jd)"]        = "ComparePrice",
            ["compare at price"]             = "ComparePrice",
            ["brand name"]                   = "Brand",
            ["primary image file"]           = "Image",
            ["primary image"]                = "Image",
            ["country of origin"]            = "CountryOfOrigin",
            ["unit of measure"]              = "UnitOfMeasure",
            ["option 1 value (size)"]        = "Size",
            ["option 2 value (color)"]       = "Color",
            ["variant price (jd)"]           = "VariantPrice",
            ["variant stock"]                = "VariantStock",
            ["variant image file"]           = "VariantImage",
            ["category icon"]                = "CategoryIcon",
        };

        public BulkImportService(
            IUnitOfWork unitOfWork,
            IValidator<ProductImportDto> productValidator,
            IValidator<CategoryImportDto> categoryValidator,
            ILogger<BulkImportService> logger,
            IMemoryCache memoryCache,
            IImageService imageService)
            : this(unitOfWork, productValidator, categoryValidator, logger, memoryCache, imageService, AppContext.BaseDirectory)
        {
        }

        public BulkImportService(
            IUnitOfWork unitOfWork,
            IValidator<ProductImportDto> productValidator,
            IValidator<CategoryImportDto> categoryValidator,
            ILogger<BulkImportService> logger,
            IMemoryCache memoryCache,
            IImageService imageService,
            string contentRootPath)
        {
            _unitOfWork = unitOfWork;
            _productValidator = productValidator;
            _categoryValidator = categoryValidator;
            _logger = logger;
            _memoryCache = memoryCache;
            _imageService = imageService;
            _contentRootPath = string.IsNullOrWhiteSpace(contentRootPath) ? AppContext.BaseDirectory : contentRootPath;
        }

        // ════════════════════════════════════════════════════════════════════
        //  UNIFIED EXCEL IMPORT
        // ════════════════════════════════════════════════════════════════════
        public async Task<ImportResult> ProcessUnifiedExcelImportAsync(string filePath, string? localImageFolderPath = null)
        {
            var result = new ImportResult();
            _logger.LogInformation("Starting unified Excel import from {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                _logger.LogError("File not found: {FilePath}", filePath);
                return result;
            }

            try
            {
                using var workbook = new XLWorkbook(filePath);

                // ── Step 1: import categories first ─────────────────────────
                var categorySheet = FindSheet(workbook, "categories", "الفئات", "category", "الفئة");
                if (categorySheet != null)
                {
                    _logger.LogInformation("Processing category sheet: {Name}", categorySheet.Name);
                    await ImportCategoriesFromWorksheet(categorySheet);
                }

                // ── Step 2: import products ─────────────────────────────────
                var productSheet = FindSheet(workbook, "products", "المنتجات", "product", "منتجات") ?? workbook.Worksheets.FirstOrDefault();
                if (productSheet != null)
                {
                    _logger.LogInformation("Processing product sheet: {Name}", productSheet.Name);
                    await ImportProductsFromWorksheet(productSheet, result, localImageFolderPath);
                }

                _logger.LogInformation("Unified Excel import completed. {Summary}", result.Summary);

                // Evict Header & Home Caches so Mega Menu and Featured Products update immediately!
                _memoryCache.Remove("Header_Categories_ar");
                _memoryCache.Remove("Header_Categories_ar-JO");
                _memoryCache.Remove("Header_Categories_en");
                _memoryCache.Remove("Header_Categories_en-US");

                _memoryCache.Remove("Home_FeaturedProducts_ar");
                _memoryCache.Remove("Home_FeaturedProducts_ar-JO");
                _memoryCache.Remove("Home_FeaturedProducts_en");
                _memoryCache.Remove("Home_FeaturedProducts_en-US");

                _memoryCache.Remove("Home_RootCategories_ar");
                _memoryCache.Remove("Home_RootCategories_ar-JO");
                _memoryCache.Remove("Home_RootCategories_en");
                _memoryCache.Remove("Home_RootCategories_en-US");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing unified Excel import");
            }

            return result;
        }

        // ════════════════════════════════════════════════════════════════════
        //  GOOGLE SHEETS IMPORT
        // ════════════════════════════════════════════════════════════════════
        public async Task<ImportResult> ProcessGoogleSheetImportAsync(string googleSheetUrl, string? localImageFolderPath = null)
        {
            var result = new ImportResult();
            _logger.LogInformation("Starting Google Sheets import from {Url}", googleSheetUrl);

            try
            {
                // Extract sheet ID from URL
                var sheetId = ExtractGoogleSheetId(googleSheetUrl);
                if (string.IsNullOrWhiteSpace(sheetId))
                {
                    result.HasError = true;
                    result.ErrorMessage = "Invalid Google Sheet URL";
                    return result;
                }

                // Download as Excel
                var exportUrl = $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=xlsx";
                using var httpClient = new System.Net.Http.HttpClient();
                var excelBytes = await httpClient.GetByteArrayAsync(exportUrl);

                // Save to temp file
                var tempFolder = Path.Combine(Path.GetTempPath(), "BolckoImports");
                Directory.CreateDirectory(tempFolder);
                var tempFilePath = Path.Combine(tempFolder, $"{Guid.NewGuid()}.xlsx");
                await File.WriteAllBytesAsync(tempFilePath, excelBytes);

                // Process using existing Excel import
                result = await ProcessUnifiedExcelImportAsync(tempFilePath, localImageFolderPath);

                // Cleanup temp file
                try { File.Delete(tempFilePath); } catch { /* ignore cleanup errors */ }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Google Sheets import");
                result.HasError = true;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private static string? ExtractGoogleSheetId(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var match = System.Text.RegularExpressions.Regex.Match(url, @"spreadsheets/d/([a-zA-Z0-9-_]+)");
            return match.Success ? match.Groups[1].Value : null;
        }

        // ════════════════════════════════════════════════════════════════════
        //  INTERNAL HELPERS
        // ════════════════════════════════════════════════════════════════════

        private async Task ImportCategoriesFromWorksheet(IXLWorksheet ws)
        {
            var range = ws.RangeUsed();
            if (range == null)
            {
                _logger.LogWarning("Worksheet '{Name}' is empty.", ws.Name);
                return;
            }
            var colIndex = BuildColumnIndex(ws);

            foreach (var row in range.RowsUsed().Skip(1))
            {
                var dto = new CategoryImportDto
                {
                    Name               = GetCell(row, colIndex, "Name"),
                    ParentCategoryName = GetCell(row, colIndex, "ParentCategoryName"),
                    Description        = GetCell(row, colIndex, "Description"),
                    DisplayOrder       = int.TryParse(GetCell(row, colIndex, "DisplayOrder"), out var ord) ? ord : 0,
                    MetaTitle          = GetCell(row, colIndex, "MetaTitle"),
                    MetaDescription    = GetCell(row, colIndex, "MetaDescription"),
                    MetaKeywords       = GetCell(row, colIndex, "MetaKeywords")
                };

                if (string.IsNullOrWhiteSpace(dto.Name)) continue;
                await SaveCategoryAsync(dto);
            }
        }

        private async Task ImportProductsFromWorksheet(IXLWorksheet ws, ImportResult result, string? localImageFolderPath = null)
        {
            var range = ws.RangeUsed();
            if (range == null)
            {
                result.HasError = true;
                result.ErrorMessage = "فشل: ورقة العمل فارغة أو لا تحتوي على خلايا مستخدمة.";
                return;
            }
            var colIndex = BuildColumnIndex(ws);

            // Log all column names and the mapped index to help with debugging!
            var headerRow = range.RowsUsed().FirstOrDefault();
            if (headerRow != null)
            {
                var columnNames = new List<string>();
                foreach (var cell in headerRow.CellsUsed())
                {
                    columnNames.Add($"[Column {cell.Address.ColumnNumber}] = '{cell.GetString().Trim()}'");
                }
                _logger.LogInformation("Excel file column names: {Columns}", string.Join(", ", columnNames));
            }

            if (!colIndex.ContainsKey("Name"))
            {
                result.HasError = true;
                result.ErrorMessage = "فشل: لم يتم العثور على عمود 'الاسم' أو 'اسم المنتج' في ملف الاكسل. يرجى التأكد من أسماء الأعمدة.";
                return;
            }
            if (!colIndex.ContainsKey("CategoryName"))
            {
                result.HasError = true;
                result.ErrorMessage = "فشل: لم يتم العثور على عمود 'التصنيف' أو 'الفئة' في ملف الاكسل. يرجى التأكد من أسماء الأعمدة.";
                return;
            }

            int rowNum = 1;

            foreach (var row in range.RowsUsed().Skip(1))
            {
                rowNum++;

                var rawSku = GetCell(row, colIndex, "Sku");
                var cleanSku = CleanAndExtractSku(rawSku);

                var dto = new ProductImportDto
                {
                    Sku             = cleanSku ?? string.Empty,
                    Name            = GetCell(row, colIndex, "Name"),
                    NameEn          = GetCell(row, colIndex, "NameEn"),
                    CategoryName    = GetCell(row, colIndex, "CategoryName"),
                    Description     = CleanHtmlDescription(GetCell(row, colIndex, "Description")),
                    DescriptionEn   = CleanHtmlDescription(GetCell(row, colIndex, "DescriptionEn")),
                    UnitOfMeasure   = GetCell(row, colIndex, "UnitOfMeasure") is { Length: > 0 } uom ? uom : "Unit",
                    Status          = GetCell(row, colIndex, "Status")  is { Length: > 0 } st  ? st  : "Active",
                    Brand           = GetCell(row, colIndex, "Brand"),
                    CountryOfOrigin = GetCell(row, colIndex, "CountryOfOrigin"),
                    ParentCategoryName = GetCell(row, colIndex, "ParentCategoryName"),
                    MicroCategoryName  = GetCell(row, colIndex, "MicroCategoryName"),
                    CategoryIcon    = GetCell(row, colIndex, "CategoryIcon"),
                    MetaTitle       = GetCell(row, colIndex, "MetaTitle"),
                    MetaDescription = GetCell(row, colIndex, "MetaDescription"),
                    MetaKeywords    = GetCell(row, colIndex, "MetaKeywords"),
                    ParentSku       = GetCell(row, colIndex, "ParentSku"),
                    VariantSku      = GetCell(row, colIndex, "VariantSku"),
                    Size            = GetCell(row, colIndex, "Size"),
                    Color           = GetCell(row, colIndex, "Color"),
                    PackagingUnit   = GetCell(row, colIndex, "PackagingUnit"),
                    VariantImageUrl = GetCell(row, colIndex, "VariantImage")
                };

                var oversizedVal = GetCell(row, colIndex, "IsOversized");
                if (!string.IsNullOrWhiteSpace(oversizedVal))
                {
                    dto.IsOversized = oversizedVal.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                      oversizedVal.Equals("1") ||
                                      oversizedVal.Equals("نعم", StringComparison.OrdinalIgnoreCase) ||
                                      oversizedVal.Equals("yes", StringComparison.OrdinalIgnoreCase);
                }

                // If ParentSku is provided, Name can be inferred from parent product if blank
                if (string.IsNullOrWhiteSpace(dto.Name) && string.IsNullOrWhiteSpace(dto.ParentSku)) continue;

                result.TotalRows++;

                var rawPrice = GetCell(row, colIndex, "Price");
                var cleanPrice = CleanAndParsePrice(rawPrice);
                if (cleanPrice.HasValue) dto.RetailPrice = cleanPrice.Value;

                var rawVarPrice = GetCell(row, colIndex, "VariantPrice");
                var cleanVarPrice = CleanAndParsePrice(rawVarPrice);
                if (cleanVarPrice.HasValue) dto.VariantPrice = cleanVarPrice.Value;

                var cleanStock = CleanAndParseInt(GetCell(row, colIndex, "Stock"));
                if (cleanStock.HasValue) dto.StockQuantity = cleanStock.Value;

                var cleanVarStock = CleanAndParseInt(GetCell(row, colIndex, "VariantStock"));
                if (cleanVarStock.HasValue) dto.VariantStock = cleanVarStock.Value;

                if (decimal.TryParse(GetCell(row, colIndex, "Weight"), out var w)) dto.Weight = w;
                dto.Dimensions = GetCell(row, colIndex, "Dimensions");

                var pics = new List<ClosedXML.Excel.Drawings.IXLPicture>();
                foreach (var p in ws.Pictures)
                {
                    try
                    {
                        if (p.TopLeftCell?.Address != null && p.TopLeftCell.Address.RowNumber == row.RowNumber())
                        {
                            pics.Add(p);
                        }
                    }
                    catch
                    {
                        // Safely ignore pictures that cause ClosedXML internal exceptions
                    }
                }

                if (pics.Any())
                {
                    var firstPic = pics.First();
                    if (firstPic.ImageStream != null)
                    {
                        using var ms = new MemoryStream();
                        firstPic.ImageStream.CopyTo(ms);
                        dto.ImageData      = ms.ToArray();
                        dto.ImageExtension = firstPic.Format.ToString().ToLower();
                        dto.ImageMimeType  = $"image/{dto.ImageExtension}";
                    }

                    foreach (var pic in pics.Skip(1))
                    {
                        if (pic.ImageStream != null)
                        {
                            using var ms = new MemoryStream();
                            pic.ImageStream.CopyTo(ms);
                            dto.AdditionalImages.Add((ms.ToArray(), pic.Format.ToString().ToLower(), $"image/{pic.Format.ToString().ToLower()}"));
                        }
                    }
                }

                // Fallback: image column URL, base64, OR local file path
                if (dto.ImageData == null)
                {
                    var imgStr = GetCell(row, colIndex, "Image");
                    if (string.IsNullOrWhiteSpace(imgStr))
                    {
                        imgStr = GetCell(row, colIndex, "VariantImage");
                    }
                    _logger.LogInformation("Product/Variant '{ProductName}' - Read image column value: '{ImageValue}', LocalFolderExists: {LocalFolderExists} ({FolderPath})", 
                        dto.Name ?? dto.VariantSku, imgStr, !string.IsNullOrWhiteSpace(localImageFolderPath) && Directory.Exists(localImageFolderPath), localImageFolderPath);

                    if (!string.IsNullOrWhiteSpace(imgStr))
                    {
                        if (imgStr.StartsWith("=IMAGE(", StringComparison.OrdinalIgnoreCase) ||
                            imgStr.StartsWith("IMAGE(",  StringComparison.OrdinalIgnoreCase))
                        {
                            var match = Regex.Match(imgStr, @"IMAGE\([""']?(http.*?)[""']?\)", RegexOptions.IgnoreCase);
                            if (match.Success) imgStr = match.Groups[1].Value;
                        }

                        if (imgStr.StartsWith("http",       StringComparison.OrdinalIgnoreCase) ||
                            imgStr.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
                        {
                            dto.ImageBase64 = imgStr;
                        }
                        else
                        {
                            // Search in both uploaded folder and persistent ImageLibrary
                            var searchFolders = new List<string>();
                            if (!string.IsNullOrWhiteSpace(localImageFolderPath) && Directory.Exists(localImageFolderPath))
                                searchFolders.Add(localImageFolderPath);

                            var libraryFolder = Path.Combine(_contentRootPath, "App_Data", "Imports", "ImageLibrary");
                            if (Directory.Exists(libraryFolder))
                                searchFolders.Add(libraryFolder);

                            foreach (var folder in searchFolders)
                            {
                                try
                                {
                                    var directoryFiles = Directory.GetFiles(folder);
                                    var matchingFile = directoryFiles.FirstOrDefault(f => 
                                        string.Equals(Path.GetFileName(f), imgStr, StringComparison.OrdinalIgnoreCase));
                                    
                                    if (matchingFile != null)
                                    {
                                        dto.ImageData = await File.ReadAllBytesAsync(matchingFile);
                                        dto.ImageExtension = Path.GetExtension(matchingFile).TrimStart('.').ToLower();
                                        dto.ImageMimeType = $"image/{dto.ImageExtension}";
                                        _logger.LogInformation("Found matching image: {Path}", matchingFile);
                                        break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to search images in {Folder}", folder);
                                }
                            }
                        }
                    }
                    else
                    {
                        // imgStr was empty in Excel — try finding matching image in library by SKU!
                        var skuToFind = dto.Sku ?? dto.VariantSku;
                        if (!string.IsNullOrWhiteSpace(skuToFind))
                        {
                            var searchFolders = new List<string>();
                            if (!string.IsNullOrWhiteSpace(localImageFolderPath) && Directory.Exists(localImageFolderPath))
                                searchFolders.Add(localImageFolderPath);

                            var libraryFolder = Path.Combine(_contentRootPath, "App_Data", "Imports", "ImageLibrary");
                            if (Directory.Exists(libraryFolder))
                                searchFolders.Add(libraryFolder);

                            var cleanTargetSku = skuToFind.Trim();
                            var candidateNames = new[]
                            {
                                $"{cleanTargetSku}.webp", $"SKU-{cleanTargetSku}.webp",
                                $"{cleanTargetSku}.jpg",  $"SKU-{cleanTargetSku}.jpg",
                                $"{cleanTargetSku}.jpeg", $"SKU-{cleanTargetSku}.jpeg",
                                $"{cleanTargetSku}.png",  $"SKU-{cleanTargetSku}.png"
                            };

                            foreach (var folder in searchFolders)
                            {
                                try
                                {
                                    var directoryFiles = Directory.GetFiles(folder);
                                    var matchingFile = directoryFiles.FirstOrDefault(f =>
                                        candidateNames.Any(c => string.Equals(Path.GetFileName(f), c, StringComparison.OrdinalIgnoreCase)));

                                    if (matchingFile != null)
                                    {
                                        dto.ImageData = await File.ReadAllBytesAsync(matchingFile);
                                        dto.ImageExtension = Path.GetExtension(matchingFile).TrimStart('.').ToLower();
                                        dto.ImageMimeType = $"image/{dto.ImageExtension}";
                                        _logger.LogInformation("Found SKU-matched image in library: {Path}", matchingFile);
                                        break;
                                    }
                                }
                                catch { /* ignore */ }
                            }
                        }
                    }
                }

                var (status, reason) = await SaveProductAsync(dto);
                result.Rows.Add(new ImportRowResult { RowNumber = rowNum, Name = dto.Name, Status = status, Reason = reason });

                if (status == ImportRowStatus.Imported) result.Imported++;
                else if (status == ImportRowStatus.Updated)  result.Updated++;
                else result.Skipped++;
            }
        }

        private async Task SaveCategoryAsync(CategoryImportDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return;

            var validation = await _categoryValidator.ValidateAsync(dto);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Category '{Name}' failed validation: {Errors}",
                    dto.Name, string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)));
                return;
            }

            var existing = (await _unitOfWork.Categories.FindAsync(c => c.Name == dto.Name)).FirstOrDefault();

            if (existing == null)
                existing = new Category { Name = dto.Name! };

            existing.Description  = dto.Description;
            existing.DisplayOrder = dto.DisplayOrder;

            if (!string.IsNullOrWhiteSpace(dto.ParentCategoryName))
            {
                var parentKey = dto.ParentCategoryName.Trim();
                if (!_categoryCache.TryGetValue(parentKey, out var parent))
                {
                    parent = (await _unitOfWork.Categories.FindAsync(c => c.Name == dto.ParentCategoryName)).FirstOrDefault();
                    if (parent != null)
                    {
                        _categoryCache[parentKey] = parent;
                    }
                }
                if (parent != null) existing.ParentCategoryId = parent.Id;
            }

            if (existing.Id == default)
                await _unitOfWork.Categories.AddAsync(existing);
            else
                _unitOfWork.Categories.Update(existing);

            await _unitOfWork.SaveChangesAsync();

            // Cache the saved category
            _categoryCache[existing.Name.Trim()] = existing;

            // Save SEO metadata for category details page
            await GenerateAndSaveCategorySeoAsync(existing, dto.MetaTitle, dto.MetaDescription, dto.MetaKeywords);
        }

        /// <summary>Returns (status, reason) for the product row.</summary>
        private async Task<(ImportRowStatus status, string reason)> SaveProductAsync(ProductImportDto dto)
        {
            // ── 1. Check if this is a child variant row under a parent SKU ──────
            bool isChildVariantRow = !string.IsNullOrWhiteSpace(dto.ParentSku) && 
                                     !string.Equals(dto.ParentSku.Trim(), dto.Sku?.Trim(), StringComparison.OrdinalIgnoreCase);

            if (isChildVariantRow)
            {
                var parentSku = dto.ParentSku!.Trim();
                var parentProduct = (await _unitOfWork.Products.FindAsync(p => p.Sku == parentSku)).FirstOrDefault();
                if (parentProduct == null && !string.IsNullOrWhiteSpace(dto.Name))
                {
                    parentProduct = (await _unitOfWork.Products.FindAsync(p => p.Name == dto.Name)).FirstOrDefault();
                }

                if (parentProduct != null)
                {
                    await SaveProductVariantAsync(parentProduct, dto);
                    return (ImportRowStatus.Imported, $"تم استيراد الفارينت ({dto.Size ?? dto.Color ?? dto.VariantSku}) للمنتج {parentProduct.Name}");
                }
                else
                {
                    _logger.LogWarning("Parent product with SKU '{ParentSku}' not found for variant row '{VariantSku}'", parentSku, dto.VariantSku);
                    return (ImportRowStatus.Skipped, $"لم يتم العثور على المنتج الأساسي برمز SKU: {parentSku}");
                }
            }

            // ── 2. Standard Parent Product Row ──────────────────────────────────
            if (string.IsNullOrWhiteSpace(dto.Name))
                return (ImportRowStatus.Skipped, "اسم المنتج فارغ");

            // Auto-fill category if only ParentCategory is specified
            if (string.IsNullOrWhiteSpace(dto.CategoryName) && !string.IsNullOrWhiteSpace(dto.ParentCategoryName))
            {
                dto.CategoryName = dto.ParentCategoryName;
            }

            // Use sheet SKU if provided; otherwise auto-generate
            bool hasSku = !string.IsNullOrWhiteSpace(dto.Sku);
            if (!hasSku)
                dto.Sku = GenerateSku(dto.Name);

            var validation = await _productValidator.ValidateAsync(dto);
            if (!validation.IsValid)
            {
                var errors = string.Join("، ", validation.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Product '{Name}' failed validation: {Errors}", dto.Name, errors);
                return (ImportRowStatus.Skipped, $"فشل التحقق: {errors}");
            }

            // ── Resolve/auto-create category hierarchy ──────────────────────
            // Sheet convention:
            //   ParentCat   = Level 1 root (e.g. "أدوات صحية")
            //   CategoryName = Level 2 sub  (e.g. "محول فلتر")
            //   MicroCat    = Level 3 leaf  (optional)
            //
            // Legacy convention (no ParentCat column):
            //   CategoryName = Level 1 root
            //
            // We detect which mode we're in by checking if ParentCategoryName is set.

            bool hasParentCol = !string.IsNullOrWhiteSpace(dto.ParentCategoryName);

            // Determine the true Level-1 root name
            string rootCatName = hasParentCol
                ? dto.ParentCategoryName!.Trim()   // ParentCat column is the real root
                : dto.CategoryName?.Trim() ?? string.Empty;

            // Determine the Level-2 sub name (only when ParentCat column exists)
            string? subCatName = hasParentCol
                ? dto.CategoryName?.Trim()         // CategoryName column is the sub
                : null;

            if (string.Equals(subCatName, rootCatName, StringComparison.OrdinalIgnoreCase))
            {
                subCatName = null;
            }

            if (string.IsNullOrWhiteSpace(rootCatName))
            {
                _logger.LogWarning("Category name is missing for product '{Name}'", dto.Name);
                return (ImportRowStatus.Skipped, "اسم التصنيف الرئيسي مفقود");
            }

            // 1. Resolve/Create Level 1 (Root Category — no parent)
            if (!_categoryCache.TryGetValue(rootCatName, out var mainCategory))
            {
                mainCategory = (await _unitOfWork.Categories.FindAsync(c => c.Name == rootCatName && c.ParentCategoryId == null)).FirstOrDefault();
                if (mainCategory == null)
                {
                    mainCategory = new Category { Name = rootCatName };
                    await _unitOfWork.Categories.AddAsync(mainCategory);
                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("Auto-created root category '{Cat}'", rootCatName);
                }
                _categoryCache[rootCatName] = mainCategory;
            }

            Category leafCategory = mainCategory;

            // 2. Resolve/Create Level 2 (Sub-category)
            if (!string.IsNullOrWhiteSpace(subCatName))
            {
                var subKey = $"{rootCatName} > {subCatName}";
                if (!_categoryCache.TryGetValue(subKey, out var subCategory))
                {
                    subCategory = (await _unitOfWork.Categories.FindAsync(c => c.Name == subCatName && c.ParentCategoryId == mainCategory.Id)).FirstOrDefault();
                    if (subCategory == null)
                    {
                        subCategory = new Category
                        {
                            Name = subCatName,
                            ParentCategoryId = mainCategory.Id
                        };
                        await _unitOfWork.Categories.AddAsync(subCategory);
                        await _unitOfWork.SaveChangesAsync();
                        _logger.LogInformation("Auto-created sub-category '{Sub}' under root '{Root}'", subCatName, rootCatName);
                    }
                    _categoryCache[subKey] = subCategory;
                }
                leafCategory = subCategory;

                // 3. Resolve/Create Level 3 (Micro-category)
                if (!string.IsNullOrWhiteSpace(dto.MicroCategoryName))
                {
                    var microKey = $"{subKey} > {dto.MicroCategoryName.Trim()}";
                    if (!_categoryCache.TryGetValue(microKey, out var microCategory))
                    {
                        microCategory = (await _unitOfWork.Categories.FindAsync(c => c.Name == dto.MicroCategoryName && c.ParentCategoryId == subCategory.Id)).FirstOrDefault();
                        if (microCategory == null)
                        {
                            microCategory = new Category
                            {
                                Name = dto.MicroCategoryName,
                                ParentCategoryId = subCategory.Id
                            };
                            await _unitOfWork.Categories.AddAsync(microCategory);
                            await _unitOfWork.SaveChangesAsync();
                            _logger.LogInformation("Auto-created micro-category '{Micro}' under sub '{Sub}'", dto.MicroCategoryName, subCatName);
                        }
                        _categoryCache[microKey] = microCategory;
                    }
                    leafCategory = microCategory;
                }
            }
            else if (!hasParentCol && !string.IsNullOrWhiteSpace(dto.MicroCategoryName))
            {
                // Legacy: CategoryName = root, MicroCategoryName = sub
                var microKey = $"{rootCatName} > {dto.MicroCategoryName.Trim()}";
                if (!_categoryCache.TryGetValue(microKey, out var microCategory))
                {
                    microCategory = (await _unitOfWork.Categories.FindAsync(c => c.Name == dto.MicroCategoryName && c.ParentCategoryId == mainCategory.Id)).FirstOrDefault();
                    if (microCategory == null)
                    {
                        microCategory = new Category
                        {
                            Name = dto.MicroCategoryName,
                            ParentCategoryId = mainCategory.Id
                        };
                        await _unitOfWork.Categories.AddAsync(microCategory);
                        await _unitOfWork.SaveChangesAsync();
                        _logger.LogInformation("Auto-created sub-category '{Sub}' under root '{Root}' (legacy mode)", dto.MicroCategoryName, rootCatName);
                    }
                    _categoryCache[microKey] = microCategory;
                }
                leafCategory = microCategory;
            }

            if (!string.IsNullOrWhiteSpace(dto.CategoryIcon) && leafCategory.ImageUrl != dto.CategoryIcon)
            {
                leafCategory.ImageUrl = dto.CategoryIcon;
                _unitOfWork.Categories.Update(leafCategory);
                await _unitOfWork.SaveChangesAsync();
            }

            // ── Parse Status ─────────────────────────────────────────────────
            var statusStr = dto.Status?.Trim();
            var status = Bolcko.Domain.Enums.ProductStatus.InStock;
            if (!string.IsNullOrWhiteSpace(statusStr))
            {
                if (statusStr.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
                    statusStr.Equals("نعم", StringComparison.OrdinalIgnoreCase) ||
                    statusStr.Equals("متاح", StringComparison.OrdinalIgnoreCase) ||
                    statusStr.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                    statusStr.Equals("InStock", StringComparison.OrdinalIgnoreCase) ||
                    statusStr.Equals("1", StringComparison.OrdinalIgnoreCase))
                {
                    status = Bolcko.Domain.Enums.ProductStatus.InStock;
                }
                else if (statusStr.Equals("Inactive", StringComparison.OrdinalIgnoreCase) ||
                         statusStr.Equals("لا", StringComparison.OrdinalIgnoreCase) ||
                         statusStr.Equals("غير متاح", StringComparison.OrdinalIgnoreCase) ||
                         statusStr.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                         statusStr.Equals("OutOfStock", StringComparison.OrdinalIgnoreCase) ||
                         statusStr.Equals("0", StringComparison.OrdinalIgnoreCase))
                {
                    status = Bolcko.Domain.Enums.ProductStatus.OutOfStock;
                }
            }

            // ── Upsert: find by SKU (if sheet provided one) or by name+category ─
            Product? product = null;
            bool isNew;

            if (hasSku)
            {
                product = (await _unitOfWork.Products.FindAsync(p => p.Sku == dto.Sku)).FirstOrDefault();
            }

            if (product == null)
            {
                product = (await _unitOfWork.Products.FindAsync(
                    p => p.Name == dto.Name && p.CategoryId == leafCategory.Id)).FirstOrDefault();
            }

            isNew = product == null;
            if (isNew)
                product = new Product { Sku = dto.Sku, CreatedAt = DateTime.UtcNow };

            product!.Name               = dto.Name;
            product.NameEn             = dto.NameEn;
            product.Description         = dto.Description;
            product.DescriptionEn       = dto.DescriptionEn;
            product.CategoryId          = leafCategory.Id;
            product.RetailPrice         = dto.RetailPrice;
            product.UnitOfMeasure       = dto.UnitOfMeasure;
            product.StockQuantity       = dto.StockQuantity;
            product.Weight              = dto.Weight;
            product.Dimensions          = dto.Dimensions;
            product.Brand               = dto.Brand;
            product.CountryOfOrigin     = dto.CountryOfOrigin;
            product.IsOversized         = dto.IsOversized;
            product.BulkPricingAvailable = dto.BulkPricingAvailable;
            product.Status              = status;
            product.UpdatedAt           = DateTime.UtcNow;

            if (isNew) await _unitOfWork.Products.AddAsync(product);
            else       _unitOfWork.Products.Update(product);

            await _unitOfWork.SaveChangesAsync();

            // Cache the English translations in MemoryCache to support zero DB changes dynamic translation
            if (!string.IsNullOrWhiteSpace(dto.NameEn))
            {
                var cacheKey = $"translation_en_{dto.Name.Trim().GetHashCode()}";
                _memoryCache.Set(cacheKey, dto.NameEn.Trim(), TimeSpan.FromDays(30));
            }
            if (!string.IsNullOrWhiteSpace(dto.DescriptionEn) && !string.IsNullOrWhiteSpace(dto.Description))
            {
                var cacheKey = $"translation_en_{dto.Description.Trim().GetHashCode()}";
                _memoryCache.Set(cacheKey, dto.DescriptionEn.Trim(), TimeSpan.FromDays(30));
            }

            // Save image
            await SaveProductImageAsync(product, dto);

            // Save Variant if the main product row contains variant parameters
            if (!string.IsNullOrWhiteSpace(dto.Size) || !string.IsNullOrWhiteSpace(dto.Color) || !string.IsNullOrWhiteSpace(dto.VariantSku))
            {
                await SaveProductVariantAsync(product, dto);
            }

            // Always generate & save rich SEO metadata for product details pages
            await GenerateAndSaveProductSeoAsync(product, leafCategory, dto);

            return isNew
                ? (ImportRowStatus.Imported, string.Empty)
                : (ImportRowStatus.Updated,  string.Empty);
        }

        private async Task SaveProductVariantAsync(Product product, ProductImportDto dto)
        {
            try
            {
                var variantSku = !string.IsNullOrWhiteSpace(dto.VariantSku) 
                    ? dto.VariantSku.Trim() 
                    : (!string.IsNullOrWhiteSpace(dto.Sku) && dto.Sku != product.Sku ? dto.Sku.Trim() : $"{product.Sku}-VAR-{Guid.NewGuid():N}"[..12]);

                var existingVariant = (await _unitOfWork.ProductVariants.FindAsync(v => 
                    (v.ProductId == product.Id && v.Sku == variantSku) || 
                    (v.ProductId == product.Id && v.Size == dto.Size && v.Color == dto.Color))).FirstOrDefault();

                bool isNewVariant = existingVariant == null;
                if (isNewVariant)
                {
                    existingVariant = new ProductVariant
                    {
                        ProductId = product.Id,
                        Sku = variantSku,
                        CreatedAt = DateTime.UtcNow
                    };
                }

                existingVariant!.Size = dto.Size;
                existingVariant.Color = dto.Color;
                existingVariant.PackagingUnit = dto.PackagingUnit;
                existingVariant.CountryOfOrigin = dto.CountryOfOrigin ?? product.CountryOfOrigin;
                existingVariant.Price = dto.VariantPrice ?? dto.RetailPrice;
                existingVariant.StockQuantity = dto.VariantStock ?? dto.StockQuantity;
                existingVariant.UpdatedAt = DateTime.UtcNow;

                // Handle Variant Image
                if (dto.ImageData is { Length: > 0 })
                {
                    try
                    {
                        using var ms = new MemoryStream(dto.ImageData);
                        var savedImg = await _imageService.SaveImageAsync(ms, $"variant.{dto.ImageExtension ?? "webp"}", "variants");
                        if (!string.IsNullOrEmpty(savedImg)) existingVariant.ImageUrl = savedImg;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to save variant image for SKU {Sku}", variantSku);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(dto.VariantImageUrl))
                {
                    if (dto.VariantImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var savedImg = await _imageService.DownloadAndCompressImageAsync(dto.VariantImageUrl, "variants");
                            if (!string.IsNullOrEmpty(savedImg)) existingVariant.ImageUrl = savedImg;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to download variant image from {Url}", dto.VariantImageUrl);
                        }
                    }
                    else
                    {
                        existingVariant.ImageUrl = dto.VariantImageUrl;
                    }
                }
                else if (string.IsNullOrEmpty(existingVariant.ImageUrl))
                {
                    existingVariant.ImageUrl = product.ImageUrl;
                }

                if (isNewVariant)
                {
                    await _unitOfWork.ProductVariants.AddAsync(existingVariant);
                }
                else
                {
                    _unitOfWork.ProductVariants.Update(existingVariant);
                }

                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Saved variant {Sku} for product ID {ProductId}", variantSku, product.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save variant for product ID {ProductId}", product.Id);
            }
        }

        private async Task SaveProductImageAsync(Product product, ProductImportDto dto)
        {
            _logger.LogInformation("Starting to save image for product {ProductId} ({ProductName})", product.Id, product.Name);
            try
            {
                string? imageUrl = null;

                if (dto.ImageData is { Length: > 0 })
                {
                    _logger.LogInformation("Saving product image from ImageData (size: {Size} bytes)", dto.ImageData.Length);
                    using var ms = new MemoryStream(dto.ImageData);
                    imageUrl = await _imageService.SaveImageAsync(ms, $"image.{dto.ImageExtension ?? "jpg"}", "products");
                    _logger.LogInformation("Saved product image to: {Url}", imageUrl);
                }
                else if (!string.IsNullOrWhiteSpace(dto.ImageBase64))
                {
                    if (dto.ImageBase64!.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            _logger.LogInformation("Downloading and saving image from URL: {Url}", dto.ImageBase64);
                            imageUrl = await _imageService.DownloadAndCompressImageAsync(dto.ImageBase64, "products");
                            _logger.LogInformation("Downloaded and saved image to: {Url}", imageUrl);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to download image from {Url}", dto.ImageBase64);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Decoding base64 image");
                        var (imageBytes, ext) = DecodeBase64Image(dto.ImageBase64!);
                        if (imageBytes != null)
                        {
                            using var ms = new MemoryStream(imageBytes);
                            imageUrl = await _imageService.SaveImageAsync(ms, $"image.{ext}", "products");
                            _logger.LogInformation("Saved base64 image to: {Url}", imageUrl);
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    product.ImageUrl = imageUrl;
                }

                if (dto.AdditionalImages != null && dto.AdditionalImages.Any())
                {
                    int order = 1;
                    foreach (var addImg in dto.AdditionalImages)
                    {
                        _logger.LogInformation("Saving additional image {Order}", order);
                        try
                        {
                            using var ms = new MemoryStream(addImg.Data);
                            var addImageUrl = await _imageService.SaveImageAsync(ms, $"image.{addImg.Ext}", "products");
                            if (!string.IsNullOrWhiteSpace(addImageUrl))
                            {
                                product.Images.Add(new ProductImage
                                {
                                    Url          = addImageUrl,
                                    DisplayOrder = order++
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to save additional image for product {ProductName}", product.Name);
                        }
                    }
                }

                _unitOfWork.Products.Update(product);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save image for product {ProductName} (ID: {ProductId})", product.Name, product.Id);
            }
        }

        // ── SKU Generation ───────────────────────────────────────────────────
        private static string GenerateSku(string name)
        {
            var ascii  = Regex.Replace(name ?? "", @"[^A-Za-z0-9]", "").ToUpperInvariant();
            var prefix = ascii.Length >= 3 ? ascii[..Math.Min(6, ascii.Length)] : "PRD";
            var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            return $"{prefix}-{suffix}";
        }

        // ── Column index builder ─────────────────────────────────────────────
        private static Dictionary<string, int> BuildColumnIndex(IXLWorksheet ws)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var range = ws.RangeUsed();
            if (range == null) return map;
            var headerRow = range.RowsUsed().FirstOrDefault();
            if (headerRow == null) return map;

            foreach (var cell in headerRow.CellsUsed())
            {
                var raw = cell.GetString().Trim();
                if (string.IsNullOrWhiteSpace(raw)) continue;

                // 1. Direct dictionary match
                if (_colMap.TryGetValue(raw, out var mappedDirect))
                {
                    if (!map.ContainsKey(mappedDirect))
                        map[mappedDirect] = cell.Address.ColumnNumber;
                }
                else
                {
                    // 2. Normalized match (remove extra whitespace)
                    var cleanHeader = Regex.Replace(raw, @"\s+", " ").Trim();
                    if (_colMap.TryGetValue(cleanHeader, out var mappedClean))
                    {
                        if (!map.ContainsKey(mappedClean))
                            map[mappedClean] = cell.Address.ColumnNumber;
                    }
                    else
                    {
                        // 3. Fallback: match by key itself
                        if (!map.ContainsKey(raw))
                            map[raw] = cell.Address.ColumnNumber;
                    }
                }
            }
            return map;
        }

        private static string GetCell(IXLRangeRow row, Dictionary<string, int> colIdx, string key)
        {
            if (colIdx.TryGetValue(key, out var colNum))
            {
                var cell = row.Cell(colNum);
                
                // إذا كانت الخلية رقمية، نقرأ قيمتها كـ Double لتجنب مشاكل فواصل الثقافة المحلية (Culture)
                if (cell.Value.IsNumber)
                {
                    return cell.Value.GetNumber().ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                if (cell.HasFormula)
                {
                    var f = cell.FormulaA1;
                    if (!string.IsNullOrWhiteSpace(f)) return "=" + f;
                }
                
                return cell.GetString().Trim();
            }
            return string.Empty;
        }

        private static IXLWorksheet? FindSheet(XLWorkbook wb, params string[] names)
        {
            foreach (var name in names)
            {
                var ws = wb.Worksheets.FirstOrDefault(w =>
                    w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (ws != null) return ws;
            }
            return null;
        }

        // ── Base64 image decoder ─────────────────────────────────────────────

        private static (byte[]? bytes, string ext) DecodeBase64Image(string b64)
        {
            try
            {
                var ext = "jpg";
                if (b64.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                {
                    var semi  = b64.IndexOf(';');
                    var slash = b64.IndexOf('/');
                    if (semi > slash) ext = b64[(slash + 1)..semi];
                    var comma = b64.IndexOf(',');
                    if (comma > -1) b64 = b64[(comma + 1)..];
                }
                return (Convert.FromBase64String(b64), ext);
            }
            catch { return (null, "jpg"); }
        }

        private decimal? CleanAndParsePrice(string? priceStr)
        {
            if (string.IsNullOrWhiteSpace(priceStr)) return null;

            var s = priceStr.Trim();
            // استبدال الفاصلة العشرية العربية ٫ بنقطة
            s = s.Replace('٫', '.');

            // إذا كانت الفاصلة عادية ولا يوجد نقطة مثل "8,45" أو "1,82"
            if (s.Contains(',') && !s.Contains('.'))
            {
                var commaIdx = s.LastIndexOf(',');
                var afterComma = s[(commaIdx + 1)..].Trim();
                var digitsAfter = Regex.Replace(afterComma, @"[^\d]", "");
                // إذا كان بعد الفاصلة خانة إلى 3 خانات، فهي فاصلة عشرية وليست فاصلة آلاف
                if (digitsAfter.Length > 0 && digitsAfter.Length <= 3)
                {
                    s = s[..commaIdx] + "." + afterComma;
                }
                else
                {
                    s = s.Replace(",", "");
                }
            }
            else
            {
                s = s.Replace(",", "");
            }

            var cleaned = Regex.Replace(s, @"[^\d\.]", "");
            if (decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var price))
            {
                return price;
            }
            return null;
        }

        private static int? CleanAndParseInt(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var cleaned = Regex.Replace(raw.Trim(), @"[^\d\.]", "");
            if (decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var dec))
            {
                return (int)Math.Round(dec);
            }
            return null;
        }

        private string? CleanAndExtractSku(string? skuStr)
        {
            if (string.IsNullOrWhiteSpace(skuStr)) return null;
            // فصل على الفواصل والأسطر فقط وتجنب فصل الـ SKU الذي يحتوي على شرطة (-)
            var parts = skuStr.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0].Trim() : null;
        }

        private static string CleanHtmlDescription(string? desc)
        {
            if (string.IsNullOrWhiteSpace(desc)) return string.Empty;
            var trimmed = desc.Trim();
            // تنظيف وسوم الـ HTML البسيطة مثل <p> و </p> من الوصف
            if (trimmed.StartsWith("<p>", StringComparison.OrdinalIgnoreCase) && trimmed.EndsWith("</p>", StringComparison.OrdinalIgnoreCase))
            {
                return Regex.Replace(trimmed, @"<[^>]+>", " ").Trim();
            }
            return trimmed;
        }

        // ════════════════════════════════════════════════════════════════════
        //  BACKGROUND JOB RUNNERS & SEO HELPERS
        // ════════════════════════════════════════════════════════════════════
        public async Task ProcessUnifiedExcelImportJobAsync(string importId, string filePath, string? zipFileOrFolderPath = null)
        {
            _logger.LogInformation("Starting background Excel import job (ImportId: {ImportId}, Path: {Path}, ZipOrFolder: {ZipOrFolder})", importId, filePath, zipFileOrFolderPath);
            var result = new ImportResult();
            string? extractedImagesFolderPath = null;

            try
            {
                if (!string.IsNullOrWhiteSpace(zipFileOrFolderPath))
                {
                    if (Directory.Exists(zipFileOrFolderPath))
                    {
                        // It's already extracted by the controller! Just use it directly.
                        extractedImagesFolderPath = zipFileOrFolderPath;
                        _logger.LogInformation("Using pre-extracted images folder: {Path}", extractedImagesFolderPath);
                    }
                    else if (File.Exists(zipFileOrFolderPath))
                    {
                        // Fallback in case it's a file path
                        extractedImagesFolderPath = Path.Combine(
                            _contentRootPath, "App_Data", "Imports", "Extracted", Guid.NewGuid().ToString());
                        Directory.CreateDirectory(extractedImagesFolderPath);
                        _logger.LogInformation("Extracting images ZIP to {Path}", extractedImagesFolderPath);

                        using (var zipStream = File.OpenRead(zipFileOrFolderPath))
                        using (var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Read))
                        {
                            foreach (var entry in archive.Entries)
                            {
                                if (string.IsNullOrWhiteSpace(entry.Name) || entry.Name.StartsWith('.'))
                                    continue;

                                var fileName = Path.GetFileName(entry.FullName);
                                if (string.IsNullOrWhiteSpace(fileName)) continue;

                                var entryPath = Path.Combine(extractedImagesFolderPath, fileName);

                                int counter = 1;
                                var baseName = Path.GetFileNameWithoutExtension(fileName);
                                var fileExt  = Path.GetExtension(fileName);
                                while (File.Exists(entryPath))
                                {
                                    entryPath = Path.Combine(extractedImagesFolderPath, $"{baseName}_{counter}{fileExt}");
                                    counter++;
                                }

                                using (var entryStream = entry.Open())
                                using (var fileStream  = new FileStream(entryPath, FileMode.Create, FileAccess.Write))
                                {
                                    await entryStream.CopyToAsync(fileStream);
                                }
                            }
                        }
                        _logger.LogInformation("ZIP extraction complete. Files: {Count}",
                            Directory.GetFiles(extractedImagesFolderPath).Length);
                    }
                }

                result = await ProcessUnifiedExcelImportAsync(filePath, extractedImagesFolderPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing background Excel import job");
                result.HasError = true;
                result.ErrorMessage = ex.Message;
            }
            finally
            {
                await WriteResultFileAsync(importId, result);

                try
                {
                    if (File.Exists(filePath)) File.Delete(filePath);
                    if (!string.IsNullOrWhiteSpace(zipFileOrFolderPath) && File.Exists(zipFileOrFolderPath)) File.Delete(zipFileOrFolderPath);
                    if (!string.IsNullOrWhiteSpace(extractedImagesFolderPath) && Directory.Exists(extractedImagesFolderPath))
                        Directory.Delete(extractedImagesFolderPath, true);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to clean up files after background Excel import job");
                }
            }
        }

        public async Task ProcessGoogleSheetImportJobAsync(string importId, string googleSheetUrl, string? zipFilePath = null)
        {
            _logger.LogInformation("Starting background Google Sheets import job (ImportId: {ImportId}, Url: {Url})", importId, googleSheetUrl);
            var result = new ImportResult();
            string? extractedImagesFolderPath = null;
            string? tempExcelPath = null;

            try
            {
                if (!string.IsNullOrWhiteSpace(zipFilePath) && File.Exists(zipFilePath))
                {
                    extractedImagesFolderPath = Path.Combine(Path.GetTempPath(), "BolckoImports", Guid.NewGuid().ToString());
                    Directory.CreateDirectory(extractedImagesFolderPath);
                    _logger.LogInformation("Extracting images ZIP to {Path}", extractedImagesFolderPath);

                    using (var zipStream = File.OpenRead(zipFilePath))
                    using (var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Read))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            if (string.IsNullOrWhiteSpace(entry.Name) || entry.Name.StartsWith('.'))
                                continue;

                            var fileName = Path.GetFileName(entry.FullName);
                            var entryPath = Path.Combine(extractedImagesFolderPath, fileName);

                            int counter = 1;
                            var originalFileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                            var fileExt = Path.GetExtension(fileName);
                            while (File.Exists(entryPath))
                            {
                                entryPath = Path.Combine(extractedImagesFolderPath, $"{originalFileNameWithoutExt}_{counter}{fileExt}");
                                counter++;
                            }

                            using (var entryStream = entry.Open())
                            using (var fileStream = new FileStream(entryPath, FileMode.Create, FileAccess.Write))
                            {
                                await entryStream.CopyToAsync(fileStream);
                            }
                        }
                    }
                }

                var sheetId = ExtractGoogleSheetId(googleSheetUrl);
                if (string.IsNullOrWhiteSpace(sheetId))
                {
                    result.HasError = true;
                    result.ErrorMessage = "Invalid Google Sheet URL";
                }
                else
                {
                    var exportUrl = $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=xlsx";
                    using var httpClient = new System.Net.Http.HttpClient();
                    var excelBytes = await httpClient.GetByteArrayAsync(exportUrl);

                    var tempFolder = Path.Combine(Path.GetTempPath(), "BolckoImports");
                    Directory.CreateDirectory(tempFolder);
                    tempExcelPath = Path.Combine(tempFolder, $"{Guid.NewGuid()}.xlsx");
                    await File.WriteAllBytesAsync(tempExcelPath, excelBytes);

                    result = await ProcessUnifiedExcelImportAsync(tempExcelPath, extractedImagesFolderPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing background Google Sheets import job");
                result.HasError = true;
                result.ErrorMessage = ex.Message;
            }
            finally
            {
                await WriteResultFileAsync(importId, result);

                try
                {
                    if (tempExcelPath != null && File.Exists(tempExcelPath)) File.Delete(tempExcelPath);
                    if (!string.IsNullOrWhiteSpace(zipFilePath) && File.Exists(zipFilePath)) File.Delete(zipFilePath);
                    if (!string.IsNullOrWhiteSpace(extractedImagesFolderPath) && Directory.Exists(extractedImagesFolderPath))
                        Directory.Delete(extractedImagesFolderPath, true);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to clean up files after background Google Sheets import job");
                }
            }
        }

        public async Task ProcessImagesZipImportJobAsync(string importId, string zipFileOrFolderPath)
        {
            var result = new ImportResult();
            _logger.LogInformation("Starting background Images-only ZIP import job {ImportId}", importId);

            string? extractedImagesFolderPath = null;
            try
            {
                var libraryFolder = Path.Combine(_contentRootPath, "App_Data", "Imports", "ImageLibrary");
                Directory.CreateDirectory(libraryFolder);

                if (!string.IsNullOrWhiteSpace(zipFileOrFolderPath))
                {
                    if (Directory.Exists(zipFileOrFolderPath))
                    {
                        extractedImagesFolderPath = zipFileOrFolderPath;
                    }
                    else if (File.Exists(zipFileOrFolderPath))
                    {
                        extractedImagesFolderPath = Path.Combine(_contentRootPath, "App_Data", "Imports", "Extracted", Guid.NewGuid().ToString());
                        Directory.CreateDirectory(extractedImagesFolderPath);

                        using var zipStream = File.OpenRead(zipFileOrFolderPath);
                        using var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Read);
                        foreach (var entry in archive.Entries)
                        {
                            if (string.IsNullOrWhiteSpace(entry.Name) || entry.Name.StartsWith('.')) continue;
                            var fileName = Path.GetFileName(entry.FullName);
                            if (string.IsNullOrWhiteSpace(fileName)) continue;

                            var destPath = Path.Combine(extractedImagesFolderPath, fileName);
                            using var entryStream = entry.Open();
                            using var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write);
                            await entryStream.CopyToAsync(fileStream);
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(extractedImagesFolderPath) && Directory.Exists(extractedImagesFolderPath))
                {
                    var imageFiles = Directory.GetFiles(extractedImagesFolderPath);
                    _logger.LogInformation("Processing {Count} images from ZIP to match with database products...", imageFiles.Length);

                    // Copy all into persistent ImageLibrary as well
                    foreach (var imgFile in imageFiles)
                    {
                        var fileName = Path.GetFileName(imgFile);
                        var libTarget = Path.Combine(libraryFolder, fileName);
                        try { File.Copy(imgFile, libTarget, true); } catch { /* ignore */ }
                    }

                    int matchedProducts = 0;
                    int matchedVariants = 0;
                    int totalFiles = imageFiles.Length;
                    int batchPending = 0;

                    // Match with DB Products & Variants
                    foreach (var imgFile in imageFiles)
                    {
                        var fileName = Path.GetFileName(imgFile);
                        var rawName = Path.GetFileNameWithoutExtension(fileName).Trim();
                        var cleanSku = rawName.StartsWith("SKU-", StringComparison.OrdinalIgnoreCase)
                            ? rawName[4..].Trim()
                            : rawName;

                        bool matched = false;

                        // 1. Try matching with Product (trackChanges: true)
                        var product = (await _unitOfWork.Products.FindAsync(p => p.Sku == rawName || p.Sku == cleanSku || ("SKU-" + p.Sku) == rawName, trackChanges: true)).FirstOrDefault();
                        if (product != null)
                        {
                            try
                            {
                                await using var fileStream = File.OpenRead(imgFile);
                                var savedUrl = await _imageService.SaveImageAsync(fileStream, fileName, "products");
                                if (!string.IsNullOrEmpty(savedUrl))
                                {
                                    product.ImageUrl = savedUrl;
                                    product.UpdatedAt = DateTime.UtcNow;

                                    var existingImage = (await _unitOfWork.ProductImages.FindAsync(pi => pi.ProductId == product.Id && pi.Url == savedUrl, trackChanges: true)).FirstOrDefault();
                                    if (existingImage == null)
                                    {
                                        await _unitOfWork.ProductImages.AddAsync(new ProductImage
                                        {
                                            ProductId = product.Id,
                                            Url = savedUrl,
                                            DisplayOrder = 1
                                        });
                                    }
                                    matchedProducts++;
                                    matched = true;
                                    batchPending++;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to attach image {File} to product {Sku}", fileName, product.Sku);
                            }
                        }

                        // 2. Try matching with ProductVariant (trackChanges: true)
                        var variant = (await _unitOfWork.ProductVariants.FindAsync(v => v.Sku == rawName || v.Sku == cleanSku || ("SKU-" + v.Sku) == rawName, trackChanges: true)).FirstOrDefault();
                        if (variant != null)
                        {
                            try
                            {
                                await using var fileStream = File.OpenRead(imgFile);
                                var savedUrl = await _imageService.SaveImageAsync(fileStream, fileName, "variants");
                                if (!string.IsNullOrEmpty(savedUrl))
                                {
                                    variant.ImageUrl = savedUrl;
                                    variant.UpdatedAt = DateTime.UtcNow;
                                    matchedVariants++;
                                    matched = true;
                                    batchPending++;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to attach image {File} to variant {Sku}", fileName, variant.Sku);
                            }
                        }

                        if (batchPending >= 50)
                        {
                            try
                            {
                                await _unitOfWork.SaveChangesAsync();
                                batchPending = 0;
                            }
                            catch (Exception saveEx)
                            {
                                _logger.LogWarning(saveEx, "Batch save error during image matching");
                            }
                        }

                        result.Rows.Add(new ImportRowResult
                        {
                            RowNumber = result.Rows.Count + 1,
                            Name = fileName,
                            Status = matched ? ImportRowStatus.Updated : ImportRowStatus.Imported,
                            Reason = matched ? $"تم الربط بنجاح مع المنتج أو المقاس (SKU: {cleanSku})" : "تم الحفظ في مكتبة الصور للربط المستقبلي"
                        });

                        if (matched) result.Updated++;
                        else result.Imported++;
                    }

                    if (batchPending > 0)
                    {
                        try
                        {
                            await _unitOfWork.SaveChangesAsync();
                        }
                        catch (Exception finalSaveEx)
                        {
                            _logger.LogWarning(finalSaveEx, "Final batch save error during image matching");
                        }
                    }

                    // Evict Caches
                    _memoryCache.Remove("Header_Categories_ar");
                    _memoryCache.Remove("Header_Categories_ar-JO");
                    _memoryCache.Remove("Header_Categories_en");
                    _memoryCache.Remove("Header_Categories_en-US");
                    _memoryCache.Remove("Home_FeaturedProducts_ar");
                    _memoryCache.Remove("Home_FeaturedProducts_ar-JO");
                    _memoryCache.Remove("Home_FeaturedProducts_en");
                    _memoryCache.Remove("Home_FeaturedProducts_en-US");

                    _logger.LogInformation("Images ZIP import processed {Total} files: {Products} products matched, {Variants} variants matched.",
                        totalFiles, matchedProducts, matchedVariants);
                }
                else
                {
                    result.HasError = true;
                    result.ErrorMessage = "لم يتم العثور على ملفات الصور.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing images ZIP import");
                result.HasError = true;
                result.ErrorMessage = ex.Message;
            }
            finally
            {
                await WriteResultFileAsync(importId, result);
                try
                {
                    if (!string.IsNullOrWhiteSpace(zipFileOrFolderPath) && File.Exists(zipFileOrFolderPath)) File.Delete(zipFileOrFolderPath);
                    if (!string.IsNullOrWhiteSpace(extractedImagesFolderPath) && Directory.Exists(extractedImagesFolderPath))
                        Directory.Delete(extractedImagesFolderPath, true);
                }
                catch { /* ignore */ }
            }
        }

        private async Task GenerateAndSaveProductSeoAsync(Product product, Category? leafCategory, ProductImportDto dto)
        {
            var brandText = !string.IsNullOrWhiteSpace(product.Brand) ? $" {product.Brand}" : "";
            var originText = !string.IsNullOrWhiteSpace(product.CountryOfOrigin) ? $" منشأ {product.CountryOfOrigin}" : "";
            var catText = leafCategory != null ? $" | قسم {leafCategory.Name}" : "";

            string metaTitle = !string.IsNullOrWhiteSpace(dto.MetaTitle) 
                ? dto.MetaTitle 
                : $"شراء {product.Name}{brandText}{originText} | أسعار التوريد الأردن بلوكو BLOCKO";

            string metaDescription = !string.IsNullOrWhiteSpace(dto.MetaDescription) 
                ? dto.MetaDescription 
                : $"احصل على {product.Name}{brandText}{originText}{catText} بأفضل سعر للبيع والتوريد للمشاريع الإنشائية في الأردن. جودة معتمدة، مواصفات قياسية، وتوصيل فوري من بلوكو BLOCKO.";

            string metaKeywords = !string.IsNullOrWhiteSpace(dto.MetaKeywords) 
                ? dto.MetaKeywords 
                : $"شراء {product.Name}, {product.Brand}, {product.CountryOfOrigin}, {leafCategory?.Name}, مواد بناء الأردن, توريد مشاريع, أسعار مواد البناء, متجر بلوكو, blocko";

            await SaveSeoMetadataAsync($"/Product/Index/{product.Id}", metaTitle, metaDescription, metaKeywords);
            await SaveSeoMetadataAsync($"/Shop/Product/Index/{product.Id}", metaTitle, metaDescription, metaKeywords);
            await SaveSeoMetadataAsync($"Product-{product.Id}", metaTitle, metaDescription, metaKeywords);
        }

        private async Task GenerateAndSaveCategorySeoAsync(Category category, string? metaTitleDto = null, string? metaDescDto = null, string? metaKeyDto = null)
        {
            string metaTitle = !string.IsNullOrWhiteSpace(metaTitleDto) 
                ? metaTitleDto 
                : $"قسم {category.Name} | توريد مواد ومعدات إنشائية - بلوكو BLOCKO";

            string metaDescription = !string.IsNullOrWhiteSpace(metaDescDto) 
                ? metaDescDto 
                : $"تصفح واطلب أفضل منتجات ومستلزمات {category.Name} بالجملة والمفرق في الأردن. أسعار منافسة وتوريد فوري للمشاريع من منصة بلوكو.";

            string metaKeywords = !string.IsNullOrWhiteSpace(metaKeyDto) 
                ? metaKeyDto 
                : $"{category.Name}, لوازم {category.Name}, أسعار {category.Name}, مواد بناء الأردن, توريد مشاريع, بلوكو";

            await SaveSeoMetadataAsync($"/Category/Index/{category.Id}", metaTitle, metaDescription, metaKeywords);
            await SaveSeoMetadataAsync($"/Shop/Category/Index/{category.Id}", metaTitle, metaDescription, metaKeywords);
            await SaveSeoMetadataAsync($"Category-{category.Id}", metaTitle, metaDescription, metaKeywords);
        }

        private async Task SaveSeoMetadataAsync(string pageName, string? title, string? desc, string? keywords)
        {
            if (string.IsNullOrWhiteSpace(pageName)) return;
            try
            {
                var existingSeo = (await _unitOfWork.SEO.FindAsync(s => s.PageName == pageName)).FirstOrDefault();
                if (existingSeo == null)
                {
                    existingSeo = new Bolcko.Domain.Entities.SEO.SEOMetadata
                    {
                        PageName = pageName,
                        PageTitle = title,
                        MetaDescription = desc,
                        MetaKeywords = keywords,
                        LastUpdated = DateTime.UtcNow
                    };
                    await _unitOfWork.SEO.AddAsync(existingSeo);
                }
                else
                {
                    existingSeo.PageTitle = !string.IsNullOrWhiteSpace(title) ? title : existingSeo.PageTitle;
                    existingSeo.MetaDescription = !string.IsNullOrWhiteSpace(desc) ? desc : existingSeo.MetaDescription;
                    existingSeo.MetaKeywords = !string.IsNullOrWhiteSpace(keywords) ? keywords : existingSeo.MetaKeywords;
                    existingSeo.LastUpdated = DateTime.UtcNow;
                    _unitOfWork.SEO.Update(existingSeo);
                }
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Saved SEO metadata for page: {PageName}", pageName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save SEO metadata for page {PageName}", pageName);
            }
        }

        private async Task WriteResultFileAsync(string importId, ImportResult result)
        {
            if (string.IsNullOrWhiteSpace(importId)) return;
            try
            {
                // Use ContentRootPath so the web request reader (GetImportStatus) and
                // the Hangfire background job both resolve the same physical directory,
                // regardless of the process working directory (critical on Linux/Render).
                var folder = Path.Combine(_contentRootPath, "App_Data", "Imports", "Results");
                Directory.CreateDirectory(folder);
                var path = Path.Combine(folder, $"{importId}.json");
                var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(path, json);
                _logger.LogInformation("Saved import result file: {Path}", path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write import result file for {ImportId}", importId);
            }
        }
    }
}
