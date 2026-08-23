namespace Innoventity.API.Domain.Entities;

public class IdeaSummary
{
    public required string Title { get; init; }
    public required string ProductType { get; init; }
    public ResearchCategory ResearchCategory { get; init; }
    public required string IprStatus { get; init; }
    public required string ResearchBackground { get; init; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            throw new ArgumentNullException(nameof(Title), "Title is required.");
        }

        if (string.IsNullOrWhiteSpace(ProductType))
        {
            throw new ArgumentNullException(nameof(ProductType), "ProductType is required.");
        }

        if (!Enum.IsDefined(typeof(ResearchCategory), ResearchCategory))
        {
            throw new ArgumentOutOfRangeException(nameof(ResearchCategory), "Invalid ResearchCategory value.");
        }

        if (string.IsNullOrWhiteSpace(IprStatus))
        {
            throw new ArgumentNullException(nameof(IprStatus), "IprStatus is required.");
        }

        if (string.IsNullOrWhiteSpace(ResearchBackground))
        {
            throw new ArgumentNullException(nameof(ResearchBackground), "ResearchBackground is required.");
        }
    }

    public bool IsComplete()
    {
        return !string.IsNullOrWhiteSpace(Title) &&
            !Title.Contains("Untitled", StringComparison.OrdinalIgnoreCase)
            && !Title.Contains("TODO", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(ProductType)
            && Enum.IsDefined(typeof(ResearchCategory), ResearchCategory) 
            && !string.IsNullOrWhiteSpace(IprStatus) 
            && !string.IsNullOrWhiteSpace(ResearchBackground) 
            && ResearchBackground.Length >= 50; // Assuming a minimum length for ResearchBackground
    }
}
