using Blocko.Services.Interfaces.SEO;
using ProductEntity = Bolcko.Domain.Entities.Product.Product;
using Bolcko.Domain.Entities.Product.DTOs;
using Bolcko.Domain.Entities.SEO;
using Bolcko.Domain.Enums;
using Bolcko.Domain.Interfaces;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Blocko.Services.Implementations.SEO
{
    public class ProductSeoService : IProductSeoService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ProductSeoService> _logger;

        public ProductSeoService(
            IUnitOfWork unitOfWork,
            IMemoryCache cache,
            ILogger<ProductSeoService> logger)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
            _logger = logger;
        }

        public async Task<byte[]> ExportProductsSeoToExcelAsync(ProductSeoFilterParamsDto filter)
        {
            var query = _unitOfWork.Products.GetAllAsQueryable().Include(p => p.Category).AsNoTracking();

            if (filter.SelectedProductIds != null && filter.SelectedProductIds.Any())
            {
                query = query.Where(p => filter.SelectedProductIds.Contains(p.Id));
            }
            else
            {
                if (filter.CategoryId.HasValue && filter.CategoryId.Value > 0)
                {
                    query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
                }

                if (!string.IsNullOrWhiteSpace(filter.Brand))
                {
                    query = query.Where(p => p.Brand == filter.Brand.Trim());
                }

                if (filter.SeoStatus.HasValue)
                {
                    query = query.Where(p => p.SeoStatus == filter.SeoStatus.Value);
                }

                if (filter.OnlyMissingDescription)
                {
                    query = query.Where(p => string.IsNullOrWhiteSpace(p.Description));
                }

                if (!string.IsNullOrWhiteSpace(filter.Search))
                {
                    var term = filter.Search.Trim().ToLower();
                    query = query.Where(p => p.Name.ToLower().Contains(term) ||
                                             (p.Sku != null && p.Sku.ToLower().Contains(term)));
                }
            }

            var products = await query.OrderByDescending(p => p.Id).ToListAsync();

            var seoPages = (await _unitOfWork.SEO.FindAsync(s => s.PageName.StartsWith("/Product/Index/")))
                .ToDictionary(s => s.PageName, s => s);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Product_SEO_Data");

            // Header Styling
            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "SKU (رمز المنتج)";
            worksheet.Cell(1, 3).Value = "اسم المنتج (Arabic Name)";
            worksheet.Cell(1, 4).Value = "التصنيف (Category)";
            worksheet.Cell(1, 5).Value = "الماركة (Brand)";
            worksheet.Cell(1, 6).Value = "الوصف التفصيلي (Full Description)";
            worksheet.Cell(1, 7).Value = "عنوان الصفحة SEO (Meta Title)";
            worksheet.Cell(1, 8).Value = "وصف SEO (Meta Description)";
            worksheet.Cell(1, 9).Value = "كلمات مفتاحية (Meta Keywords)";
            worksheet.Cell(1, 10).Value = "حالة الـ SEO (Status: PendingSeo / InReview / SeoApproved)";

            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
            headerRow.Style.Font.SetFontColor(XLColor.White);
            headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int row = 2;
            foreach (var p in products)
            {
                var pageKey = $"/Product/Index/{p.Id}";
                seoPages.TryGetValue(pageKey, out var seo);

                worksheet.Cell(row, 1).Value = p.Id;
                worksheet.Cell(row, 2).Value = p.Sku ?? "";
                worksheet.Cell(row, 3).Value = p.Name;
                worksheet.Cell(row, 4).Value = p.Category?.Name ?? "";
                worksheet.Cell(row, 5).Value = p.Brand ?? "";
                worksheet.Cell(row, 6).Value = p.Description ?? "";
                worksheet.Cell(row, 7).Value = seo?.PageTitle ?? "";
                worksheet.Cell(row, 8).Value = seo?.MetaDescription ?? "";
                worksheet.Cell(row, 9).Value = seo?.MetaKeywords ?? "";
                worksheet.Cell(row, 10).Value = p.SeoStatus.ToString();

                row++;
            }

            worksheet.Columns().AdjustToContents(1, 100);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportSingleProductSeoToExcelAsync(int productId)
        {
            return await ExportProductsSeoToExcelAsync(new ProductSeoFilterParamsDto
            {
                SelectedProductIds = new List<int> { productId }
            });
        }

        public async Task<BulkSeoJobResultDto> ImportProductSeoFromStreamAsync(Stream fileStream, bool autoApproveSeo = true)
        {
            var result = new BulkSeoJobResultDto
            {
                JobId = Guid.NewGuid().ToString("N"),
                StartedAt = DateTime.UtcNow
            };

            try
            {
                using var workbook = new XLWorkbook(fileStream);
                var worksheet = workbook.Worksheets.First();
                var rows = worksheet.RowsUsed().Skip(1);

                result.TotalRecords = rows.Count();

                foreach (var r in rows)
                {
                    try
                    {
                        int id = 0;
                        var idStr = r.Cell(1).GetString().Trim();
                        var skuStr = r.Cell(2).GetString().Trim();

                        ProductEntity? product = null;
                        if (int.TryParse(idStr, out id) && id > 0)
                        {
                            product = await _unitOfWork.Products.GetByIdAsync(id);
                        }
                        else if (!string.IsNullOrWhiteSpace(skuStr))
                        {
                            product = (await _unitOfWork.Products.FindAsync(p => p.Sku == skuStr)).FirstOrDefault();
                        }

                        if (product == null)
                        {
                            result.SkippedCount++;
                            result.Warnings.Add($"الصف {r.RowNumber()}: لم يتم العثور على المنتج برقم ID ({idStr}) أو SKU ({skuStr}).");
                            continue;
                        }

                        var desc = r.Cell(6).GetString().Trim();
                        var metaTitle = r.Cell(7).GetString().Trim();
                        var metaDesc = r.Cell(8).GetString().Trim();
                        var metaKeywords = r.Cell(9).GetString().Trim();
                        var statusStr = r.Cell(10).GetString().Trim();

                        if (!string.IsNullOrWhiteSpace(desc))
                        {
                            product.Description = desc;
                        }

                        if (Enum.TryParse<SeoStatus>(statusStr, true, out var parsedStatus))
                        {
                            product.SeoStatus = parsedStatus;
                        }
                        else if (autoApproveSeo && !string.IsNullOrWhiteSpace(desc))
                        {
                            product.SeoStatus = SeoStatus.SeoApproved;
                        }

                        _unitOfWork.Products.Update(product);

                        string pageKey = $"/Product/Index/{product.Id}";
                        var existingSeo = (await _unitOfWork.SEO.FindAsync(s => s.PageName == pageKey)).FirstOrDefault();

                        if (existingSeo == null)
                        {
                            existingSeo = new SEOMetadata
                            {
                                PageName = pageKey,
                                PageTitle = !string.IsNullOrWhiteSpace(metaTitle) ? metaTitle : product.Name,
                                MetaDescription = !string.IsNullOrWhiteSpace(metaDesc) ? metaDesc : product.Description,
                                MetaKeywords = metaKeywords,
                                PageUrl = $"/Shop/Product/Details/{product.Id}",
                                LastUpdated = DateTime.UtcNow
                            };
                            await _unitOfWork.SEO.AddAsync(existingSeo);
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(metaTitle)) existingSeo.PageTitle = metaTitle;
                            if (!string.IsNullOrWhiteSpace(metaDesc)) existingSeo.MetaDescription = metaDesc;
                            if (!string.IsNullOrWhiteSpace(metaKeywords)) existingSeo.MetaKeywords = metaKeywords;
                            existingSeo.LastUpdated = DateTime.UtcNow;
                            _unitOfWork.SEO.Update(existingSeo);
                        }

                        result.UpdatedCount++;
                    }
                    catch (Exception ex)
                    {
                        result.SkippedCount++;
                        result.Errors.Add($"الصف {r.RowNumber()}: خطأ أثناء المعالجة - {ex.Message}");
                    }
                }

                await _unitOfWork.CompleteAsync();
                result.IsCompleted = true;
                result.CompletedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"فشل فتح ملف Excel: {ex.Message}");
            }

            return result;
        }

        public async Task ProcessSeoBulkImportHangfireJobAsync(string jobId, string tempFilePath, bool autoApproveSeo)
        {
            var result = new BulkSeoJobResultDto
            {
                JobId = jobId,
                StartedAt = DateTime.UtcNow
            };
            _cache.Set(jobId, result, TimeSpan.FromHours(2));

            try
            {
                if (!File.Exists(tempFilePath))
                {
                    result.Errors.Add("ملف الاستيراد المؤقت غير موجود.");
                    result.IsCompleted = true;
                    _cache.Set(jobId, result, TimeSpan.FromHours(2));
                    return;
                }

                using var stream = File.OpenRead(tempFilePath);
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.First();
                var rows = worksheet.RowsUsed().Skip(1).ToList();

                result.TotalRecords = rows.Count;
                _cache.Set(jobId, result, TimeSpan.FromHours(2));

                int batchSize = 150;
                for (int i = 0; i < rows.Count; i += batchSize)
                {
                    var batchRows = rows.Skip(i).Take(batchSize);

                    foreach (var r in batchRows)
                    {
                        try
                        {
                            int id = 0;
                            var idStr = r.Cell(1).GetString().Trim();
                            var skuStr = r.Cell(2).GetString().Trim();

                            ProductEntity? product = null;
                            if (int.TryParse(idStr, out id) && id > 0)
                            {
                                product = await _unitOfWork.Products.GetByIdAsync(id);
                            }
                            else if (!string.IsNullOrWhiteSpace(skuStr))
                            {
                                product = (await _unitOfWork.Products.FindAsync(p => p.Sku == skuStr)).FirstOrDefault();
                            }

                            if (product == null)
                            {
                                result.SkippedCount++;
                                continue;
                            }

                            var desc = r.Cell(6).GetString().Trim();
                            var metaTitle = r.Cell(7).GetString().Trim();
                            var metaDesc = r.Cell(8).GetString().Trim();
                            var metaKeywords = r.Cell(9).GetString().Trim();
                            var statusStr = r.Cell(10).GetString().Trim();

                            if (!string.IsNullOrWhiteSpace(desc))
                            {
                                product.Description = desc;
                            }

                            if (Enum.TryParse<SeoStatus>(statusStr, true, out var parsedStatus))
                            {
                                product.SeoStatus = parsedStatus;
                            }
                            else if (autoApproveSeo && !string.IsNullOrWhiteSpace(desc))
                            {
                                product.SeoStatus = SeoStatus.SeoApproved;
                            }

                            _unitOfWork.Products.Update(product);

                            string pageKey = $"/Product/Index/{product.Id}";
                            var existingSeo = (await _unitOfWork.SEO.FindAsync(s => s.PageName == pageKey)).FirstOrDefault();

                            if (existingSeo == null)
                            {
                                existingSeo = new SEOMetadata
                                {
                                    PageName = pageKey,
                                    PageTitle = !string.IsNullOrWhiteSpace(metaTitle) ? metaTitle : product.Name,
                                    MetaDescription = !string.IsNullOrWhiteSpace(metaDesc) ? metaDesc : product.Description,
                                    MetaKeywords = metaKeywords,
                                    PageUrl = $"/Shop/Product/Details/{product.Id}",
                                    LastUpdated = DateTime.UtcNow
                                };
                                await _unitOfWork.SEO.AddAsync(existingSeo);
                            }
                            else
                            {
                                if (!string.IsNullOrWhiteSpace(metaTitle)) existingSeo.PageTitle = metaTitle;
                                if (!string.IsNullOrWhiteSpace(metaDesc)) existingSeo.MetaDescription = metaDesc;
                                if (!string.IsNullOrWhiteSpace(metaKeywords)) existingSeo.MetaKeywords = metaKeywords;
                                existingSeo.LastUpdated = DateTime.UtcNow;
                                _unitOfWork.SEO.Update(existingSeo);
                            }

                            result.UpdatedCount++;
                        }
                        catch (Exception ex)
                        {
                            result.SkippedCount++;
                            _logger.LogWarning(ex, "Error processing SEO row");
                        }
                    }

                    await _unitOfWork.CompleteAsync();
                    _cache.Set(jobId, result, TimeSpan.FromHours(2));
                }

                result.IsCompleted = true;
                result.CompletedAt = DateTime.UtcNow;
                _cache.Set(jobId, result, TimeSpan.FromHours(2));

                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hangfire SEO Job Failed: {JobId}", jobId);
                result.Errors.Add($"خطأ استثنائي أثناء معالجة الوظيفة: {ex.Message}");
                result.IsCompleted = true;
                _cache.Set(jobId, result, TimeSpan.FromHours(2));
            }
        }

        public async Task<BulkSeoJobResultDto?> GetSeoJobResultAsync(string jobId)
        {
            _cache.TryGetValue<BulkSeoJobResultDto>(jobId, out var result);
            return await Task.FromResult(result);
        }

        public async Task<bool> UpdateSingleProductSeoAsync(ProductSeoImportDto dto)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(dto.Id);
            if (product == null) return false;

            if (dto.Description != null) product.Description = dto.Description;
            if (dto.SeoStatus.HasValue) product.SeoStatus = dto.SeoStatus.Value;

            _unitOfWork.Products.Update(product);

            string pageKey = $"/Product/Index/{product.Id}";
            var existingSeo = (await _unitOfWork.SEO.FindAsync(s => s.PageName == pageKey)).FirstOrDefault();

            if (existingSeo == null)
            {
                existingSeo = new SEOMetadata
                {
                    PageName = pageKey,
                    PageTitle = !string.IsNullOrWhiteSpace(dto.MetaTitle) ? dto.MetaTitle : product.Name,
                    MetaDescription = !string.IsNullOrWhiteSpace(dto.MetaDescription) ? dto.MetaDescription : product.Description,
                    MetaKeywords = dto.MetaKeywords,
                    PageUrl = $"/Shop/Product/Details/{product.Id}",
                    LastUpdated = DateTime.UtcNow
                };
                await _unitOfWork.SEO.AddAsync(existingSeo);
            }
            else
            {
                if (dto.MetaTitle != null) existingSeo.PageTitle = dto.MetaTitle;
                if (dto.MetaDescription != null) existingSeo.MetaDescription = dto.MetaDescription;
                if (dto.MetaKeywords != null) existingSeo.MetaKeywords = dto.MetaKeywords;
                existingSeo.LastUpdated = DateTime.UtcNow;
                _unitOfWork.SEO.Update(existingSeo);
            }

            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<(int total, int approved, int pending, int missingDesc)> GetSeoMetricsAsync()
        {
            var query = _unitOfWork.Products.GetAllAsQueryable().AsNoTracking();
            var total = await query.CountAsync();
            var approved = await query.CountAsync(p => p.SeoStatus == Bolcko.Domain.Enums.SeoStatus.SeoApproved);
            var pending = await query.CountAsync(p => p.SeoStatus == Bolcko.Domain.Enums.SeoStatus.PendingSeo || p.SeoStatus == Bolcko.Domain.Enums.SeoStatus.InReview);
            var missingDesc = await query.CountAsync(p => string.IsNullOrWhiteSpace(p.Description));

            return (total, approved, pending, missingDesc);
        }
    }
}

