import { vi } from "vitest";
import type {
  Asset,
  AuditEvent,
  InventoryItem,
  Portfolio,
  PurchaseRequest,
  PurchaseRequestStatus,
  RiskIndicator,
} from "../api";

export const customerId = "11111111-1111-1111-1111-111111111111";
export const bordeauxId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

export const assets: Asset[] = [
  {
    id: bordeauxId,
    name: "Bordeaux Premier Cru 2016",
    type: "FineWine",
    region: "Bordeaux",
    vintageYear: 2016,
    marketPrice: 12500,
    currency: "DKK",
  },
];

export const portfolio: Portfolio = {
  customerId,
  customerName: "Emma Jensen",
  items: [
    {
      assetId: bordeauxId,
      assetName: "Bordeaux Premier Cru 2016",
      region: "Bordeaux",
      vintageYear: 2016,
      quantity: 6,
      averagePurchasePrice: 9800,
      marketPrice: 12500,
      totalMarketValue: 75000,
      gainLoss: 16200,
    },
  ],
  totalMarketValue: 75000,
  totalGainLoss: 16200,
};

export const riskIndicators: RiskIndicator[] = [
  {
    type: "HighConcentration",
    severity: "High",
    title: "High portfolio concentration",
    message: "Bordeaux Premier Cru 2016 represents 100% of the total portfolio value.",
  },
];

export const inventory: InventoryItem[] = [
  {
    id: "66666666-6666-6666-6666-666666666666",
    assetId: bordeauxId,
    quantityAvailable: 18,
    warehouseLocation: "AAL-WH-01",
    lastUpdatedUtc: "2026-06-14T08:00:00Z",
  },
];

export function purchaseRequest(id: string, status: PurchaseRequestStatus): PurchaseRequest {
  return {
    id,
    customerId,
    assetId: bordeauxId,
    quantity: 2,
    requestedPrice: 12300,
    status,
    requestedBy: "Kris",
    createdUtc: "2026-06-15T10:00:00Z",
  };
}

type Route = (method: string, path: string, body: unknown) => Response | Promise<Response> | undefined;

export function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

/**
 * Stubs global fetch with an in-memory version of the API.
 * `state.purchaseRequests` and `state.auditEvents` can be mutated by custom routes.
 */
export function stubApi(options: { purchaseRequests?: PurchaseRequest[]; routes?: Route[] } = {}) {
  const state = {
    purchaseRequests: options.purchaseRequests ?? [],
    auditEvents: [] as AuditEvent[],
  };

  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = new URL(input.toString());
    const method = init?.method ?? "GET";
    const body = init?.body ? JSON.parse(init.body.toString()) : undefined;

    for (const route of options.routes ?? []) {
      const response = route(method, url.pathname, body);
      if (response) {
        return response;
      }
    }

    if (method === "GET") {
      switch (url.pathname) {
        case "/api/assets":
          return jsonResponse(assets);
        case `/api/customers/${customerId}/portfolio`:
          return jsonResponse(portfolio);
        case `/api/customers/${customerId}/risk-indicators`:
          return jsonResponse(riskIndicators);
        case "/api/inventory":
          return jsonResponse(inventory);
        case "/api/purchase-requests":
          return jsonResponse(state.purchaseRequests);
        case "/api/audit-events":
          return jsonResponse(state.auditEvents);
      }
    }

    return jsonResponse({ error: `No fake route for ${method} ${url.pathname}` }, 404);
  });

  vi.stubGlobal("fetch", fetchMock);

  return { fetchMock, state };
}
