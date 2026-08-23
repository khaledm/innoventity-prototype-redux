namespace Innoventity.API.Domain.Entities;

public class Market
    {
        public required string TargetMarket { get; init; }
        public required string TargetCustomerBase { get; init; }
        public required string TargetCustomerType { get; init; }
        public decimal? RelevantMarketSize { get; set; }
        public decimal? PotentialMarketSize { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(TargetMarket))
            {
                throw new ArgumentNullException(nameof(TargetMarket), "TargetMarket is required.");
            }

            if (string.IsNullOrWhiteSpace(TargetCustomerBase))
            {
                throw new ArgumentNullException(nameof(TargetCustomerBase), "TargetCustomerBase is required.");
            }

            if (string.IsNullOrWhiteSpace(TargetCustomerType))
            {
                throw new ArgumentNullException(nameof(TargetCustomerType), "TargetCustomerType is required.");
            }
        }

    public bool IsComplete()
    {
        return RelevantMarketSize > 0 &&
               PotentialMarketSize > 0;
    }
}
