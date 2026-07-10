using Bolcko.Domain.Entities.Delivery;

namespace Blocko.Services.Interfaces.Delivery
{
    public interface IDeliveryDocumentService
    {
        byte[] GenerateExcelSheet(DeliveryJob job);
        byte[] GeneratePdfDocument(DeliveryJob job);
    }
}
