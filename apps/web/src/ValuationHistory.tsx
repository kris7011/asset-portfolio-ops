import { useEffect, useState } from "react";
import { api, type Asset } from "./api";
import { formatMoney } from "./format";

type Series = {
  title: string;
  points: { date: string; value: number }[];
  isDemonstrationData: boolean;
  note: string;
  summary: { label: string; value: string }[];
};

const PORTFOLIO_OPTION = "portfolio";

const WIDTH = 900;
const HEIGHT = 260;
const MARGIN = { top: 12, right: 12, bottom: 28, left: 64 };

type Props = {
  assets: Asset[];
  customerId: string;
  customerName: string;
};

/**
 * Price history of one asset, or the customer's portfolio valued at
 * historical prices. The seed series is synthetic and labelled as such.
 */
export function ValuationHistory({ assets, customerId, customerName }: Props) {
  const [selection, setSelection] = useState(PORTFOLIO_OPTION);
  const [series, setSeries] = useState<Series | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let ignore = false;

    const load = selection === PORTFOLIO_OPTION
      ? api.getPortfolioValuationHistory(customerId).then((history): Series => ({
          title: `${history.customerName}'s portfolio`,
          points: history.points.map((point) => ({ date: point.date, value: point.totalValue })),
          isDemonstrationData: history.isDemonstrationData,
          note: history.basis,
          summary: summarize(history.points.map((point) => point.totalValue)),
        }))
      : api.getAssetValuations(selection).then((history): Series => ({
          title: history.assetName,
          points: history.points.map((point) => ({ date: point.date, value: point.price })),
          isDemonstrationData: history.isDemonstrationData,
          note: `Price per unit. Source: ${history.sources.join(", ")}`,
          summary: summarize(history.points.map((point) => point.price)),
        }));

    load
      .then((loaded) => {
        if (!ignore) {
          setSeries(loaded);
          setError(null);
        }
      })
      .catch((requestError: unknown) => {
        if (!ignore) {
          setError(requestError instanceof Error ? requestError.message : "Could not load valuations.");
        }
      });

    return () => {
      ignore = true;
    };
  }, [selection, customerId]);

  return (
    <section className="card wideCard" aria-labelledby="valuation-heading">
      <div className="sectionHeader">
        <div>
          <h2 id="valuation-heading">Valuation history</h2>
          <p>{series?.title ?? "Loading…"}</p>
        </div>
        <label className="fieldLabel">
          Show
          <select value={selection} onChange={(event) => setSelection(event.target.value)}>
            <option value={PORTFOLIO_OPTION}>{customerName}'s portfolio</option>
            {assets.map((asset) => (
              <option key={asset.id} value={asset.id}>{asset.name}</option>
            ))}
          </select>
        </label>
      </div>

      {error && <p role="alert" className="inlineError">{error}</p>}

      {series && (
        <>
          {series.isDemonstrationData && (
            <p className="demoBadge">
              Demonstration data: a synthetic monthly series, not real market prices.
            </p>
          )}

          <dl className="summaryRow">
            {series.summary.map((item) => (
              <div key={item.label}>
                <dt>{item.label}</dt>
                <dd>{item.value}</dd>
              </div>
            ))}
          </dl>

          <LineChart points={series.points} label={`${series.title}, value over time`} />
          <p className="chartNote">{series.note}</p>
        </>
      )}
    </section>
  );
}

function summarize(values: number[]): { label: string; value: string }[] {
  if (values.length === 0) {
    return [{ label: "Valuations", value: "none" }];
  }

  const first = values[0];
  const latest = values[values.length - 1];
  const change = first === 0 ? 0 : ((latest - first) / first) * 100;

  return [
    { label: "First", value: formatMoney(first) },
    { label: "Latest", value: formatMoney(latest) },
    { label: "Change", value: `${change >= 0 ? "+" : ""}${change.toFixed(1)} %` },
    { label: "Low / high", value: `${formatMoney(Math.min(...values))} / ${formatMoney(Math.max(...values))}` },
  ];
}

function niceTicks(min: number, max: number): number[] {
  const span = Math.max(max - min, 1);
  const rough = span / 4;
  const magnitude = 10 ** Math.floor(Math.log10(rough));
  const step = [1, 2, 2.5, 5, 10].map((factor) => factor * magnitude).find((candidate) => candidate >= rough) ?? rough;
  const ticks: number[] = [];

  // The axis always covers the whole series.
  const last = Math.ceil(max / step) * step;

  for (let value = Math.floor(min / step) * step; value <= last + step / 1000; value += step) {
    ticks.push(value);
  }

  return ticks;
}

function LineChart({ points, label }: { points: { date: string; value: number }[]; label: string }) {
  const [hover, setHover] = useState<number | null>(null);

  if (points.length === 0) {
    return <p>No valuations in this period.</p>;
  }

  const values = points.map((point) => point.value);
  const ticks = niceTicks(Math.min(...values), Math.max(...values));
  const yMin = ticks[0];
  const yMax = ticks[ticks.length - 1];
  const innerWidth = WIDTH - MARGIN.left - MARGIN.right;
  const innerHeight = HEIGHT - MARGIN.top - MARGIN.bottom;
  const x = (index: number) => MARGIN.left + (points.length === 1 ? innerWidth / 2 : (index / (points.length - 1)) * innerWidth);
  const y = (value: number) => MARGIN.top + innerHeight - ((value - yMin) / (yMax - yMin || 1)) * innerHeight;
  const path = points.map((point, index) => `${index ? "L" : "M"}${x(index).toFixed(1)},${y(point.value).toFixed(1)}`).join("");
  const active = hover === null ? null : points[hover];

  return (
    <div className="chartWrap">
      <svg viewBox={`0 0 ${WIDTH} ${HEIGHT}`} role="img" aria-label={label} className="lineChart">
        {ticks.map((tick) => (
          <g key={tick}>
            <line className="gridline" x1={MARGIN.left} x2={WIDTH - MARGIN.right} y1={y(tick)} y2={y(tick)} />
            <text className="axisLabel" x={MARGIN.left - 8} y={y(tick) + 4} textAnchor="end">
              {Math.round(tick).toLocaleString("da-DK")}
            </text>
          </g>
        ))}
        {points.map((point, index) =>
          index % 6 === 0 || index === points.length - 1 ? (
            <text
              key={point.date}
              className="axisLabel"
              x={x(index)}
              y={HEIGHT - 8}
              textAnchor={index === 0 ? "start" : index === points.length - 1 ? "end" : "middle"}
            >
              {point.date.slice(0, 7)}
            </text>
          ) : null,
        )}
        <path d={path} className="seriesLine" />
        {active && hover !== null && (
          <>
            <line className="crosshair" x1={x(hover)} x2={x(hover)} y1={MARGIN.top} y2={HEIGHT - MARGIN.bottom} />
            <circle className="seriesMarker" cx={x(hover)} cy={y(active.value)} r={5} />
          </>
        )}
        {points.map((point, index) => (
          <rect
            key={point.date}
            className="hitArea"
            x={x(index) - innerWidth / Math.max(points.length - 1, 1) / 2}
            y={MARGIN.top}
            width={innerWidth / Math.max(points.length - 1, 1)}
            height={innerHeight}
            onMouseEnter={() => setHover(index)}
            onMouseLeave={() => setHover(null)}
          />
        ))}
      </svg>
      <p className="chartReadout" aria-live="polite">
        {active ? `${active.date}: ${formatMoney(active.value)}` : "Hover the chart to read a value."}
      </p>
    </div>
  );
}
