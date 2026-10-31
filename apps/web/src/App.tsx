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
import { formatMoney } from "./format";
import { PurchasePreview } from "./PurchasePreview";
import { ValuationHistory } from "./ValuationHistory";

const demoCustomerId = "11111111-1111-1111-1111-111111111111";
const demoAssetId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

type DashboardData = {
  assets: Asset[];
  portfolio: Portfolio;
  riskIndicators: RiskIndicator[];
  inventory: InventoryItem[];
  purchaseRequests: PurchaseRequest[];
  auditEvents: AuditEvent[];
};

const emptyDashboard: Omit<DashboardData, "portfolio"> & { portfolio: Portfolio | null } = {
  assets: [],
  portfolio: null,
  riskIndicators: [],
  inventory: [],
  purchaseRequests: [],
  auditEvents: [],
};

async function fetchDashboardData(): Promise<DashboardData> {
  const [assets, portfolio, riskIndicators, inventory, purchaseRequests, auditEvents] =
    await Promise.all([
      api.getAssets(),
      api.getPortfolio(demoCustomerId),
      api.getRiskIndicators(demoCustomerId),
      api.getInventory(),
      api.getPurchaseRequests(),
      api.getAuditEvents(),
    ]);

  return { assets, portfolio, riskIndicators, inventory, purchaseRequests, auditEvents };
}

function getErrorMessage(requestError: unknown, fallback: string) {
  return requestError instanceof Error ? requestError.message : fallback;
}

function App() {
  const [dashboard, setDashboard] = useState<DashboardData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isCreating, setIsCreating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [updatingPurchaseRequestId, setUpdatingPurchaseRequestId] = useState<string | null>(null);

  const { assets, portfolio, riskIndicators, inventory, purchaseRequests, auditEvents } =
    dashboard ?? emptyDashboard;

  const totalInventory = useMemo(() => {
    return inventory.reduce((total, item) => total + item.quantityAvailable, 0);
  }, [inventory]);

  // Reload data after an action without replacing the dashboard with the loading state.
  async function refreshDashboard() {
    setDashboard(await fetchDashboardData());
  }

  async function createDemoPurchaseRequest() {
    try {
      setIsCreating(true);
      setError(null);

      await api.createPurchaseRequest({
        customerId: demoCustomerId,
        assetId: demoAssetId,
        quantity: 2,
        requestedPrice: 12300,
        requestedBy: "Kris",
      });

      await refreshDashboard();
    } catch (requestError) {
      setError(getErrorMessage(requestError, "Could not create purchase request."));
    } finally {
      setIsCreating(false);
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

      await refreshDashboard();
    } catch (requestError) {
      setError(getErrorMessage(requestError, "Could not update purchase request."));
    } finally {
      setUpdatingPurchaseRequestId(null);
    }
  }

  useEffect(() => {
    let ignore = false;

    fetchDashboardData()
      .then((data) => {
        if (!ignore) {
          setDashboard(data);
        }
      })
      .catch((requestError: unknown) => {
        if (!ignore) {
          setError(getErrorMessage(requestError, "Could not load dashboard."));
        }
      })
      .finally(() => {
        if (!ignore) {
          setIsLoading(false);
        }
      });

    // Ignore late responses if the component unmounts (and in React StrictMode's double effect run).
    return () => {
      ignore = true;
    };
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

        <button
          type="button"
          className="primaryButton"
          onClick={createDemoPurchaseRequest}
          disabled={isLoading || isCreating}
        >
          {isCreating ? "Creating..." : "Create demo purchase request"}
        </button>
      </section>

      {error && (
        <section className="errorBox" role="alert">
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

            <ValuationHistory
              assets={assets}
              customerId={demoCustomerId}
              customerName={portfolio?.customerName ?? "Customer"}
            />

            <PurchasePreview
              assets={assets}
              customerId={demoCustomerId}
              performedBy="Kris"
              onRequestCreated={refreshDashboard}
            />

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