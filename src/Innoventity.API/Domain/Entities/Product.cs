namespace Innoventity.API.Domain.Entities;

public class Product
{
    private string _productKeywords = string.Empty;
    public required string ProductDescription { get; init; }
    public required string TechnologyDescription { get; init; }
    public required string ProductAdvantages { get; init; }
    public required string DevelopmentPhase { get; init; }
    public required string DevelopmentProcess { get; init; }
    public required string TargetBeneficiaries { get; init; }
    public required string ProductKeywords { get => _productKeywords; init => _productKeywords = value?.Trim() ?? string.Empty; }
    public required string AdvantageKeywords { get; init; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ProductDescription))
        {
            throw new ArgumentNullException(nameof(ProductDescription), "ProductDescription is required.");
        }

        if (string.IsNullOrWhiteSpace(TechnologyDescription))
        {
            throw new ArgumentNullException(nameof(TechnologyDescription), "TechnologyDescription is required.");
        }

        if (string.IsNullOrWhiteSpace(DevelopmentPhase))
        {
            throw new ArgumentNullException(nameof(DevelopmentPhase), "DevelopmentPhase is required.");
        }

        if (string.IsNullOrWhiteSpace(DevelopmentProcess))
        {
            throw new ArgumentNullException(nameof(DevelopmentProcess), "DevelopmentProcess is required.");
        }

        if (string.IsNullOrWhiteSpace(TargetBeneficiaries))
        {
            throw new ArgumentNullException(nameof(TargetBeneficiaries), "TargetBeneficiaries is required.");
        }

        if (string.IsNullOrWhiteSpace(ProductKeywords))
        {
            throw new ArgumentNullException(nameof(ProductKeywords), "ProductKeywords is required.");
        }

        if (string.IsNullOrWhiteSpace(AdvantageKeywords))
        {
            throw new ArgumentNullException(nameof(AdvantageKeywords), "AdvantageKeywords is required.");
        }
    }

    public bool IsComplete()
    {
        return
                !string.IsNullOrWhiteSpace(ProductDescription) &&
                !string.IsNullOrWhiteSpace(TechnologyDescription) &&
                !string.IsNullOrWhiteSpace(TargetBeneficiaries);
    }
}
