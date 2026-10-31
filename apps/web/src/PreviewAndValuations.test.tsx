import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import App from "./App";
import type { PurchasePreview } from "./api";
import { bordeauxId, customerId, jsonResponse, purchaseRequest, stubApi } from "./test/fakeApi";

const preview: PurchasePreview = {
  isPreview: true,
  notice: "Preview only. Nothing has been saved: no purchase request, holding, inventory change or audit event was created.",
  customerId,
  customerName: "Emma Jensen",
  asset: { assetId: bordeauxId, assetName: "Bordeaux Premier Cru 2016", marketPrice: 12500, currency: "DKK" },
  quantity: 2,
  pricePerUnit: 12500,
  priceSource: "Market",
  estimatedCost: 25000,
  inventory: { quantityAvailable: 18, isSufficient: true },
  pendingRequests: { count: 0, exposure: 0 },
  before: {
    totalMarketValue: 101700,
    positions: [
      { assetId: bordeauxId, assetName: "Bordeaux Premier Cru 2016", quantity: 6, marketValue: 75000, sharePercent: 73.75 },
      { assetId: "burgundy", assetName: "Burgundy Grand Cru 2019", quantity: 3, marketValue: 26700, sharePercent: 26.25 },
    ],
    largestAssetName: "Bordeaux Premier Cru 2016",
    largestSharePercent: 73.75,
    level: "High",
  },
  after: {
    totalMarketValue: 126700,
    positions: [
      { assetId: bordeauxId, assetName: "Bordeaux Premier Cru 2016", quantity: 8, marketValue: 100000, sharePercent: 78.93 },
      { assetId: "burgundy", assetName: "Burgundy Grand Cru 2019", quantity: 3, marketValue: 26700, sharePercent: 21.07 },
    ],
    largestAssetName: "Bordeaux Premier Cru 2016",
    largestSharePercent: 78.93,
    level: "High",
  },
  largestShareChangePercentagePoints: 5.18,
  warnings: [
    {
      code: "ConcentrationRemains",
      severity: "High",
      message: "Concentration stays High: Bordeaux Premier Cru 2016 would be 78.93% of the portfolio (now 73.75%).",
    },
  ],
};

function previewCard() {
  return screen.getByRole("heading", { name: "Purchase preview" }).closest("section.card") as HTMLElement;
}

describe("Valuation history", () => {
  it("shows the portfolio series labelled as demonstration data, and switches to an asset", async () => {
    const { fetchMock } = stubApi();
    const user = userEvent.setup();

    render(<App />);

    expect(await screen.findByText(/Demonstration data: a synthetic monthly series/)).toBeInTheDocument();
    expect(screen.getByRole("img", { name: "Emma Jensen's portfolio, value over time" })).toBeInTheDocument();
    expect(screen.getByText("Current holdings valued at each historical price.")).toBeInTheDocument();

    await user.selectOptions(screen.getByRole("combobox", { name: "Show" }), bordeauxId);

    expect(await screen.findByRole("img", { name: "Bordeaux Premier Cru 2016, value over time" })).toBeInTheDocument();
    expect(screen.getByText("+4.2 %")).toBeInTheDocument();
    expect(fetchMock.mock.calls.some(([input]) => input.toString().endsWith(`/api/assets/${bordeauxId}/valuations`))).toBe(true);
  });
});

describe("Purchase preview", () => {
  it("previews with a read-only GET and shows before, after and warnings without creating anything", async () => {
    const { fetchMock } = stubApi({
      routes: [
        (method, path) =>
          method === "GET" && path === `/api/customers/${customerId}/purchase-preview` ? jsonResponse(preview) : undefined,
      ],
    });
    const user = userEvent.setup();

    render(<App />);

    const card = await waitFor(previewCard);
    await user.click(within(card).getByRole("button", { name: "Preview purchase" }));

    expect(await within(card).findByRole("note")).toHaveTextContent("Preview only. Nothing has been saved");
    expect(within(card).getByRole("img", { name: /^Before: Bordeaux Premier Cru 2016 73.8 %/ })).toBeInTheDocument();
    expect(within(card).getByRole("img", { name: /^After: Bordeaux Premier Cru 2016 78.9 %/ })).toBeInTheDocument();
    expect(within(card).getByText(/Concentration stays High/)).toBeInTheDocument();
    expect(within(card).getByText("73.8 % → 78.9 %")).toBeInTheDocument();

    const previewCall = fetchMock.mock.calls.find(([input]) => input.toString().includes("purchase-preview"));
    expect(previewCall?.[1]?.method ?? "GET").toBe("GET");
    expect(new URL(previewCall![0].toString()).searchParams.get("quantity")).toBe("2");
    expect(fetchMock.mock.calls.filter(([, init]) => init?.method === "POST")).toHaveLength(0);
  });

  it("creates the purchase request only on the explicit action, with the previewed values", async () => {
    const { fetchMock, state } = stubApi({
      routes: [
        (method, path) =>
          method === "GET" && path === `/api/customers/${customerId}/purchase-preview` ? jsonResponse(preview) : undefined,
        (method, path, body) => {
          if (method === "POST" && path === "/api/purchase-requests") {
            expect(body).toEqual({
              customerId,
              assetId: bordeauxId,
              quantity: 2,
              requestedPrice: 12500,
              requestedBy: "Kris",
            });
            state.purchaseRequests = [purchaseRequest("new-1", 0)];
            return jsonResponse(purchaseRequest("new-1", 0), 201);
          }
          return undefined;
        },
      ],
    });
    const user = userEvent.setup();

    render(<App />);

    const card = await waitFor(previewCard);
    await user.click(within(card).getByRole("button", { name: "Preview purchase" }));
    await user.click(await within(card).findByRole("button", { name: "Create purchase request from this preview" }));

    expect(await within(card).findByRole("status")).toHaveTextContent("Purchase request created for 2 × Bordeaux Premier Cru 2016");
    expect(fetchMock.mock.calls.filter(([, init]) => init?.method === "POST")).toHaveLength(1);
  });

  it("does not allow creating a request from a stale preview", async () => {
    stubApi({
      routes: [
        (method, path) =>
          method === "GET" && path === `/api/customers/${customerId}/purchase-preview` ? jsonResponse(preview) : undefined,
      ],
    });
    const user = userEvent.setup();

    render(<App />);

    const card = await waitFor(previewCard);
    await user.click(within(card).getByRole("button", { name: "Preview purchase" }));
    await within(card).findByRole("note");

    const quantity = within(card).getByRole("spinbutton", { name: "Quantity" });
    await user.clear(quantity);
    await user.type(quantity, "5");

    expect(within(card).getByRole("button", { name: "Create purchase request from this preview" })).toBeDisabled();
    expect(within(card).getByText("The form changed. Preview again before creating a request.")).toBeInTheDocument();
  });

  it("shows the API's validation message", async () => {
    stubApi({
      routes: [
        (method, path) =>
          method === "GET" && path === `/api/customers/${customerId}/purchase-preview`
            ? jsonResponse({ error: "Quantity must be between 1 and 10000." }, 400)
            : undefined,
      ],
    });
    const user = userEvent.setup();

    render(<App />);

    const card = await waitFor(previewCard);
    await user.click(within(card).getByRole("button", { name: "Preview purchase" }));

    expect(await within(card).findByRole("alert")).toHaveTextContent("Quantity must be between 1 and 10000.");
  });
});
