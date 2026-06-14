import { useEffect, useMemo, useState } from "react";
import "./App.css";
import { api, type Asset, type AuditEvent, type InventoryItem, type Portfolio, type PurchaseRequest } from "./api";

const demoCustomerId = "11111111-1111-1111-1111-111111111111";
const demoAssetId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

function App() {
  const [assets, setAssets] = useState<Asset[]>([]);
  const [portfolio, setPortfolio] = useState<Portfolio | null>(null);
  const [inventory, setInventory] = useState<InventoryItem[]>([]);
  const [purchaseRequests, setPurchaseRequests] = useState<PurchaseRequest[]>([]);
  const [auditEvents, setAuditEvents] = useState<AuditEvent[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const totalInventory = useMemo(() => {
    return inventory.reduce((total, item) => total + item.quantityAvailable, 0);
  }, [inventory]);

  async function loadDashboard() {
    try {
      setIsLoading(true);
      setError(null);

      const [
        assetsResponse,
        portfolioResponse,
        inventoryResponse,
        purchaseRequestsResponse,
        auditEventsResponse,
      ] = await Promise.all([
        api.getAssets(),
        api.getPortfolio(demoCustomerId),
        api.getInventory(),
        api.getPurchaseRequests(),
        api.getAuditEvents(),
      ]);

      setAssets(assetsResponse);
      setPortfolio(portfolioResponse);
      setInventory(inventoryResponse);
      setPurchaseRequests(purchaseRequestsResponse);
      setAuditEvents(auditEventsResponse);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Unknown error");
    } finally {
      setIsLoading(false);
    }
  }

  async function createDemoPurchaseRequest() {
    try {
      setError(null);

      await api.createPurchaseRequest({
        customerId: demoCustomerId,
        assetId: demoAssetId,
        quantity: 2,
        requestedPrice: 12300,
        requestedBy: "Kris",
      });

      await loadDashboard();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Unknown error");
    }
  }

  useEffect(() => {
    loadDashboard();
  }, []);

  return (
    <main className="page">
      <section className="hero">
        <div>
          <p className="eyebrow">Asset Portfolio Ops</p>
          <h1>Investment operations dashboard</h1>
          <p className="heroText">
            A cloud-first full-stack demo for managing investment assets,
            portfolio value, inventory, purchase requests and audit events.
          </p>
        </div>

        <button className="primaryButton" onClick={createDemoPurchaseRequest}>
          Create demo purchase request
        </button>
      </section>

      {error && (
        <section className="errorBox">
          <strong>Something went wrong:</strong> {error}
        </section>
      )}

      {isLoading ? (
        <section className="card">
          <p>Loading dashboard...</p>
        </section>
      ) : (
        <>
          <section className="metricsGrid">
            <MetricCard
              label="Portfolio value"
              value={formatMoney(portfolio?.totalMarketValue ?? 0)}
            />
            <MetricCard
              label="Portfolio gain/loss"
              value={formatMoney(portfolio?.totalGainLoss ?? 0)}
            />
            <MetricCard
              label="Assets"
              value={assets.length.toString()}
            />
            <MetricCard
              label="Inventory units"
              value={totalInventory.toString()}
            />
          </section>

          <section className="contentGrid">
            <section className="card largeCard">
              <div className="sectionHeader">
                <div>
                  <h2>Portfolio</h2>
                  <p>{portfolio?.customerName}</p>
                </div>
              </div>

              <div className="table">
                <div className="tableRow tableHeader">
                  <span>Asset</span>
                  <span>Qty</span>
                  <span>Market value</span>
                  <span>Gain/loss</span>
                </div>

                {portfolio?.items.map((item) => (
                  <div className="tableRow" key={item.assetId}>
                    <span>
                      {item.assetName}
                      <small>{item.region} · {item.vintageYear}</small>
                    </span>
                    <span>{item.quantity}</span>
                    <span>{formatMoney(item.totalMarketValue)}</span>
                    <span>{formatMoney(item.gainLoss)}</span>
                  </div>
                ))}
              </div>
            </section>

            <section className="card">
              <div className="sectionHeader">
                <h2>Inventory</h2>
              </div>

              <div className="list">
                {inventory.map((item) => {
                  const asset = assets.find((assetItem) => assetItem.id === item.assetId);

                  return (
                    <div className="listItem" key={item.id}>
                      <strong>{asset?.name ?? "Unknown asset"}</strong>
                      <span>{item.quantityAvailable} units · {item.warehouseLocation}</span>
                    </div>
                  );
                })}
              </div>
            </section>

            <section className="card">
              <div className="sectionHeader">
                <h2>Purchase requests</h2>
              </div>

              <div className="list">
                {purchaseRequests.length === 0 ? (
                  <p>No purchase requests yet.</p>
                ) : (
                  purchaseRequests.map((request) => {
                    const asset = assets.find((assetItem) => assetItem.id === request.assetId);

                    return (
                      <div className="listItem" key={request.id}>
                        <strong>{asset?.name ?? "Unknown asset"}</strong>
                        <span>
                          {request.quantity} units · {formatMoney(request.requestedPrice)}
                        </span>
                      </div>
                    );
                  })
                )}
              </div>
            </section>

            <section className="card">
              <div className="sectionHeader">
                <h2>Audit events</h2>
              </div>

              <div className="list">
                {auditEvents.length === 0 ? (
                  <p>No audit events yet.</p>
                ) : (
                  auditEvents.map((event) => (
                    <div className="listItem" key={event.id}>
                      <strong>{event.entityType} {event.action}</strong>
                      <span>
                        {event.performedBy} · {new Date(event.timestampUtc).toLocaleString()}
                      </span>
                    </div>
                  ))
                )}
              </div>
            </section>
          </section>
        </>
      )}
    </main>
  );
}

function MetricCard({ label, value }: { label: string; value: string }) {
  return (
    <section className="metricCard">
      <span>{label}</span>
      <strong>{value}</strong>
    </section>
  );
}

function formatMoney(value: number) {
  return new Intl.NumberFormat("da-DK", {
    style: "currency",
    currency: "DKK",
    maximumFractionDigits: 0,
  }).format(value);
}

export default App;