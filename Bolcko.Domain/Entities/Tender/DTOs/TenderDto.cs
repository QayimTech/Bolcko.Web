using Bolcko.Domain.Enums;

namespace Bolcko.Domain.Entities.Tender.DTOs
{
    public class TenderDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string TenderTitle { get; set; } = string.Empty;
        public string? TenderDescription { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? SubmissionDeadline { get; set; }
        public DateTime? RequiredDeliveryDate { get; set; }
        public TenderStatus Status { get; set; }
        public decimal? TotalQuotedAmount { get; set; }
        public int ItemCount { get; set; }
    }
}
