import { useState, type FormEvent } from "react";
import {
  api,
  type Asset,
  type ConcentrationSnapshot,
  type PurchasePreview as PreviewResult,
} from "./api";
import { formatMoney, formatPercent } from "./format";

// Fixed categorical order: an asset keeps its colour wherever it appears.
const SERIES_COLORS = ["#2a78d6", "#eb6834", "#1baf7a", "#eda100", "#e87ba4"];

type Props = {
  assets: Asset[];
  customerId: string;
  performedBy: string;
  onRequestCreated: () => Promise<void>;
};

type FormState = {
  assetId: string;
  quantity: string;
  requestedPrice: string;
};

/**
 * Shows what a purchase would do to the portfolio before anything is saved.
 * Previewing only reads; creating the purchase request is a separate,
 * explicit action that uses exactly the previewed values.
 */
export function PurchasePreview({ assets, customerId, performedBy, onRequestCreated }: Props) {
  const [form, setForm] = useState<FormState>({
    assetId: assets[0]?.id ?? "",
    quantity: "2",
    requestedPrice: "",
  });
  const [preview, setPreview] = useState<PreviewResult | null>(null);
  const [previewedForm, setPreviewedForm] = useState<FormState | null>(null);
  const [isBusy, setIsBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [created, setCreated] = useState<string | null>(null);

  const isCurrent = preview !== null && previewedForm !== null &&
    previewedForm.assetId === form.assetId &&
    previewedForm.quantity === form.quantity &&
    previewedForm.requestedPrice === form.requestedPrice;

  function update(field: keyof FormState, value: string) {
    setForm((current) => ({ ...current, [field]: value }));
    setCreated(null);
  }

  async function runPreview(event: FormEvent) {
    event.preventDefault();
    setIsBusy(true);
    setError(null);
    setCreated(null);

    try {
      const result = await api.getPurchasePreview({
        customerId,
        assetId: form.assetId,
        quantity: Number(form.quantity),
        requestedPrice: form.requestedPrice === "" ? undefined : Number(form.requestedPrice),
      });

      setPreview(result);
      setPreviewedForm(form);
    } catch (requestError) {
      setPreview(null);
      setError(requestError instanceof Error ? requestError.message : "Could not preview the purchase.");
    } finally {
      setIsBusy(false);
    }
  }

  async function createRequest() {
    if (!preview || !isCurrent) {
      return;
    }

    setIsBusy(true);
    setError(null);

    try {
      await api.createPurchaseRequest({
        customerId,
        assetId: preview.asset.assetId,
        quantity: preview.quantity,
        requestedPrice: preview.pricePerUnit,
        requestedBy: performedBy,
      });

      setCreated(`Purchase request created for ${preview.quantity} × ${preview.asset.assetName}. It is now Pending.`);
      setPreview(null);
      setPreviewedForm(null);
      await onRequestCreated();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Could not create the purchase request.");
    } finally {
      setIsBusy(false);
    }
  }

  const colorFor = (assetId: string) =>
    SERIES_COLORS[Math.max(0, assets.findIndex((asset) => asset.id === assetId)) % SERIES_COLORS.length];

  return (
    <section className="card wideCard" aria-labelledby="preview-heading">
      <div className="sectionHeader">
        <div>
          <h2 id="preview-heading">Purchase preview</h2>
          <p>See the effect on portfolio concentration before creating a request.</p>
        </div>
      </div>

      <form className="previewForm" onSubmit={runPreview}>
        <label className="fieldLabel">
          Asset
          <select value={form.assetId} onChange={(event) => update("assetId", event.target.value)}>
            {assets.map((asset) => (
              <option key={asset.id} value={asset.id}>{asset.name}</option>
            ))}
          </select>
        </label>
        <label className="fieldLabel">
          Quantity
          <input
            type="number"
            min={1}
            max={10000}
            value={form.quantity}
            onChange={(event) => update("quantity", event.target.value)}
          />
        </label>
        <label className="fieldLabel">
          Price per unit (optional)
          <input
            type="number"
            min={1}
            placeholder="Market price"
            value={form.requestedPrice}
            onChange={(event) => update("requestedPrice", event.target.value)}
          />
        </label>
        <button type="submit" className="secondaryButton" disabled={isBusy || form.assetId === ""}>
          Preview purchase
        </button>
      </form>

      {error && <p role="alert" className="inlineError">{error}</p>}
      {created && <p role="status" className="successNote">{created}</p>}

      {preview && (
        <div className="previewResult">
          <p className="previewNotice" role="note">
            <strong>Preview only.</strong> {preview.notice.replace(/^Preview only\.\s*/, "")}
          </p>

          <dl className="summaryRow">
            <div>
              <dt>Purchase</dt>
              <dd>{preview.quantity} × {preview.asset.assetName}</dd>
            </div>
            <div>
              <dt>Estimated cost</dt>
              <dd>{formatMoney(preview.estimatedCost)} <small>({preview.priceSource === "Market" ? "market price" : "your price"})</small></dd>
            </div>
            <div>
              <dt>Inventory</dt>
              <dd>{preview.inventory.quantityAvailable} available{preview.inventory.isSufficient ? "" : " — not enough"}</dd>
            </div>
            <div>
              <dt>Largest position</dt>
              <dd>
                {formatPercent(preview.before.largestSharePercent)} → {formatPercent(preview.after.largestSharePercent)}{" "}
                <small>({preview.largestShareChangePercentagePoints >= 0 ? "+" : ""}{preview.largestShareChangePercentagePoints.toFixed(2)} pp)</small>
              </dd>
            </div>
          </dl>

          <AllocationBar label="Before" snapshot={preview.before} colorFor={colorFor} />
          <AllocationBar label="After" snapshot={preview.after} colorFor={colorFor} />

          <ul className="legend" aria-label="Assets">
            {preview.after.positions.map((position) => (
              <li key={position.assetId}>
                <i style={{ background: colorFor(position.assetId) }} />
                {position.assetName}
              </li>
            ))}
          </ul>

          {preview.warnings.length > 0 && (
            <ul className="warningList">
              {preview.warnings.map((warning) => (
                <li key={warning.code} className={`severity-${warning.severity.toLowerCase()}-border`}>
                  <span className={`severity-badge severity-${warning.severity.toLowerCase()}`}>{warning.severity}</span>
                  {warning.message}
                </li>
              ))}
            </ul>
          )}

          <div className="previewActions">
            <button
              type="button"
              className="primaryButton darkButton"
              onClick={createRequest}
              disabled={isBusy || !isCurrent}
            >
              Create purchase request from this preview
            </button>
            {!isCurrent && <small>The form changed. Preview again before creating a request.</small>}
          </div>
        </div>
      )}
    </section>
  );
}

function AllocationBar({
  label,
  snapshot,
  colorFor,
}: {
  label: string;
  snapshot: ConcentrationSnapshot;
  colorFor: (assetId: string) => string;
}) {
  const levelClass = snapshot.level === "High" ? "high" : snapshot.level === "Medium" ? "medium" : "low";

  return (
    <div className="allocationRow">
      <div className="allocationHeader">
        <strong>{label}</strong>
        <span>{formatMoney(snapshot.totalMarketValue)}</span>
        <span className={`severity-badge severity-${levelClass}`}>
          Concentration: {snapshot.level === "None" ? "below 60 %" : snapshot.level}
        </span>
      </div>
      <div className="allocationBar" role="img" aria-label={`${label}: ${snapshot.positions
        .map((position) => `${position.assetName} ${formatPercent(position.sharePercent)}`)
        .join(", ")}`}>
        {snapshot.positions.map((position) => (
          <span
            key={position.assetId}
            className="allocationSegment"
            title={`${position.assetName}: ${position.quantity} units, ${formatMoney(position.marketValue)} (${formatPercent(position.sharePercent)})`}
            style={{ width: `${position.sharePercent}%`, background: colorFor(position.assetId) }}
          >
            {position.sharePercent >= 14 ? formatPercent(position.sharePercent) : ""}
          </span>
        ))}
      </div>
    </div>
  );
}
