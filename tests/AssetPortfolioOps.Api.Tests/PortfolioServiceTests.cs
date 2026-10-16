using AssetPortfolioOps.Api.Data;
using AssetPortfolioOps.Api.Features.Portfolios;

namespace AssetPortfolioOps.Api.Tests;

public sealed class PortfolioServiceTests
{
    [Fact]
    public void GetPortfolio_ReturnsExpectedMarketValue()
    {
        using var dbContext = TestDbContextFactory.Create();
        var service = new PortfolioService(dbContext);

        var portfolio = service.GetPortfolio(SeedData.CustomerEmmaId);

        Assert.NotNull(portfolio);
        Assert.Equal(portfolio.Items.Sum(item => item.TotalMarketValue), portfolio.TotalMarketValue);
        Assert.Equal(portfolio.Items.Sum(item => item.GainLoss), portfolio.TotalGainLoss);
        Assert.True(portfolio.TotalMarketValue > 0);
    }

    [Fact]
    public void GetPortfolio_ReturnsNull_WhenCustomerDoesNotExist()
    {
        using var dbContext = TestDbContextFactory.Create();
        var service = new PortfolioService(dbContext);

        var portfolio = service.GetPortfolio(Guid.NewGuid());

        Assert.Null(portfolio);
    }
}
