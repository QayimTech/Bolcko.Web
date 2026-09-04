using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Blocko.Services.Interfaces.Supplier;
using Bolcko.Domain.Entities.Supplier;
using Bolcko.Domain.Enums;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderEntity = Bolcko.Domain.Entities.Order.Order;

namespace Blocko.Services.Implementations.Supplier
{
    public class SupplierApiService : ISupplierApiService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly HttpClient _httpClient;
        private readonly ILogger<SupplierApiService> _logger;

        public SupplierApiService(IUnitOfWork unitOfWork, HttpClient httpClient, ILogger<SupplierApiService> logger)
        {
            _unitOfWork = unitOfWork;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<SupplierProviderConfig?> GetActiveConfigAsync(string supplierKey = "qannas")
        {
            return await _unitOfWork.SupplierProviderConfigs.GetAllAsQueryable()
                .FirstOrDefaultAsync(c => c.IsActive && c.SupplierKey.ToLower() == supplierKey.ToLower());
        }

        public async Task<List<SupplierProviderConfig>> GetAllConfigsAsync()
        {
            return await _unitOfWork.SupplierProviderConfigs.GetAllAsQueryable().ToListAsync();
        }

        public async Task<bool> SaveConfigAsync(SupplierProviderConfig config)
        {
            var existing = await _unitOfWork.SupplierProviderConfigs.GetAllAsQueryable()
                .FirstOrDefaultAsync(c => c.Id == config.Id || c.SupplierKey.ToLower() == config.SupplierKey.ToLower());

            if (existing != null)
            {
                existing.SupplierName = config.SupplierName;
                existing.SupplierKey = config.SupplierKey;
                existing.BaseUrl = config.BaseUrl;
                existing.ApiPhoneNumber = config.ApiPhoneNumber;
                existing.ApiPassword = config.ApiPassword;
                existing.ApiEmail = config.ApiEmail;
                existing.PickupCityId = config.PickupCityId;
                existing.PickupRegionId = config.PickupRegionId;
                existing.PickupVillageId = config.PickupVillageId;
                existing.PickupAddressLine = config.PickupAddressLine;
                existing.ContactPersonName = config.ContactPersonName;
                existing.ContactPhoneNumber = config.ContactPhoneNumber;
                existing.AutoDispatchEnabled = config.AutoDispatchEnabled;
                existing.IsActive = config.IsActive;
                existing.CustomHeadersJson = config.CustomHeadersJson;
                existing.CustomPayloadMappingJson = config.CustomPayloadMappingJson;
                existing.Notes = config.Notes;
                existing.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.SupplierProviderConfigs.Update(existing);
            }
            else
            {
                config.CreatedAt = DateTime.UtcNow;
                config.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.SupplierProviderConfigs.AddAsync(config);
            }

            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<bool> TestConnectionAsync(SupplierProviderConfig config)
        {
            try
            {
                var token = await AuthenticateAndGetTokenAsync(config);
                return !string.IsNullOrEmpty(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TestConnectionAsync failed for supplier {SupplierKey}", config.SupplierKey);
                return false;
            }
        }

        public async Task<SupplierOrderResultDto> CreatePurchaseOrderAsync(OrderEntity order, SupplierProviderConfig? config = null)
        {
            config ??= await GetActiveConfigAsync(order.PrimarySupplierKey ?? "qannas");
            if (config == null || !config.IsActive)
            {
                return new SupplierOrderResultDto
                {
                    Success = false,
                    Message = "إعدادات المورد غير مفعّلة في النظام."
                };
            }

            try
            {
                var token = await AuthenticateAndGetTokenAsync(config);
                if (string.IsNullOrEmpty(token))
                {
                    return new SupplierOrderResultDto
                    {
                        Success = false,
                        Message = "فشل تسجيل الدخول وجلب التوكن من مزود الجملة."
                    };
                }

                // If Qannas Provider
                if (config.SupplierKey.Equals("qannas", StringComparison.OrdinalIgnoreCase))
                {
                    return await SendQannasOrderAsync(order, config, token);
                }

                // Fallback / Generic REST API
                return new SupplierOrderResultDto
                {
                    Success = false,
                    Message = $"نوع المورد '{config.SupplierKey}' غير مدعوم حالياً كربط مباشر."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing CreatePurchaseOrderAsync for Order #{OrderId}", order.Id);
                return new SupplierOrderResultDto
                {
                    Success = false,
                    Message = $"خطأ استثنائي أثناء إرسال أمر الشراء: {ex.Message}",
                    ErrorDetail = ex.StackTrace
                };
            }
        }

        private async Task<SupplierOrderResultDto> SendQannasOrderAsync(OrderEntity order, SupplierProviderConfig config, string token)
        {
            var baseUrl = config.BaseUrl.TrimEnd('/');
            var orderUrl = $"{baseUrl}/ar/api/Orders/Add";

            var customerId = config.ExternalCustomerId ?? 503;

            // Build Order Details list
            var orderDetailsList = new List<object>();
            decimal calculatedTotal = 0;

            if (order.Items != null && order.Items.Any())
            {
                foreach (var item in order.Items)
                {
                    int variantId = item.ExternalSupplierVariantId ?? item.Product?.ExternalSupplierVariantId ?? 3643; // Default fallback variant
                    decimal price = item.UnitPrice > 0 ? item.UnitPrice : 10.0m;
                    calculatedTotal += price * item.Quantity;

                    var pName = item.Product?.Name ?? $"منتج #{item.ProductId}";
                    orderDetailsList.Add(new
                    {
                        price = (double)price,
                        quantity = item.Quantity,
                        productVariantsId = variantId,
                        note = $"طلب توريد لمتجر بلوكو (طلب #{order.OrderNumber}) - منتج: {pName}",
                        vendorNote = "جاهز للاستلام والتجهيز من قبل موظف بلوكو في مستودع رأس العين"
                    });
                }
            }
            else
            {
                calculatedTotal = order.TotalAmount > 0 ? order.TotalAmount : 10.0m;
                orderDetailsList.Add(new
                {
                    price = (double)calculatedTotal,
                    quantity = 1,
                    productVariantsId = 3643,
                    note = $"طلب توريد لمتجر بلوكو #{order.OrderNumber}",
                    vendorNote = "تسليم لموظف بلوكو في رأس العين"
                });
            }

            var custName = order.User != null ? $"{order.User.FirstName} {order.User.LastName}".Trim() : (order.ShippingAddress?.AddressLine1 ?? "عميل بلوكو");
            var custPhone = order.User?.PhoneNumber ?? "0780000000";
            var fullAddress = $"عمان - رأس العين - مستودع القناص [استلام موظف بلوكو لتسليم GLC] - للعميل: {custName} ({custPhone}) - {order.ShippingAddress?.City}";

            var payloadObj = new
            {
                totalPrice = (double)calculatedTotal,
                addresses = fullAddress,
                customersId = customerId,
                orderDetails = orderDetailsList
            };

            var json = JsonSerializer.Serialize(payloadObj);
            var request = new HttpRequestMessage(HttpMethod.Post, orderUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Qannas Order API Response (HTTP {Status}): {Body}", response.StatusCode, responseBody);

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    var root = doc.RootElement;
                    var success = root.TryGetProperty("success", out var sProp) && sProp.GetBoolean();
                    
                    if (success && root.TryGetProperty("data", out var dataEl))
                    {
                        int orderId = dataEl.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : 0;
                        int orderNum = dataEl.TryGetProperty("orderNumber", out var numProp) ? numProp.GetInt32() : 0;

                        // Update Order tracking
                        order.ExternalSupplierOrderNumber = orderNum > 0 ? orderNum.ToString() : orderId.ToString();
                        order.SourcingStatus = SourcingStatus.SentToSupplier;
                        order.SourcingNotes = $"تم إنشاء أمر التوريد بنجاح في نظام القناص برقم #{orderNum} (ID: {orderId}) لمستودع رأس العين";
                        _unitOfWork.Orders.Update(order);
                        await _unitOfWork.SaveChangesAsync();

                        return new SupplierOrderResultDto
                        {
                            Success = true,
                            Message = $"تم إرسال أمر الشراء للقناص بنجاح! رقم الطلب لديهم: #{orderNum}",
                            ExternalOrderId = orderId,
                            ExternalOrderNumber = orderNum.ToString()
                        };
                    }
                    else
                    {
                        var msg = root.TryGetProperty("message", out var mProp) ? mProp.GetString() : "استجابة غير متوقعة من المورد";
                        return new SupplierOrderResultDto
                        {
                            Success = false,
                            Message = $"فشل إنشاء الطلب لدى القناص: {msg}",
                            ErrorDetail = responseBody
                        };
                    }
                }
                catch (Exception ex)
                {
                    return new SupplierOrderResultDto
                    {
                        Success = false,
                        Message = "تم الاتصال ولكن حدث خطأ في تحليل استجابة المورد.",
                        ErrorDetail = ex.Message
                    };
                }
            }

            return new SupplierOrderResultDto
            {
                Success = false,
                Message = $"فشل الاتصال بسيرفر القناص (HTTP {response.StatusCode})",
                ErrorDetail = responseBody
            };
        }

        public async Task<SupplierOrderStatusDto> GetOrderStatusAsync(string externalOrderNumber, SupplierProviderConfig? config = null)
        {
            config ??= await GetActiveConfigAsync("qannas");
            if (config == null) return new SupplierOrderStatusDto { Success = false, StatusText = "المورد غير مفعل" };

            var list = await SyncOrdersStatusAsync(config);
            var match = list.FirstOrDefault(o => o.ExternalOrderNumber == externalOrderNumber);
            return match ?? new SupplierOrderStatusDto { Success = false, StatusText = "لم يتم العثور على الطلب" };
        }

        public async Task<List<SupplierOrderStatusDto>> SyncOrdersStatusAsync(SupplierProviderConfig? config = null)
        {
            config ??= await GetActiveConfigAsync("qannas");
            var result = new List<SupplierOrderStatusDto>();
            if (config == null || !config.IsActive) return result;

            try
            {
                var token = await AuthenticateAndGetTokenAsync(config);
                if (string.IsNullOrEmpty(token)) return result;

                var baseUrl = config.BaseUrl.TrimEnd('/');
                var url = $"{baseUrl}/ar/api/Orders/GetByStatus";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return result;

                var jsonStr = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonStr);
                if (doc.RootElement.TryGetProperty("data", out var dataArr) && dataArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in dataArr.EnumerateArray())
                    {
                        var orderNum = item.TryGetProperty("orderNumber", out var on) ? on.GetInt32().ToString() : "";
                        var status = item.TryGetProperty("orderStatus", out var os) ? os.GetInt32() : 0;
                        var approvalText = item.TryGetProperty("salesApprovalStatusText", out var st) ? st.GetString() : "";
                        var total = item.TryGetProperty("totalPrice", out var tp) ? tp.GetDecimal() : 0;

                        string statusArabic = status switch
                        {
                            0 => "جديد (بانتظار الموافقة)",
                            1 => "قيد التجهيز في مستودع القناص",
                            2 => "تم التجهيز والبيك أب",
                            3 => "مع المندوب",
                            4 => "مكتمل / تم التسليم",
                            5 => "ملغي / مرتجع",
                            _ => $"حالة #{status}"
                        };

                        result.Add(new SupplierOrderStatusDto
                        {
                            Success = true,
                            ExternalOrderNumber = orderNum,
                            OrderStatus = status,
                            StatusText = statusArabic,
                            SalesApprovalStatusText = approvalText,
                            TotalPrice = total
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing supplier orders status");
            }

            return result;
        }

        private async Task<string?> AuthenticateAndGetTokenAsync(SupplierProviderConfig config)
        {
            if (config.SupplierKey.Equals("qannas", StringComparison.OrdinalIgnoreCase))
            {
                var baseUrl = config.BaseUrl.TrimEnd('/');
                var loginUrl = $"{baseUrl}/ar/api/Auth/CustomerLogin";

                var phone = !string.IsNullOrWhiteSpace(config.ApiPhoneNumber) ? config.ApiPhoneNumber.Trim() : "0782023800";
                var password = !string.IsNullOrWhiteSpace(config.ApiPassword) ? config.ApiPassword.Trim() : "Qannas@100";

                var bodyJson = JsonSerializer.Serialize(new { phoneNumber = phone, password = password });
                var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

                var resp = await _httpClient.PostAsync(loginUrl, content);
                if (resp.IsSuccessStatusCode)
                {
                    var respStr = await resp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(respStr);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("data", out var dataEl))
                    {
                        if (dataEl.TryGetProperty("id", out var custId))
                        {
                            config.ExternalCustomerId = custId.GetInt32();
                        }
                        if (dataEl.TryGetProperty("userId", out var uId))
                        {
                            config.ExternalUserId = uId.GetInt32();
                        }
                        if (dataEl.TryGetProperty("user", out var userEl) && userEl.TryGetProperty("token", out var tokenEl))
                        {
                            var token = tokenEl.GetString();
                            config.AuthToken = token;
                            _unitOfWork.SupplierProviderConfigs.Update(config);
                            await _unitOfWork.SaveChangesAsync();
                            return token;
                        }
                    }
                }
            }

            return null;
        }
    }
}
