using AssetPortfolioOps.Api.Features.Portfolios;

namespace AssetPortfolioOps.Api.Tests;

public sealed class PortfolioConcentrationTests
{
    private static PositionValue Position(string name, decimal value, int quantity = 1)
    {
        return new PositionValue(Guid.NewGuid(), name, quantity, value);
    }

    [Theory]
    [InlineData(59.99, "None")]
    [InlineData(60, "Medium")]
    [InlineData(69.99, "Medium")]
    [InlineData(70, "High")]
    [InlineData(100, "High")]
    public void LevelFor_UsesTheRiskIndicatorThresholds(double sharePercent, string expected)
    {
        Assert.Equal(expected, PortfolioConcentration.LevelFor((decimal)sharePercent));
    }

    [Fact]
    public void Calculate_ReturnsSharesOrderedByValue()
    {
        var result = PortfolioConcentration.Calculate([
            Position("Small", 25m),
            Position("Large", 75m)
        ]);

        Assert.Equal(100m, result.TotalMarketValue);
        Assert.Equal(["Large", "Small"], result.Positions.Select(position => position.AssetName));
        Assert.Equal(75m, result.LargestSharePercent);
        Assert.Equal("High", result.Level);
    }

    [Fact]
    public void Calculate_MergesPositionsOfTheSameAsset()
    {
        var assetId = Guid.NewGuid();

        var result = PortfolioConcentration.Calculate([
            new PositionValue(assetId, "Bordeaux", 6, 600m),
            new PositionValue(assetId, "Bordeaux", 2, 200m),
            Position("Other", 200m)
        ]);

        var bordeaux = Assert.Single(result.Positions, position => position.AssetId == assetId);
        Assert.Equal(8, bordeaux.Quantity);
        Assert.Equal(800m, bordeaux.MarketValue);
        Assert.Equal(80m, bordeaux.SharePercent);
    }

    [Fact]
    public void Calculate_HandlesAnEmptyPortfolio()
    {
        var result = PortfolioConcentration.Calculate([]);

        Assert.Equal(0m, result.TotalMarketValue);
        Assert.Null(result.LargestPosition);
        Assert.Equal("None", result.Level);
    }
}
