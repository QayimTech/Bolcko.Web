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
            ["Product Name (English)"]       = "NameEn",
            ["product name english"]         = "NameEn",
            ["name_en"]                      = "NameEn",
            ["الاسم الانجليزي"]              = "NameEn",
            // ── Description ──────────────────────────────────────────────────
            ["الوصف"]                        = "Description",
            ["الوصف (العربي)"]               = "Description",
            ["الوصف (العربية)"]              = "Description",
            ["الوصف (Arabic)"]               = "Description",
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

            ["التصنيف الفرعي"]              = "ParentCategoryName",
            ["الفئة الأم"]                   = "ParentCategoryName",
            ["اسم الفئة الأم"]               = "ParentCategoryName",
            ["parentcategoryname"]           = "ParentCategoryName",

            ["التصنيف الدقيق (Micro)"]       = "MicroCategoryName",
            ["التصنيف الدقيق"]               = "MicroCategoryName",
            ["microcategoryname"]            = "MicroCategoryName",
            // ── SKU / Product code ────────────────────────────────────────────
            ["كود المنتج"]                   = "Sku",
            ["كود_المنتج"]                   = "Sku",
            ["sku"]                          = "Sku",
            ["product code"]                 = "Sku",
            // ── Price ─────────────────────────────────────────────────────────
            ["السعر"]                        = "Price",
            ["price"]                        = "Price",
            ["retailprice"]                  = "Price",
            // ── Stock ─────────────────────────────────────────────────────────
            ["الكمية"]                       = "Stock",
            ["stock"]                        = "Stock",
            ["stockquantity"]                = "Stock",
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
            // ── Brand / Supplier ──────────────────────────────────────────────
            ["البراند"]                      = "Brand",
            ["brand"]                        = "Brand",
            ["اسم المورد"]                   = "Brand",
            ["المورد"]                       = "Brand",
            // ── Country of origin ─────────────────────────────────────────────
            ["بلد المنشأ"]                   = "CountryOfOrigin",
            ["countryoforigin"]              = "CountryOfOrigin",
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
        };

        public BulkImportService(
            IUnitOfWork unitOfWork,
            IValidator<ProductImportDto> productValidator,
            IValidator<CategoryImportDto> categoryValidator,
            ILogger<BulkImportService> logger,
            IMemoryCache memoryCache)
        {
            _unitOfWork = unitOfWork;
            _productValidator = productValidator;
            _categoryValidator = categoryValidator;
            _logger = logger;
            _memoryCache = memoryCache;
        }

        // ════════════════════════════════════════════════════════════════════
        //  UNIFIED EXCEL IMPORT
        // ════════════════════════════════════════════════════════════════════
        public async Task<ImportResult> ProcessUnifiedExcelImportAsync(string filePath)
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
                    await ImportProductsFromWorksheet(productSheet, result);
                }

                _logger.LogInformation("Unified Excel import completed. {Summary}", result.Summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing unified Excel import");
            }

            return result;
        }

        // ════════════════════════════════════════════════════════════════════
        //  UNIFIED JSON IMPORT
        // ════════════════════════════════════════════════════════════════════
        public async Task<ImportResult> ProcessUnifiedJsonImportAsync(string filePath)
        {
            var result = new ImportResult();
            _logger.LogInformation("Starting unified JSON import from {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                _logger.LogError("File not found: {FilePath}", filePath);
                return result;
            }

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // ── Categories ────────────────────────────────────────────
                if (root.TryGetProperty("categories", out var catArray) ||
                    root.TryGetProperty("الفئات", out catArray))
                {
                    foreach (var catEl in catArray.EnumerateArray())
                    {
                        var dto = new CategoryImportDto
                        {
                            Name               = GetJsonString(catEl, "name", "الاسم", "اسم الفئة"),
                            ParentCategoryName = GetJsonString(catEl, "parentCategoryName", "الفئة الأم", "parentcategoryname"),
                            Description        = GetJsonString(catEl, "description", "الوصف"),
                            DisplayOrder       = GetJsonInt(catEl, "displayOrder", "ترتيب العرض")
                        };
                        await SaveCategoryAsync(dto);
                    }
                }

                // ── Products ──────────────────────────────────────────────
                if (root.TryGetProperty("products", out var prodArray) ||
                    root.TryGetProperty("المنتجات", out prodArray))
                {
                    int rowNum = 1;
                    foreach (var prodEl in prodArray.EnumerateArray())
                    {
                        rowNum++;
                        result.TotalRows++;
                        var dto = new ProductImportDto
                        {
                            Sku              = GetJsonString(prodEl, "sku", "كود المنتج", "product code"),
                            Name             = GetJsonString(prodEl, "name", "الاسم", "اسم المنتج"),
                            NameEn           = GetJsonString(prodEl, "nameEn", "Product Name (English)"),
                            CategoryName     = GetJsonString(prodEl, "categoryName", "اسم الفئة", "categoryname"),
                            Description      = GetJsonString(prodEl, "description", "الوصف"),
                            DescriptionEn    = GetJsonString(prodEl, "descriptionEn", "Description (English)"),
                            RetailPrice      = GetJsonDecimal(prodEl, "price", "السعر", "retailPrice"),
                            StockQuantity    = GetJsonInt(prodEl, "stock", "الكمية", "stockQuantity"),
                            UnitOfMeasure    = GetJsonString(prodEl, "unitOfMeasure", "وحدة القياس", "الوحدة") is { Length: > 0 } u ? u : "Unit",
                            Status           = GetJsonString(prodEl, "status", "الحالة") is { Length: > 0 } s ? s : "Active",
                            Brand            = GetJsonString(prodEl, "brand", "البراند", "اسم المورد"),
                            CountryOfOrigin  = GetJsonString(prodEl, "countryOfOrigin", "بلد المنشأ"),
                            ImageBase64      = GetJsonString(prodEl, "imageBase64", "الصورة")
                        };

                        if (!string.IsNullOrWhiteSpace(dto.ImageBase64))
                            ParseBase64Image(dto);

                        var (status, reason) = await SaveProductAsync(dto);
                        result.Rows.Add(new ImportRowResult { RowNumber = rowNum, Name = dto.Name, Status = status, Reason = reason });

                        if (status == ImportRowStatus.Imported) result.Imported++;
                        else if (status == ImportRowStatus.Updated)  result.Updated++;
                        else result.Skipped++;
                    }
                }

                _logger.LogInformation("Unified JSON import completed. {Summary}", result.Summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing unified JSON import");
            }

            return result;
        }

        // ════════════════════════════════════════════════════════════════════
        //  LEGACY  (backward-compat)
        // ════════════════════════════════════════════════════════════════════
        public async Task ProcessCategoryImportAsync(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                using var workbook = new XLWorkbook(filePath);
                var ws = workbook.Worksheets.FirstOrDefault();
                if (ws != null) await ImportCategoriesFromWorksheet(ws);
            }
            catch (Exception ex) { _logger.LogError(ex, "Legacy category import error"); }
        }

        public async Task ProcessProductImportAsync(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                using var workbook = new XLWorkbook(filePath);
                var ws = workbook.Worksheets.FirstOrDefault();
                if (ws != null) await ImportProductsFromWorksheet(ws, new ImportResult());
            }
            catch (Exception ex) { _logger.LogError(ex, "Legacy product import error"); }
        }

        // ════════════════════════════════════════════════════════════════════
        //  INTERNAL HELPERS
        // ════════════════════════════════════════════════════════════════════

        private async Task ImportCategoriesFromWorksheet(IXLWorksheet ws)
        {
            var colIndex = BuildColumnIndex(ws);

            foreach (var row in ws.RangeUsed().RowsUsed().Skip(1))
            {
                var dto = new CategoryImportDto
                {
                    Name               = GetCell(row, colIndex, "Name"),
                    ParentCategoryName = GetCell(row, colIndex, "ParentCategoryName"),
                    Description        = GetCell(row, colIndex, "Description"),
                    DisplayOrder       = int.TryParse(GetCell(row, colIndex, "DisplayOrder"), out var ord) ? ord : 0
                };

                if (string.IsNullOrWhiteSpace(dto.Name)) continue;
                await SaveCategoryAsync(dto);
            }
        }

        private async Task ImportProductsFromWorksheet(IXLWorksheet ws, ImportResult result)
        {
            var colIndex = BuildColumnIndex(ws);

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

            foreach (var row in ws.RangeUsed().RowsUsed().Skip(1))
            {
                rowNum++;

                var dto = new ProductImportDto
                {
                    Sku             = GetCell(row, colIndex, "Sku"),
                    Name            = GetCell(row, colIndex, "Name"),
                    NameEn          = GetCell(row, colIndex, "NameEn"),
                    CategoryName    = GetCell(row, colIndex, "CategoryName"),
                    Description     = GetCell(row, colIndex, "Description"),
                    DescriptionEn   = GetCell(row, colIndex, "DescriptionEn"),
                    UnitOfMeasure   = GetCell(row, colIndex, "UnitOfMeasure") is { Length: > 0 } uom ? uom : "Unit",
                    Status          = GetCell(row, colIndex, "Status")  is { Length: > 0 } st  ? st  : "Active",
                    Brand           = GetCell(row, colIndex, "Brand"),
                    CountryOfOrigin = GetCell(row, colIndex, "CountryOfOrigin"),
                    ParentCategoryName = GetCell(row, colIndex, "ParentCategoryName"),
                    MicroCategoryName  = GetCell(row, colIndex, "MicroCategoryName"),
                    CategoryIcon    = GetCell(row, colIndex, "CategoryIcon")
                };

                if (string.IsNullOrWhiteSpace(dto.Name)) continue;

                result.TotalRows++;

                if (decimal.TryParse(GetCell(row, colIndex, "Price"), out var price)) dto.RetailPrice = price;
                if (int.TryParse(GetCell(row, colIndex, "Stock"), out var qty))  dto.StockQuantity = qty;
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

                // Fallback: image column URL or base64
                if (dto.ImageData == null)
                {
                    var imgStr = GetCell(row, colIndex, "Image");

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
                var parent = (await _unitOfWork.Categories.FindAsync(c => c.Name == dto.ParentCategoryName)).FirstOrDefault();
                if (parent != null) existing.ParentCategoryId = parent.Id;
            }

            if (existing.Id == default)
                await _unitOfWork.Categories.AddAsync(existing);
            else
                _unitOfWork.Categories.Update(existing);

            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>Returns (status, reason) for the product row.</summary>
        private async Task<(ImportRowStatus status, string reason)> SaveProductAsync(ProductImportDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return (ImportRowStatus.Skipped, "اسم المنتج فارغ");

            // Use sheet SKU if provided; otherwise auto-generate
            bool hasSku = !string.IsNullOrWhiteSpace(dto.Sku);
            if (!hasSku)
                dto.Sku = GenerateSku(dto.Name);
            else
            {
                // Ensure uniqueness when using a provided SKU (only for truly new products)
                // Collision check is deferred below after we know if it's an existing product
            }

            var validation = await _productValidator.ValidateAsync(dto);
            if (!validation.IsValid)
            {
                var errors = string.Join("، ", validation.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Product '{Name}' failed validation: {Errors}", dto.Name, errors);
                return (ImportRowStatus.Skipped, $"فشل التحقق: {errors}");
            }

            // ── Resolve/auto-create category hierarchy ──────────────────────
            if (string.IsNullOrWhiteSpace(dto.CategoryName))
            {
                _logger.LogWarning("Category name is missing for product '{Name}'", dto.Name);
                return (ImportRowStatus.Skipped, "اسم التصنيف الرئيسي مفقود");
            }

            // 1. Resolve/Create Level 1 (Main Category)
            var mainCategory = (await _unitOfWork.Categories.FindAsync(c => c.Name == dto.CategoryName)).FirstOrDefault();
            if (mainCategory == null)
            {
                mainCategory = new Category { Name = dto.CategoryName };
                await _unitOfWork.Categories.AddAsync(mainCategory);
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Auto-created main category '{Cat}'", dto.CategoryName);
            }

            Category leafCategory = mainCategory;

            // 2. Resolve/Create Level 2 (Sub-category)
            if (!string.IsNullOrWhiteSpace(dto.ParentCategoryName))
            {
                var subCategory = (await _unitOfWork.Categories.FindAsync(c => c.Name == dto.ParentCategoryName && c.ParentCategoryId == mainCategory.Id)).FirstOrDefault();
                if (subCategory == null)
                {
                    subCategory = new Category 
                    { 
                        Name = dto.ParentCategoryName,
                        ParentCategoryId = mainCategory.Id
                    };
                    await _unitOfWork.Categories.AddAsync(subCategory);
                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("Auto-created sub-category '{Cat}' under parent '{Parent}'", dto.ParentCategoryName, dto.CategoryName);
                }
                leafCategory = subCategory;

                // 3. Resolve/Create Level 3 (Micro-category)
                if (!string.IsNullOrWhiteSpace(dto.MicroCategoryName))
                {
                    var microCategory = (await _unitOfWork.Categories.FindAsync(c => c.Name == dto.MicroCategoryName && c.ParentCategoryId == subCategory.Id)).FirstOrDefault();
                    if (microCategory == null)
                    {
                        microCategory = new Category
                        {
                            Name = dto.MicroCategoryName,
                            ParentCategoryId = subCategory.Id
                        };
                        await _unitOfWork.Categories.AddAsync(microCategory);
                        await _unitOfWork.SaveChangesAsync();
                        _logger.LogInformation("Auto-created micro-category '{Cat}' under sub-category '{Parent}'", dto.MicroCategoryName, dto.ParentCategoryName);
                    }
                    leafCategory = microCategory;
                }
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
            product.Description         = dto.Description;
            product.CategoryId          = leafCategory.Id;
            product.RetailPrice         = dto.RetailPrice;
            product.UnitOfMeasure       = dto.UnitOfMeasure;
            product.StockQuantity       = dto.StockQuantity;
            product.Weight              = dto.Weight;
            product.Dimensions          = dto.Dimensions;
            product.Brand               = dto.Brand;
            product.CountryOfOrigin     = dto.CountryOfOrigin;
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

            return isNew
                ? (ImportRowStatus.Imported, string.Empty)
                : (ImportRowStatus.Updated,  string.Empty);
        }

        private async Task SaveProductImageAsync(Product product, ProductImportDto dto)
        {
            byte[]? imageBytes = null;
            string ext = "jpg";

            if (dto.ImageData is { Length: > 0 })
            {
                imageBytes = dto.ImageData;
                ext = dto.ImageExtension ?? "jpg";
            }
            else if (!string.IsNullOrWhiteSpace(dto.ImageBase64))
            {
                if (dto.ImageBase64!.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using var httpClient = new System.Net.Http.HttpClient();
                        imageBytes = await httpClient.GetByteArrayAsync(dto.ImageBase64);
                        var uri = new Uri(dto.ImageBase64);
                        ext = Path.GetExtension(uri.AbsolutePath).TrimStart('.') ?? "jpg";
                        if (string.IsNullOrWhiteSpace(ext)) ext = "jpg";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to download image from {Url}", dto.ImageBase64);
                    }
                }
                else
                {
                    (imageBytes, ext) = DecodeBase64Image(dto.ImageBase64!);
                }
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
            Directory.CreateDirectory(uploadsFolder);

            if (imageBytes != null)
            {
                var fileName  = $"{Guid.NewGuid()}.{ext}";
                var imagePath = Path.Combine(uploadsFolder, fileName);
                await File.WriteAllBytesAsync(imagePath, imageBytes);
                product.ImageUrl = $"/uploads/products/{fileName}";
            }

            if (dto.AdditionalImages != null && dto.AdditionalImages.Any())
            {
                int order = 1;
                foreach (var addImg in dto.AdditionalImages)
                {
                    var addFileName  = $"{Guid.NewGuid()}.{addImg.Ext}";
                    var addImagePath = Path.Combine(uploadsFolder, addFileName);
                    await File.WriteAllBytesAsync(addImagePath, addImg.Data);

                    product.Images.Add(new ProductImage
                    {
                        Url          = $"/uploads/products/{addFileName}",
                        DisplayOrder = order++
                    });
                }
            }

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();
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
            var headerRow = ws.RangeUsed().RowsUsed().FirstOrDefault();
            if (headerRow == null) return map;

            foreach (var cell in headerRow.CellsUsed())
            {
                var raw = cell.GetString().Trim();
                var key = _colMap.TryGetValue(raw, out var mapped) ? mapped : raw;
                if (!map.ContainsKey(key))
                    map[key] = cell.Address.ColumnNumber;
            }
            return map;
        }

        private static string GetCell(IXLRangeRow row, Dictionary<string, int> colIdx, string key)
        {
            if (colIdx.TryGetValue(key, out var colNum))
            {
                var cell = row.Cell(colNum);
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

        // ── JSON helpers ─────────────────────────────────────────────────────
        private static string GetJsonString(JsonElement el, params string[] keys)
        {
            foreach (var k in keys)
                if (el.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String)
                    return v.GetString() ?? string.Empty;
            return string.Empty;
        }

        private static int GetJsonInt(JsonElement el, params string[] keys)
        {
            foreach (var k in keys)
                if (el.TryGetProperty(k, out var v) && v.TryGetInt32(out var i))
                    return i;
            return 0;
        }

        private static decimal GetJsonDecimal(JsonElement el, params string[] keys)
        {
            foreach (var k in keys)
                if (el.TryGetProperty(k, out var v) && v.TryGetDecimal(out var d))
                    return d;
            return 0;
        }

        // ── Base64 image decoder ─────────────────────────────────────────────
        private static void ParseBase64Image(ProductImportDto dto)
        {
            try
            {
                var b64 = dto.ImageBase64!;
                var ext = "jpg";

                if (b64.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                {
                    var semi  = b64.IndexOf(';');
                    var slash = b64.IndexOf('/');
                    if (semi > slash) ext = b64[(slash + 1)..semi];
                    var comma = b64.IndexOf(',');
                    if (comma > -1) b64 = b64[(comma + 1)..];
                }

                dto.ImageData      = Convert.FromBase64String(b64);
                dto.ImageExtension = ext;
                dto.ImageMimeType  = $"image/{ext}";
            }
            catch { /* ignore malformed base64 */ }
        }

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
    }
}
