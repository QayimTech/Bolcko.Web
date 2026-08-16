using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Blocko.Services.Interfaces.Delivery;
using Bolcko.Domain.Entities.Delivery;
using Bolcko.Domain.Entities.Order;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Services.Implementations.Delivery
{
    public class DeliveryApiService : IDeliveryApiService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly HttpClient _httpClient;

        public DeliveryApiService(IUnitOfWork unitOfWork, HttpClient httpClient)
        {
            _unitOfWork = unitOfWork;
            _httpClient = httpClient;
        }

        public async Task<DeliveryProviderConfig?> GetActiveConfigAsync()
        {
            return await _unitOfWork.DeliveryProviderConfigs.GetAllAsQueryable()
                .FirstOrDefaultAsync(c => c.IsActive);
        }

        public async Task<bool> SaveConfigAsync(DeliveryProviderConfig config)
        {
            var existing = await _unitOfWork.DeliveryProviderConfigs.GetAllAsQueryable()
                .FirstOrDefaultAsync(c => c.ProviderKey == config.ProviderKey || c.Id == config.Id);

            if (existing != null)
            {
                existing.ProviderName = config.ProviderName;
                existing.ProviderKey = config.ProviderKey;
                existing.BaseUrl = config.BaseUrl;
                existing.CompanyId = config.CompanyId;
                existing.ApiEmail = config.ApiEmail;
                existing.ApiPassword = config.ApiPassword;
                existing.WebhookSecret = config.WebhookSecret;
                existing.OutboundWebhookUrl = config.OutboundWebhookUrl;
                existing.CustomHeadersJson = config.CustomHeadersJson;
                existing.CustomPayloadMappingJson = config.CustomPayloadMappingJson;
                existing.IsActive = config.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.DeliveryProviderConfigs.Update(existing);
            }
            else
            {
                config.CreatedAt = DateTime.UtcNow;
                await _unitOfWork.DeliveryProviderConfigs.AddAsync(config);
            }

            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<CreateShipmentResultDto> CreateShipmentAsync(Bolcko.Domain.Entities.Order.Order order, string destinationCityId, string destinationRegionId, string destinationVillageId, string? notes = null)
        {
            var config = await GetActiveConfigAsync();
            if (config == null || string.IsNullOrWhiteSpace(config.BaseUrl))
            {
                return new CreateShipmentResultDto
                {
                    Success = false,
                    Message = "إعدادات شركات التوصيل API غير مفعّلة في لوحة التحكم."
                };
            }

            var mapsUrl = (order.ShippingAddress?.Latitude.HasValue == true && order.ShippingAddress?.Longitude.HasValue == true) 
                ? $"https://maps.google.com/?q={order.ShippingAddress.Latitude},{order.ShippingAddress.Longitude}" 
                : string.Empty;

            var itemsCount = order.Items != null && order.Items.Any() ? order.Items.Sum(i => i.Quantity) : 1;

            var itemsSummary = order.Items != null && order.Items.Any()
                ? string.Join(" • ", order.Items.Select(i => {
                    var pName = i.Product?.Name ?? $"منتج #{i.ProductId}";
                    var variantStr = i.ProductVariant != null ? string.Join(" - ", new[] { i.ProductVariant.Size, i.ProductVariant.Color, i.ProductVariant.PackagingUnit }.Where(s => !string.IsNullOrEmpty(s))) : "";
                    var variantDetails = !string.IsNullOrEmpty(variantStr) ? $" ({variantStr})" : "";
                    var webSku = i.ProductVariant?.Sku ?? i.Product?.Sku ?? "N/A";
                    return $"[الكمية: {i.Quantity}] {pName}{variantDetails} - SKU: {webSku}";
                }))
                : "مواد ومستلزمات بناء";

            var itemsNotesFormatted = order.Items != null && order.Items.Any()
                ? string.Join(" \\n ", order.Items.Select((i, idx) => {
                    var pName = i.Product?.Name ?? $"منتج #{i.ProductId}";
                    var variantStr = i.ProductVariant != null ? string.Join(" - ", new[] { i.ProductVariant.Size, i.ProductVariant.Color, i.ProductVariant.PackagingUnit }.Where(s => !string.IsNullOrEmpty(s))) : "";
                    var variantDetails = !string.IsNullOrEmpty(variantStr) ? $" ({variantStr})" : "";
                    var webSku = i.ProductVariant?.Sku ?? i.Product?.Sku ?? "N/A";
                    var imgUrl = !string.IsNullOrEmpty(i.ProductVariant?.ImageUrl) ? i.ProductVariant.ImageUrl : (!string.IsNullOrEmpty(i.Product?.ImageUrl) ? i.Product.ImageUrl : "");
                    var relativePath = !string.IsNullOrEmpty(imgUrl) ? (imgUrl.StartsWith("/") ? imgUrl : $"/{imgUrl}") : "";
                    var absoluteImgUrl = !string.IsNullOrEmpty(relativePath) && !relativePath.StartsWith("http", StringComparison.OrdinalIgnoreCase) 
                        ? $"https://bolcko.com{relativePath}" 
                        : relativePath;
                    var imgText = !string.IsNullOrEmpty(absoluteImgUrl) ? $" | 📷 صورة: {absoluteImgUrl}" : "";
                    return $"[{idx + 1}] {pName}{variantDetails} - 📦 الكمية: {i.Quantity} - 🏷️ SKU: {webSku}{imgText}";
                }))
                : "طلب مواد بناء";

            var requestUrl = config.BaseUrl.Contains("/ship/request") 
                ? config.BaseUrl.Trim() 
                : $"{config.BaseUrl.TrimEnd('/')}/ship/request/by-email";

            string jsonPayloadString;

            var addressParts = new List<string>();
            if (order.ShippingAddress != null)
            {
                if (!string.IsNullOrWhiteSpace(order.ShippingAddress.City)) addressParts.Add($"مدينة {order.ShippingAddress.City}");
                if (!string.IsNullOrWhiteSpace(order.ShippingAddress.StateProvince)) addressParts.Add($"منطقة {order.ShippingAddress.StateProvince}");
                if (!string.IsNullOrWhiteSpace(order.ShippingAddress.AddressLine1)) addressParts.Add(order.ShippingAddress.AddressLine1);
                if (!string.IsNullOrWhiteSpace(order.ShippingAddress.AddressLine2)) addressParts.Add(order.ShippingAddress.AddressLine2);
            }
            var fullAddressLine = addressParts.Any() ? string.Join(" - ", addressParts) : "عنوان التسليم الخاص بالعميل";

            var targetCityId = long.TryParse(destinationCityId, out var parsedCityId) && parsedCityId > 1 ? parsedCityId : 0;
            var targetRegionId = long.TryParse(destinationRegionId, out var parsedRegionId) && parsedRegionId > 1 ? parsedRegionId : 0;
            var targetVillageId = long.TryParse(destinationVillageId, out var parsedVillageId) && parsedVillageId > 1 ? parsedVillageId : 0;

            if (targetCityId == 0 || targetVillageId == 0)
            {
                var searchKeys = new[] { 
                    order.ShippingAddress?.City,
                    order.ShippingAddress?.StateProvince, 
                    order.ShippingAddress?.AddressLine1 
                }.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

                var providerKey = config.ProviderKey.ToLowerInvariant();

                // 1. Primary DB Mapping Table Lookup (Provider Agnostic)
                try
                {
                    foreach (var searchKey in searchKeys)
                    {
                        if (string.IsNullOrWhiteSpace(searchKey)) continue;
                        var cleanKey = searchKey.Replace("مدينة", "").Replace("منطقة", "").Replace("محافظة", "").Trim();

                        var dbMapping = await _unitOfWork.DeliveryProviderLocationMappings.GetAllAsQueryable()
                            .FirstOrDefaultAsync(m => m.ProviderKey.ToLower() == providerKey &&
                                (m.SearchName.Contains(cleanKey) || m.NormalizedSearchName.Contains(cleanKey) || cleanKey.Contains(m.SearchName)));

                        if (dbMapping != null)
                        {
                            targetCityId = dbMapping.ExternalCityId;
                            targetRegionId = dbMapping.ExternalRegionId;
                            targetVillageId = dbMapping.ExternalVillageId;
                            break;
                        }
                    }
                }
                catch { /* DB Table Query Protection */ }

                // 2. Dynamic API On-The-Fly Fetch (Fallback if DB not populated)
                if (targetCityId == 0 || targetVillageId == 0)
                {
                    foreach (var searchKey in searchKeys)
                    {
                        if (string.IsNullOrWhiteSpace(searchKey)) continue;

                        var cleanKey = searchKey.Replace("مدينة", "").Replace("منطقة", "").Replace("محافظة", "").Trim();
                        var firstWord = cleanKey.Split(' ').FirstOrDefault(w => w.Length >= 2) ?? cleanKey;
                        var villages = await GetVillagesAsync(firstWord);

                        if (villages != null && villages.Any())
                        {
                            var matched = villages.FirstOrDefault(v => 
                                (!string.IsNullOrEmpty(v.ArabicName) && (v.ArabicName.Contains(firstWord) || firstWord.Contains(v.ArabicName))) ||
                                (!string.IsNullOrEmpty(v.CityName) && (v.CityName.Contains(firstWord, StringComparison.OrdinalIgnoreCase) || firstWord.Contains(v.CityName, StringComparison.OrdinalIgnoreCase))) ||
                                (!string.IsNullOrEmpty(v.Name) && (v.Name.Contains(firstWord, StringComparison.OrdinalIgnoreCase) || firstWord.Contains(v.Name, StringComparison.OrdinalIgnoreCase))));
                            
                            if (matched != null)
                            {
                                targetCityId = matched.CityId;
                                targetRegionId = matched.RegionId;
                                targetVillageId = matched.Id;
                                break;
                            }
                        }
                    }
                }
            }

            // Pure dynamic values without hardcoding fallback defaults:
            // If targetVillageId wasn't found in DB or API, leave 0 so external provider doesn't show wrong village (e.g. Om Alfahem)

            if (!string.IsNullOrWhiteSpace(config.CustomPayloadMappingJson) && config.CustomPayloadMappingJson.Contains("{"))
            {
                var template = config.CustomPayloadMappingJson;
                var receiverNameVal = order.User != null ? $"{order.User.FirstName} {order.User.LastName}".Trim() : (order.ShippingAddress?.AddressLine1 ?? "عميل بلوكو");
                var receiverPhoneVal = order.User?.PhoneNumber ?? "0590000000";
                var addressVal = fullAddressLine;

                var pwdVal = !string.IsNullOrWhiteSpace(config.ApiPassword) ? config.ApiPassword : "Ham@123ham";

                jsonPayloadString = template
                    .Replace("{ApiEmail}", config.ApiEmail)
                    .Replace("{ApiPassword}", pwdVal)
                    .Replace("{OrderTotal}", ((double)order.TotalAmount).ToString("F2", System.Globalization.CultureInfo.InvariantCulture))
                    .Replace("\"{OrderTotal}\"", ((double)order.TotalAmount).ToString("F2", System.Globalization.CultureInfo.InvariantCulture))
                    .Replace("{OrderNumber}", order.OrderNumber)
                    .Replace("{CustomerName}", receiverNameVal)
                    .Replace("{CustomerPhone}", receiverPhoneVal)
                    .Replace("{OrderQuantity}", itemsCount.ToString())
                    .Replace("\"{OrderQuantity}\"", itemsCount.ToString())
                    .Replace("{OrderContents}", itemsSummary)
                    .Replace("{OrderItemsNotes}", itemsNotesFormatted)
                    .Replace("{GoogleMapsUrl}", mapsUrl)
                    .Replace("{CustomerAddress}", addressVal)
                    .Replace("{CityId}", targetCityId.ToString())
                    .Replace("\"{CityId}\"", targetCityId.ToString())
                    .Replace("{RegionId}", targetRegionId.ToString())
                    .Replace("\"{RegionId}\"", targetRegionId.ToString())
                    .Replace("{VillageId}", targetVillageId.ToString())
                    .Replace("\"{VillageId}\"", targetVillageId.ToString())
                    .Replace("{OutboundWebhookUrl}", config.OutboundWebhookUrl ?? string.Empty);
            }
            else
            {
                var payloadObj = new
                {
                    email = config.ApiEmail,
                    password = config.ApiPassword,
                    pkgUnitType = "METRIC",
                    pkg = new
                    {
                        cod = (double)order.TotalAmount,
                        invoiceNumber = order.OrderNumber,
                        receiverName = order.User != null ? $"{order.User.FirstName} {order.User.LastName}".Trim() : (order.ShippingAddress?.AddressLine1 ?? "عميل بلوكو"),
                        receiverPhone = order.User?.PhoneNumber ?? "0590000000",
                        serviceType = "STANDARD",
                        shipmentType = "COD",
                        paymentType = "CASH",
                        quantity = itemsCount,
                        contents = itemsSummary,
                        locationUrl = mapsUrl,
                        notes = string.IsNullOrWhiteSpace(notes) ? $"طلب رقم {order.OrderNumber} - {fullAddressLine} {mapsUrl}" : $"{notes} - {fullAddressLine} {mapsUrl}",
                        webhookUrl = config.OutboundWebhookUrl ?? string.Empty
                    },
                    destinationAddress = new
                    {
                        addressLine1 = fullAddressLine,
                        cityId = targetCityId,
                        regionId = targetRegionId,
                        villageId = targetVillageId,
                        latitude = order.ShippingAddress?.Latitude ?? 31.9038,
                        longitude = order.ShippingAddress?.Longitude ?? 35.2058
                    },
                    originAddress = new
                    {
                        addressLine1 = "عمان - حي معصوم",
                        cityId = 395,
                        regionId = 33,
                        villageId = 18076
                    }
                };

                jsonPayloadString = JsonSerializer.Serialize(payloadObj);
            }

            var jsonContent = new StringContent(jsonPayloadString, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = jsonContent
            };
            if (!string.IsNullOrWhiteSpace(config.CompanyId))
            {
                request.Headers.TryAddWithoutValidation("company-id", config.CompanyId);
            }

            if (!string.IsNullOrWhiteSpace(config.CustomHeadersJson))
            {
                try
                {
                    using var hDoc = JsonDocument.Parse(config.CustomHeadersJson);
                    if (hDoc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in hDoc.RootElement.EnumerateObject())
                        {
                            request.Headers.Remove(prop.Name);
                            request.Headers.TryAddWithoutValidation(prop.Name, prop.Value.ToString());
                        }
                    }
                }
                catch { }
            }

            try
            {
                // Write detailed log for debugging
                var logFolder = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                if (!Directory.Exists(logFolder)) Directory.CreateDirectory(logFolder);
                var logPath = Path.Combine(logFolder, "delivery_api.log");
                var logContent = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC]\nURL: {requestUrl}\nCompanyId Header: {config.CompanyId}\nCustom Headers: {config.CustomHeadersJson}\nSENT PAYLOAD:\n{jsonPayloadString}\n-----------------------------------\n";
                File.AppendAllText(logPath, logContent);

                var response = await _httpClient.SendAsync(request);
                var responseString = await response.Content.ReadAsStringAsync();

                var responseLog = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] RESPONSE Status: {response.StatusCode}\nBODY: {responseString}\n===================================\n";
                File.AppendAllText(logPath, responseLog);

                if (!response.IsSuccessStatusCode)
                {
                    return new CreateShipmentResultDto
                    {
                        Success = false,
                        Message = $"فشل الاتصال بشركة التوصيل API (Status: {response.StatusCode})",
                        ErrorDetail = $"Response: {responseString} | Sent Payload: {jsonPayloadString}"
                    };
                }

                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;

                var packageId = root.TryGetProperty("id", out var idProp) ? idProp.ToString() : null;
                var barcode = root.TryGetProperty("barcode", out var bcProp) ? bcProp.ToString() : null;
                var codBarcode = root.TryGetProperty("codBarcode", out var cbcProp) ? cbcProp.ToString() : null;

                // Save OrderShipmentMapping in DB
                var mapping = new OrderShipmentMapping
                {
                    OrderId = order.Id,
                    ProviderKey = config.ProviderKey,
                    ExternalPackageId = packageId,
                    Barcode = barcode,
                    CodBarcode = codBarcode,
                    CurrentStatus = "CREATED",
                    ArabicStatus = "تم إنشاء الشحنة بنجاح 📦",
                    LastStatusUpdatedAt = DateTime.UtcNow,
                    RawWebhookPayload = responseString
                };

                await _unitOfWork.OrderShipmentMappings.AddAsync(mapping);
                await _unitOfWork.CompleteAsync();

                return new CreateShipmentResultDto
                {
                    Success = true,
                    PackageId = packageId,
                    Barcode = barcode,
                    CodBarcode = codBarcode,
                    CodAmount = (double)order.TotalAmount,
                    Message = "تم إضافة وإنشاء الشحنة بنجاح 📦"
                };
            }
            catch (Exception ex)
            {
                return new CreateShipmentResultDto
                {
                    Success = false,
                    Message = "حدث خطأ غير متوقع أثناء الاتصال بـ API التوصيل.",
                    ErrorDetail = ex.Message
                };
            }
        }

        public async Task<List<LogesTechsVillageDto>> GetVillagesAsync(string? search = null)
        {
            var config = await GetActiveConfigAsync();
            if (config == null || string.IsNullOrEmpty(config.CompanyId))
            {
                return new List<LogesTechsVillageDto>();
            }

            var uri = new Uri(config.BaseUrl);
            var baseDomain = $"{uri.Scheme}://{uri.Authority}";
            var requestUrl = $"{baseDomain}/api/addresses/villages";
            if (!string.IsNullOrWhiteSpace(search))
            {
                requestUrl += $"?search={Uri.EscapeDataString(search)}";
            }

            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add("company-id", config.CompanyId);

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return new List<LogesTechsVillageDto>();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                var list = new List<LogesTechsVillageDto>();
                JsonElement arrayElement = default;

                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    arrayElement = doc.RootElement;
                }
                else if (doc.RootElement.TryGetProperty("data", out var dataArr) && dataArr.ValueKind == JsonValueKind.Array)
                {
                    arrayElement = dataArr;
                }

                if (arrayElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in arrayElement.EnumerateArray())
                    {
                        list.Add(new LogesTechsVillageDto
                        {
                            Id = item.TryGetProperty("id", out var idP) && idP.ValueKind == JsonValueKind.Number ? idP.GetInt64() : 0,
                            Name = item.TryGetProperty("name", out var nP) ? nP.GetString() ?? "" : "",
                            ArabicName = item.TryGetProperty("arabicName", out var anP) ? anP.GetString() ?? "" : "",
                            CityId = item.TryGetProperty("cityId", out var cP) && cP.ValueKind == JsonValueKind.Number ? cP.GetInt64() : 0,
                            CityName = item.TryGetProperty("cityName", out var cnP) ? cnP.GetString() ?? "" : "",
                            RegionId = item.TryGetProperty("regionId", out var rP) && rP.ValueKind == JsonValueKind.Number ? rP.GetInt64() : 0,
                            RegionName = item.TryGetProperty("regionName", out var rnP) ? rnP.GetString() ?? "" : ""
                        });
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                File.AppendAllText("logs/delivery_api.log", $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] GetVillages EXCEPTION: {ex.Message}\n");
                return new List<LogesTechsVillageDto>();
            }
        }

        public async Task<string?> PrintShipmentAwbPdfAsync(List<long> packageIds)
        {
            var config = await GetActiveConfigAsync();
            if (config == null || string.IsNullOrEmpty(config.CompanyId)) return null;

            var requestUrl = $"{config.BaseUrl.TrimEnd('/')}/guests/{config.CompanyId}/packages/pdf";
            var payload = new { ids = packageIds };

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("company-id", config.CompanyId);

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.TryGetProperty("url", out var u) ? u.GetString() : null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> CancelShipmentAsync(long shipmentId)
        {
            var config = await GetActiveConfigAsync();
            if (config == null || string.IsNullOrEmpty(config.CompanyId)) return false;

            var requestUrl = $"{config.BaseUrl.TrimEnd('/')}/guests/{config.CompanyId}/packages/{shipmentId}/cancel";
            var payload = new { email = config.ApiEmail, password = config.ApiPassword };

            var request = new HttpRequestMessage(HttpMethod.Put, requestUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("company-id", config.CompanyId);

            try
            {
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<OrderShipmentMapping?> GetPackageStatusAsync(string barcodeOrId)
        {
            return await _unitOfWork.OrderShipmentMappings.GetAllAsQueryable()
                .FirstOrDefaultAsync(m => m.Barcode == barcodeOrId || m.ExternalPackageId == barcodeOrId);
        }

        public async Task<int> SyncProviderLocationsAsync(string? providerKey = null)
        {
            var config = await GetActiveConfigAsync();
            if (config == null) return 0;

            var targetProviderKey = !string.IsNullOrWhiteSpace(providerKey) ? providerKey : config.ProviderKey;
            var villages = await GetVillagesAsync();

            if (villages == null || !villages.Any()) return 0;

            var existingMappings = await _unitOfWork.DeliveryProviderLocationMappings.GetAllAsQueryable()
                .Where(m => m.ProviderKey.ToLower() == targetProviderKey.ToLower())
                .ToListAsync();

            int addedCount = 0;
            foreach (var v in villages)
            {
                var searchName = !string.IsNullOrEmpty(v.ArabicName) ? v.ArabicName : v.Name;
                if (string.IsNullOrWhiteSpace(searchName)) continue;

                var normalized = searchName.Replace("مدينة", "").Replace("منطقة", "").Replace("محافظة", "").Trim();

                var exists = existingMappings.Any(m => m.ExternalVillageId == v.Id);
                if (!exists)
                {
                    var entity = new DeliveryProviderLocationMapping
                    {
                        ProviderKey = targetProviderKey.ToLowerInvariant(),
                        SearchName = searchName,
                        NormalizedSearchName = normalized,
                        ExternalCityId = v.CityId,
                        ExternalCityName = v.CityName,
                        ExternalRegionId = v.RegionId,
                        ExternalRegionName = v.RegionName,
                        ExternalVillageId = v.Id,
                        ExternalVillageName = searchName,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.DeliveryProviderLocationMappings.AddAsync(entity);
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                await _unitOfWork.CompleteAsync();
            }

            return existingMappings.Count + addedCount;
        }
    }
}
