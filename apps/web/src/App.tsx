import { useEffect, useMemo, useState } from "react";
import "./App.css";
import {
  api,
  purchaseRequestStatusLabels,
  type Asset,
  type InventoryItem,
  type Portfolio,
  type PurchaseRequest,
  type PurchaseRequestStatus,
  type RiskIndicator,
  type AuditEvent,
} from "./api";

const demoCustomerId = "11111111-1111-1111-1111-111111111111";
const demoAssetId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

function App() {
  const [assets, setAssets] = useState<Asset[]>([]);
  const [portfolio, setPortfolio] = useState<Portfolio | null>(null);
  const [riskIndicators, setRiskIndicators] = useState<RiskIndicator[]>([]);
  const [inventory, setInventory] = useState<InventoryItem[]>([]);
  const [purchaseRequests, setPurchaseRequests] = useState<PurchaseRequest[]>([]);
  const [auditEvents, setAuditEvents] = useState<AuditEvent[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [updatingPurchaseRequestId, setUpdatingPurchaseRequestId] = useState<string | null>(null);

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
        riskIndicatorsResponse,
        inventoryResponse,
        purchaseRequestsResponse,
        auditEventsResponse,
      ] = await Promise.all([
        api.getAssets(),
        api.getPortfolio(demoCustomerId),
        api.getRiskIndicators(demoCustomerId),
        api.getInventory(),
        api.getPurchaseRequests(),
        api.getAuditEvents(),
      ]);

      setAssets(assetsResponse);
      setPortfolio(portfolioResponse);
      setRiskIndicators(riskIndicatorsResponse);
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

  async function updatePurchaseRequestStatus(
    id: string,
    action: "approve" | "reject" | "complete"
  ) {
    try {
      setUpdatingPurchaseRequestId(id);
      setError(null);

      const request = {
        performedBy: "Kris",
      };

      if (action === "approve") {
        await api.approvePurchaseRequest(id, request);
      }

      if (action === "reject") {
        await api.rejectPurchaseRequest(id, request);
      }

      if (action === "complete") {
        await api.completePurchaseRequest(id, request);
      }

      await loadDashboard();
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : "Could not update purchase request."
      );
    } finally {
      setUpdatingPurchaseRequestId(null);
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
                <h2>Risk indicators</h2>
              </div>

              <div className="list">
                {riskIndicators.length === 0 ? (
                  <p>No risk indicators available.</p>
                ) : (
                  riskIndicators.map((indicator) => (
                    <div className="listItem riskItem" key={`${indicator.type}-${indicator.title}`}>
                      <div className="riskHeader">
                        <strong>{indicator.title}</strong>
                        <span className={`severity-badge severity-${indicator.severity.toLowerCase()}`}>
                          {indicator.severity}
                        </span>
                      </div>

                      <span>{indicator.message}</span>
                    </div>
                  ))
                )}
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
                    const statusLabel = getPurchaseRequestStatusLabel(request.status);
                    const isUpdating = updatingPurchaseRequestId === request.id;

                    return (
                      <div className="listItem" key={request.id}>
                        <strong>{asset?.name ?? "Unknown asset"}</strong>

                        <span>
                          {request.quantity} units · {formatMoney(request.requestedPrice)}
                        </span>

                        <span className={`status-badge status-${statusLabel.toLowerCase()}`}>
                          {statusLabel}
                        </span>

                        <div className="action-buttons">
                          {canApproveOrReject(request.status) && (
                            <>
                              <button
                                type="button"
                                onClick={() => updatePurchaseRequestStatus(request.id, "approve")}
                                disabled={isUpdating}
                              >
                                Approve
                              </button>

                              <button
                                type="button"
                                onClick={() => updatePurchaseRequestStatus(request.id, "reject")}
                                disabled={isUpdating}
                              >
                                Reject
                              </button>
                            </>
                          )}

                          {canComplete(request.status) && (
                            <button
                              type="button"
                              onClick={() => updatePurchaseRequestStatus(request.id, "complete")}
                              disabled={isUpdating}
                            >
                              Complete
                            </button>
                          )}

                          {!canApproveOrReject(request.status) && !canComplete(request.status) && (
                            <small>No actions available</small>
                          )}
                        </div>
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

function getPurchaseRequestStatusLabel(status: PurchaseRequestStatus) {
  return purchaseRequestStatusLabels[status] ?? "Unknown";
}

function canApproveOrReject(status: PurchaseRequestStatus) {
  return status === 0;
}

function canComplete(status: PurchaseRequestStatus) {
  return status === 1;
}

export default App;