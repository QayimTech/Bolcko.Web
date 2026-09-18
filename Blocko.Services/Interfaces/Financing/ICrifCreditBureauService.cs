using System;
using System.Threading.Tasks;

namespace Blocko.Services.Interfaces.Financing
{
    public class CrifCreditAssessmentResult
    {
        public string NationalIdOrRegNo { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public int CreditScore { get; set; } // 300 - 850
        public string RiskGrade { get; set; } = "AA"; // AAA, AA, A, BBB, BB, C
        public double TrustScorePercentage { get; set; } // e.g. 96.5%
        public decimal RecommendedCreditLimitJod { get; set; }
        public int BouncedChecksCount { get; set; }
        public decimal BouncedChecksTotalAmountJod { get; set; }
        public bool IsFinancingApproved { get; set; }
        public string BureauReferenceNumber { get; set; } = string.Empty;
        public DateTime AssessmentDateUtc { get; set; } = DateTime.UtcNow;
        public string SummaryNotes { get; set; } = string.Empty;
    }

    public interface ICrifCreditBureauService
    {
        Task<CrifCreditAssessmentResult> AssessContractorAsync(string nationalIdOrRegNo, decimal requestedFinancingAmount, string companyName);
    }
}
