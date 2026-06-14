namespace AssetPortfolioOps.Api.Features.Portfolios;

public interface IPortfolioService
{
    PortfolioResponse? GetPortfolio(Guid customerId);
}
