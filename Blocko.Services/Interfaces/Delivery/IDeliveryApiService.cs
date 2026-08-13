using System.Collections.Generic;
using System.Threading.Tasks;
using Bolcko.Domain.Entities.Delivery;
using Bolcko.Domain.Entities.Order;

namespace Blocko.Services.Interfaces.Delivery
{
    public class CreateShipmentResultDto
    {
        public bool Success { get; set; }
        public string? PackageId { get; set; }
        public string? Barcode { get; set; }
        public string? CodBarcode { get; set; }
        public double CodAmount { get; set; }
        public string? Message { get; set; }
        public string? ErrorDetail { get; set; }
    }

    public class LogesTechsVillageDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ArabicName { get; set; } = string.Empty;
        public long CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public long RegionId { get; set; }
        public string RegionName { get; set; } = string.Empty;
    }

    public interface IDeliveryApiService
    {
        Task<CreateShipmentResultDto> CreateShipmentAsync(Bolcko.Domain.Entities.Order.Order order, string destinationCityId, string destinationRegionId, string destinationVillageId, string? notes = null);
        Task<List<LogesTechsVillageDto>> GetVillagesAsync(string? search = null);
        Task<string?> PrintShipmentAwbPdfAsync(List<long> packageIds);
        Task<bool> CancelShipmentAsync(long shipmentId);
        Task<OrderShipmentMapping?> GetPackageStatusAsync(string barcodeOrId);
        Task<DeliveryProviderConfig?> GetActiveConfigAsync();
        Task<bool> SaveConfigAsync(DeliveryProviderConfig config);
    }
}
