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
    public class LogesTechsApiService : ILogesTechsApiService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly HttpClient _httpClient;

        public LogesTechsApiService(IUnitOfWork unitOfWork, HttpClient httpClient)
        {
            _unitOfWork = unitOfWork;
            _httpClient = httpClient;
        }

        public async Task<DeliveryProviderConfig?> GetActiveConfigAsync()
        {
            return await _unitOfWork.DeliveryProviderConfigs.GetAllAsQueryable()
                .FirstOrDefaultAsync(c => c.ProviderKey == "LogesTechs" && c.IsActive);
        }

        public async Task<bool> SaveConfigAsync(DeliveryProviderConfig config)
        {
            var existing = await _unitOfWork.DeliveryProviderConfigs.GetAllAsQueryable()
                .FirstOrDefaultAsync(c => c.ProviderKey == "LogesTechs");

            if (existing != null)
            {
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
            }
            else
            {
                config.ProviderName = "LogesTechs";
                config.ProviderKey = "LogesTechs";
                config.CreatedAt = DateTime.UtcNow;
                await _unitOfWork.DeliveryProviderConfigs.AddAsync(config);
            }

            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<CreateShipmentResultDto> CreateShipmentAsync(Bolcko.Domain.Entities.Order.Order order, string destinationCityId, string destinationRegionId, string destinationVillageId, string? notes = null)
        {
            var config = await GetActiveConfigAsync();
            if (config == null || string.IsNullOrEmpty(config.CompanyId) || string.IsNullOrEmpty(config.ApiEmail))
            {
                return new CreateShipmentResultDto
                {
                    Success = false,
                    Message = "إعدادات LogesTechs API غير مكتملة في لوحة التحكم."
                };
            }

            var requestUrl = $"{config.BaseUrl.TrimEnd('/')}/ship/request/by-email";

            var payloadObj = new
            {
                email = config.ApiEmail,
                password = config.ApiPassword,
                pkgUnitType = "METRIC",
                pkg = new
                {
                    cod = (double)order.TotalAmount,
                    notes = notes ?? $"طلب مواد بناء رقم #{order.OrderNumber}",
                    invoiceNumber = order.OrderNumber,
                    senderName = "بلوكو لتوريدات البناء",
                    businessSenderName = "Bolcko Building Materials",
                    senderPhone = "0599000000",
                    receiverName = order.User != null ? $"{order.User.FirstName} {order.User.LastName}".Trim() : "عميل بلوكو",
                    receiverPhone = order.User?.PhoneNumber ?? "0590000000",
                    serviceType = "STANDARD",
                    shipmentType = "COD",
                    quantity = 1,
                    description = $"طلب مواد بناء - {order.Items?.Count ?? 1} أصناف",
                    webhookUrl = config.OutboundWebhookUrl ?? ""
                },
                destinationAddress = new
                {
                    addressLine1 = order.ShippingAddress?.AddressLine1 ?? "عنوان العميل",
                    cityId = long.TryParse(destinationCityId, out var cId) ? cId : 1,
                    regionId = long.TryParse(destinationRegionId, out var rId) ? rId : 1,
                    villageId = long.TryParse(destinationVillageId, out var vId) ? vId : 1,
                    latitude = order.ShippingAddress?.Latitude ?? 31.9038,
                    longitude = order.ShippingAddress?.Longitude ?? 35.2058
                },
                originAddress = new
                {
                    addressLine1 = "المركز الرئيسي لتوريدات البناء",
                    cityId = 1,
                    regionId = 1,
                    villageId = 1
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(payloadObj), Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = jsonContent
            };
            request.Headers.Add("company-id", config.CompanyId);

            try
            {
                var response = await _httpClient.SendAsync(request);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new CreateShipmentResultDto
                    {
                        Success = false,
                        Message = $"فشل الاتصال بـ LogesTechs API (Status: {response.StatusCode})",
                        ErrorDetail = responseString
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
                    ProviderKey = "LogesTechs",
                    ExternalPackageId = packageId,
                    Barcode = barcode,
                    CodBarcode = codBarcode,
                    CodAmount = (double)order.TotalAmount,
                    CurrentStatus = "PENDING_CUSTOMER_CARE_APPROVAL",
                    ArabicStatus = "طلب جديد بمجتمع LogesTechs",
                    DispatchedAt = DateTime.UtcNow
                };

                await _unitOfWork.OrderShipmentMappings.AddAsync(mapping);
                await _unitOfWork.SaveChangesAsync();

                return new CreateShipmentResultDto
                {
                    Success = true,
                    PackageId = packageId,
                    Barcode = barcode,
                    CodBarcode = codBarcode,
                    CodAmount = (double)order.TotalAmount,
                    Message = "تم إنشاء ورفع الشحنة لدى LogesTechs بنجاح!"
                };
            }
            catch (Exception ex)
            {
                return new CreateShipmentResultDto
                {
                    Success = false,
                    Message = "حدث خطأ غير متوقع أثناء الاتصال بالـ API.",
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

            var requestUrl = $"{config.BaseUrl.TrimEnd('/')}/addresses/villages";
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
                if (doc.RootElement.TryGetProperty("data", out var dataArr) && dataArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in dataArr.EnumerateArray())
                    {
                        list.Add(new LogesTechsVillageDto
                        {
                            Id = item.GetProperty("id").GetInt64(),
                            Name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                            ArabicName = item.TryGetProperty("arabicName", out var an) ? an.GetString() ?? "" : "",
                            CityId = item.TryGetProperty("cityId", out var c) ? c.GetInt64() : 0,
                            CityName = item.TryGetProperty("cityName", out var cn) ? cn.GetString() ?? "" : "",
                            RegionId = item.TryGetProperty("regionId", out var r) ? r.GetInt64() : 0,
                            RegionName = item.TryGetProperty("regionName", out var rn) ? rn.GetString() ?? "" : ""
                        });
                    }
                }
                return list;
            }
            catch
            {
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
    }
}
