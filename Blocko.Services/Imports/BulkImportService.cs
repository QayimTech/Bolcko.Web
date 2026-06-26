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

namespace Blocko.Services.Imports
{
    public class BulkImportService : IBulkImportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<ProductImportDto> _productValidator;
        private readonly IValidator<CategoryImportDto> _categoryValidator;
        private readonly ILogger<BulkImportService> _logger;

        // ─── Arabic ↔ English column name map ───────────────────────────────
        private static readonly Dictionary<string, string> _colMap = new(StringComparer.OrdinalIgnoreCase)
        {
            // Categories
            ["الاسم"]              = "Name",
            ["اسم الفئة"]          = "Name",
            ["الفئة الأم"]         = "ParentCategoryName",
            ["اسم الفئة الأم"]     = "ParentCategoryName",
            ["الوصف"]              = "Description",
            ["ترتيب العرض"]        = "DisplayOrder",
            // Products
            ["اسم المنتج"]         = "Name",
            ["اسم_المنتج"]         = "Name",
            ["اسم الفئة للمنتج"]   = "CategoryName",
            ["اسم_الفئة"]          = "CategoryName",
            ["السعر"]              = "Price",
            ["الكمية"]             = "Stock",
            ["وحدة القياس"]        = "UnitOfMeasure",
            ["الوزن"]              = "Weight",
            ["الأبعاد"]            = "Dimensions",
            ["الحالة"]             = "Status",
            ["صورة"]               = "Image",
            ["الصورة"]             = "Image",
            ["التصنيف"]            = "CategoryName",
            ["البراند"]            = "Brand",
            ["بلد المنشأ"]         = "CountryOfOrigin",
            ["أيقونة الفئة"]       = "CategoryIcon",
            ["الايكون"]             = "CategoryIcon",
            // Also accept English as-is
            ["name"]               = "Name",
            ["parentcategoryname"] = "ParentCategoryName",
            ["description"]        = "Description",
            ["displayorder"]       = "DisplayOrder",
            ["categoryname"]       = "CategoryName",
            ["price"]              = "Price",
            ["retailprice"]        = "Price",
            ["stock"]              = "Stock",
            ["stockquantity"]      = "Stock",
            ["unitofmeasure"]      = "UnitOfMeasure",
            ["weight"]             = "Weight",
            ["dimensions"]         = "Dimensions",
            ["status"]             = "Status",
            ["image"]              = "Image",
            ["brand"]              = "Brand",
            ["countryoforigin"]    = "CountryOfOrigin",
        };

