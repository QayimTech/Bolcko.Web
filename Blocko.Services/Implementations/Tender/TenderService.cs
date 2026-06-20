using Blocko.Services.Interfaces.Tender;
using Bolcko.Domain.Entities.Tender;
using Bolcko.Domain.Entities.Tender.DTOs;
using Bolcko.Domain.Interfaces;

namespace Blocko.Services.Implementations.Tender
{
    public class TenderService : ITenderService
    {
        private readonly IUnitOfWork _unitOfWork;
        public TenderService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<TenderDto> CreateTenderAsync(TenderDto tenderDto)
        {
            var tender = new Bolcko.Domain.Entities.Tender.Tender
            {
                UserId = tenderDto.UserId,
                TenderTitle = tenderDto.TenderTitle,
                TenderDescription = tenderDto.TenderDescription,
                RequestDate = DateTime.UtcNow,
                SubmissionDeadline = tenderDto.SubmissionDeadline,
                RequiredDeliveryDate = tenderDto.RequiredDeliveryDate,
                Status = tenderDto.Status
            };
            await _unitOfWork.Tenders.AddAsync(tender);
            await _unitOfWork.CompleteAsync();
            tenderDto.Id = tender.Id;
            return tenderDto;
        }

        public async Task<TenderDto> CreateQuoteRequestAsync(QuoteRequestDto quoteRequestDto, int? userId = null)
        {
            var tender = new Bolcko.Domain.Entities.Tender.Tender
            {
                UserId = userId, // Null for guest users
                GuestName = !userId.HasValue ? quoteRequestDto.FullName : null,
                GuestEmail = !userId.HasValue ? quoteRequestDto.Email : null,
                GuestPhone = !userId.HasValue ? quoteRequestDto.Phone : null,
                GuestCompany = !userId.HasValue ? quoteRequestDto.CompanyName : null,
                GuestCity = !userId.HasValue ? quoteRequestDto.City : null,
                TenderTitle = $"Quote Request: {quoteRequestDto.ProjectName ?? "General Materials"}",
                TenderDescription = $"Company: {quoteRequestDto.CompanyName}\n" +
                                    $"Name: {quoteRequestDto.FullName}\n" +
                                    $"Email: {quoteRequestDto.Email}\n" +
                                    $"Phone: {quoteRequestDto.Phone}\n" +
                                    $"City: {quoteRequestDto.City}\n" +
                                    $"Project Type: {quoteRequestDto.ProjectType}\n" +
                                    $"Notes: {quoteRequestDto.Notes}",
                RequestDate = DateTime.UtcNow,
                Status = Bolcko.Domain.Enums.TenderStatus.Pending
            };

            await _unitOfWork.Tenders.AddAsync(tender);
            await _unitOfWork.CompleteAsync();

            if (quoteRequestDto.Products?.Any() ?? false)
            {
                foreach (var product in quoteRequestDto.Products)
                {
                    var tenderItem = new TenderItem
                    {
                        TenderId = tender.Id,
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        Unit = product.Unit,
                        RequestedQuantity = product.Quantity
                    };
                    await _unitOfWork.TenderItems.AddAsync(tenderItem);
                }
                await _unitOfWork.CompleteAsync();
            }

            return new TenderDto { Id = tender.Id, TenderTitle = tender.TenderTitle };
        }

        public async Task<IEnumerable<TenderDto>> GetOpenTendersAsync()
        {
            var tenders = await _unitOfWork.Tenders.GetOpenTendersAsync();
            return tenders.Select(t => new TenderDto
            {
                Id = t.Id,
                UserId = t.UserId,
                UserName = t.User?.UserName,
                TenderTitle = t.TenderTitle,
                TenderDescription = t.TenderDescription,
                RequestDate = t.RequestDate,
                SubmissionDeadline = t.SubmissionDeadline,
                RequiredDeliveryDate = t.RequiredDeliveryDate,
                Status = t.Status,
                TotalQuotedAmount = t.TotalQuotedAmount,
                ItemCount = t.Items?.Count ?? 0
            });
        }

        public async Task<IEnumerable<TenderDto>> GetTendersByUserAsync(int userId)
        {
            var tenders = await _unitOfWork.Tenders.FindAsync(t => t.UserId == userId);
            return tenders.Select(t => new TenderDto
            {
                Id = t.Id,
                UserId = t.UserId,
                UserName = t.User?.UserName,
                TenderTitle = t.TenderTitle,
                TenderDescription = t.TenderDescription,
                RequestDate = t.RequestDate,
                SubmissionDeadline = t.SubmissionDeadline,
                RequiredDeliveryDate = t.RequiredDeliveryDate,
                Status = t.Status,
                TotalQuotedAmount = t.TotalQuotedAmount,
                ItemCount = t.Items?.Count ?? 0
            });
        }

        public async Task<Bolcko.Domain.Entities.Tender.Tender?> GetTenderByIdAsync(int id)
        {
            return await _unitOfWork.Tenders.GetByIdAsync(id);
        }

        public async Task<bool> SubmitQuotationPricesAsync(int tenderId, Dictionary<int, decimal> itemPrices, string? notes)
        {
            var tender = await _unitOfWork.Tenders.GetByIdAsync(tenderId);
            if (tender == null) return false;

            decimal totalAmount = 0;
            foreach (var item in tender.Items)
            {
                if (itemPrices.TryGetValue(item.Id, out decimal price))
                {
                    item.ProposedPricePerUnit = price;
                    item.SubtotalItem = price * item.RequestedQuantity;
                    totalAmount += item.SubtotalItem.Value;
                }
            }

            tender.TotalQuotedAmount = totalAmount;
            tender.Status = Bolcko.Domain.Enums.TenderStatus.Priced;
            if (!string.IsNullOrEmpty(notes))
            {
                tender.NegotiationNotes = notes;
            }

            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<bool> RequestPriceNegotiationAsync(int tenderId, Dictionary<int, decimal> targetPrices, string feedback)
        {
            var tender = await _unitOfWork.Tenders.GetByIdAsync(tenderId);
            if (tender == null) return false;

            foreach (var item in tender.Items)
            {
                if (targetPrices.TryGetValue(item.Id, out decimal targetPrice))
                {
                    item.TargetPricePerUnit = targetPrice;
                }
            }

            tender.NegotiationNotes = feedback;
            tender.Status = Bolcko.Domain.Enums.TenderStatus.Negotiation;

            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<bool> AcceptQuotationAsync(int tenderId)
        {
            var tender = await _unitOfWork.Tenders.GetByIdAsync(tenderId);
            if (tender == null) return false;

            tender.Status = Bolcko.Domain.Enums.TenderStatus.Accepted;
            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<bool> RejectTenderAsync(int tenderId, string reason)
        {
            var tender = await _unitOfWork.Tenders.GetByIdAsync(tenderId);
            if (tender == null) return false;

            tender.Status = Bolcko.Domain.Enums.TenderStatus.Rejected;
            tender.NegotiationNotes = reason;
            await _unitOfWork.CompleteAsync();
            return true;
        }
    }
}