        public BulkImportService(
            IUnitOfWork unitOfWork,
            IValidator<ProductImportDto> productValidator,
            IValidator<CategoryImportDto> categoryValidator,
            ILogger<BulkImportService> logger)
        {
            _unitOfWork = unitOfWork;
            _productValidator = productValidator;
            _categoryValidator = categoryValidator;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════════
        //  UNIFIED EXCEL IMPORT  (one file → two sheets)
        // ════════════════════════════════════════════════════════════════════
        public async Task ProcessUnifiedExcelImportAsync(string filePath)
        {
            _logger.LogInformation("Starting unified Excel import from {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                _logger.LogError("File not found: {FilePath}", filePath);
                return;
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
                    await ImportProductsFromWorksheet(productSheet);
                }

                _logger.LogInformation("Unified Excel import completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing unified Excel import");
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  UNIFIED JSON IMPORT
        // ════════════════════════════════════════════════════════════════════
        public async Task ProcessUnifiedJsonImportAsync(string filePath)
        {
            _logger.LogInformation("Starting unified JSON import from {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                _logger.LogError("File not found: {FilePath}", filePath);
                return;
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
                    foreach (var prodEl in prodArray.EnumerateArray())
                    {
                        var dto = new ProductImportDto
                        {
                            Name             = GetJsonString(prodEl, "name", "الاسم", "اسم المنتج"),
                            CategoryName     = GetJsonString(prodEl, "categoryName", "اسم الفئة", "categoryname"),
                            Description      = GetJsonString(prodEl, "description", "الوصف"),
                            RetailPrice      = GetJsonDecimal(prodEl, "price", "السعر", "retailPrice"),
                            StockQuantity    = GetJsonInt(prodEl, "stock", "الكمية", "stockQuantity"),
                            UnitOfMeasure    = GetJsonString(prodEl, "unitOfMeasure", "وحدة القياس") ?? "Unit",
                            Status           = GetJsonString(prodEl, "status", "الحالة") ?? "Active",
                            Brand            = GetJsonString(prodEl, "brand", "البراند"),
                            CountryOfOrigin  = GetJsonString(prodEl, "countryOfOrigin", "بلد المنشأ"),
                            ImageBase64      = GetJsonString(prodEl, "imageBase64", "الصورة")
                        };

                        // Handle base64 image
                        if (!string.IsNullOrWhiteSpace(dto.ImageBase64))
                        {
                            ParseBase64Image(dto);
                        }

                        await SaveProductAsync(dto);
                    }
                }

                _logger.LogInformation("Unified JSON import completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing unified JSON import");
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  LEGACY  (backward-compat, kept for Hangfire jobs already enqueued)
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
                if (ws != null) await ImportProductsFromWorksheet(ws);
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

        private async Task ImportProductsFromWorksheet(IXLWorksheet ws)
        {
            var colIndex = BuildColumnIndex(ws);

            foreach (var row in ws.RangeUsed().RowsUsed().Skip(1))
            {
                var dto = new ProductImportDto
                {
                    Name          = GetCell(row, colIndex, "Name"),
                    CategoryName  = GetCell(row, colIndex, "CategoryName"),
                    Description   = GetCell(row, colIndex, "Description"),
                    UnitOfMeasure = GetCell(row, colIndex, "UnitOfMeasure") is { Length: > 0 } uom ? uom : "Unit",
                    Status        = GetCell(row, colIndex, "Status") is { Length: > 0 } st ? st : "Active",
                    Brand         = GetCell(row, colIndex, "Brand"),
                    CountryOfOrigin = GetCell(row, colIndex, "CountryOfOrigin"),
                    ParentCategoryName = GetCell(row, colIndex, "ParentCategoryName"),
                    CategoryIcon  = GetCell(row, colIndex, "CategoryIcon")
                };

                if (decimal.TryParse(GetCell(row, colIndex, "Price"), out var price)) dto.RetailPrice = price;
                if (int.TryParse(GetCell(row, colIndex, "Stock"), out var qty)) dto.StockQuantity = qty;
                if (decimal.TryParse(GetCell(row, colIndex, "Weight"), out var w)) dto.Weight = w;
                dto.Dimensions = GetCell(row, colIndex, "Dimensions");

                if (string.IsNullOrWhiteSpace(dto.Name)) continue;

                // Extract embedded pictures in this row
                var pics = ws.Pictures.Where(p => 
                    p.TopLeftCell.Address.RowNumber <= row.RowNumber() && 
                    p.BottomRightCell.Address.RowNumber >= row.RowNumber()).ToList();
                
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
                
                // Fallback: Check if the user placed an image URL, Base64, or =IMAGE() formula in the image column
                if (dto.ImageData == null)
                {
                    var imgStr = GetCell(row, colIndex, "Image");
                    
                    if (!string.IsNullOrWhiteSpace(imgStr))
                    {
                        // Check if it's an Excel formula like =IMAGE("url")
                        if (imgStr.StartsWith("=IMAGE(", StringComparison.OrdinalIgnoreCase) || imgStr.StartsWith("IMAGE(", StringComparison.OrdinalIgnoreCase))
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(imgStr, "IMAGE\\([\"']?(http.*?)[\"']?\\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                imgStr = match.Groups[1].Value;
                            }
                        }

                        if (imgStr.StartsWith("http", StringComparison.OrdinalIgnoreCase) || imgStr.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
                        {
                            dto.ImageBase64 = imgStr; // We will handle HTTP URLs in SaveProductImageAsync
                        }
                    }
                }

                await SaveProductAsync(dto);
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
            {
                existing = new Category { Name = dto.Name! };
            }

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

        private async Task SaveProductAsync(ProductImportDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return;

            // Auto-generate SKU if not provided
            if (string.IsNullOrWhiteSpace(dto.Sku))
                dto.Sku = GenerateSku(dto.Name);

            // Ensure SKU is unique
            var existing = (await _unitOfWork.Products.FindAsync(p => p.Sku == dto.Sku)).FirstOrDefault();
            if (existing != null)
                dto.Sku = GenerateSku(dto.Name); // re-generate on collision

            var validation = await _productValidator.ValidateAsync(dto);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Product '{Name}' failed validation: {Errors}",
                    dto.Name, string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)));
                return;
            }

            var category = (await _unitOfWork.Categories.FindAsync(c => c.Name == dto.CategoryName)).FirstOrDefault();
            if (category == null)
            {
                if (string.IsNullOrWhiteSpace(dto.CategoryName))
                {
                    _logger.LogWarning("Category name is missing for product '{Name}'", dto.Name);
                    return;
                }

                // Auto-create category if it does not exist
                category = new Category { Name = dto.CategoryName };
                await _unitOfWork.Categories.AddAsync(category);
                await _unitOfWork.SaveChangesAsync(); // save immediately to get Id
                _logger.LogInformation("Auto-created category '{Cat}' for product '{Name}'", dto.CategoryName, dto.Name);
            }

            bool categoryUpdated = false;

            if (!string.IsNullOrWhiteSpace(dto.CategoryIcon) && category.ImageUrl != dto.CategoryIcon)
            {
                category.ImageUrl = dto.CategoryIcon;
                categoryUpdated = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.ParentCategoryName))
            {
                var parent = (await _unitOfWork.Categories.FindAsync(c => c.Name == dto.ParentCategoryName)).FirstOrDefault();
                if (parent == null)
                {
                    parent = new Category { Name = dto.ParentCategoryName };
                    await _unitOfWork.Categories.AddAsync(parent);
                    await _unitOfWork.SaveChangesAsync();
                }
                
                if (category.ParentCategoryId != parent.Id)
                {
                    category.ParentCategoryId = parent.Id;
                    categoryUpdated = true;
                }
            }

            if (categoryUpdated)
            {
                _unitOfWork.Categories.Update(category);
                await _unitOfWork.SaveChangesAsync();
            }

            Enum.TryParse<Bolcko.Domain.Enums.ProductStatus>(dto.Status, true, out var status);

            // Find by name+category for upsert (since SKU is auto-generated each run for new items)
            var product = (await _unitOfWork.Products.FindAsync(
                p => p.Name == dto.Name && p.CategoryId == category.Id)).FirstOrDefault();

            bool isNew = product == null;
            if (isNew)
                product = new Product { Sku = dto.Sku, CreatedAt = DateTime.UtcNow };

            product!.Name               = dto.Name;
            product.Description         = dto.Description;
            product.CategoryId          = category.Id;
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

            // Save image
            await SaveProductImageAsync(product, dto);
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

            // Save primary image
            if (imageBytes != null)
            {
                var fileName  = $"{Guid.NewGuid()}.{ext}";
                var imagePath = Path.Combine(uploadsFolder, fileName);
                await File.WriteAllBytesAsync(imagePath, imageBytes);
                product.ImageUrl = $"/uploads/products/{fileName}";
            }

            // Save additional images
            if (dto.AdditionalImages != null && dto.AdditionalImages.Any())
            {
                int order = 1;
                foreach (var addImg in dto.AdditionalImages)
                {
                    var addFileName = $"{Guid.NewGuid()}.{addImg.Ext}";
                    var addImagePath = Path.Combine(uploadsFolder, addFileName);
                    await File.WriteAllBytesAsync(addImagePath, addImg.Data);
                    
                    product.Images.Add(new ProductImage
                    {
                        Url = $"/uploads/products/{addFileName}",
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
            // Transliterate Latin chars from name; use "PRD" for purely Arabic names
            var ascii = Regex.Replace(name ?? "", @"[^A-Za-z0-9]", "").ToUpperInvariant();
            var prefix = ascii.Length >= 3 ? ascii[..Math.Min(6, ascii.Length)] : "PRD";
            var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            return $"{prefix}-{suffix}";
        }

        // ── Column index builder (reads header row, maps Arabic/English) ─────
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
                var val = cell.GetString().Trim();
                return val;
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
