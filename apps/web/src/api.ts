const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5107";

export type Asset = {
    id: string;
    name: string;
    type: string;
    region: string;
    vintageYear: number;
    marketPrice: number;
    currency: string;
};

export type PortfolioItem = {
    assetId: string;
    assetName: string;
    region: string;
    vintageYear: number;
    quantity: number;
    averagePurchasePrice: number;
    marketPrice: number;
    totalMarketValue: number;
    gainLoss: number;
};

export type Portfolio = {
    customerId: string;
    customerName: string;
    items: PortfolioItem[];
    totalMarketValue: number;
    totalGainLoss: number;
};

export type InventoryItem = {
    id: string;
    assetId: string;
    quantityAvailable: number;
    warehouseLocation: string;
    lastUpdatedUtc: string;
};

export type PurchaseRequest = {
    id: string;
    customerId: string;
    assetId: string;
    quantity: number;
    requestedPrice: number;
    status: number;
    requestedBy: string;
    createdUtc: string;
};

export type AuditEvent = {
    id: string;
    entityId: string;
    entityType: string;
    action: string;
    performedBy: string;
    timestampUtc: string;
    metadata: Record<string, string>;
};

export type CreatePurchaseRequest = {
    customerId: string;
    assetId: string;
    quantity: number;
    requestedPrice: number;
    requestedBy: string;
};

async function getJson<T>(url: string): Promise<T> {
    const response = await fetch(`${API_BASE_URL}${url}`);

    if (!response.ok) {
        throw new Error(`Request failed: ${response.status}`);
    }

    return response.json();
}

async function postJson<TResponse, TBody>(url: string, body: TBody): Promise<TResponse> {
    const response = await fetch(`${API_BASE_URL}${url}`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(body),
    });

    if (!response.ok) {
        throw new Error(`Request failed: ${response.status}`);
    }

    return response.json();
}

export const api = {
    getAssets: () => getJson<Asset[]>("/api/assets"),

    getPortfolio: (customerId: string) =>
        getJson<Portfolio>(`/api/customers/${customerId}/portfolio`),

    getInventory: () => getJson<InventoryItem[]>("/api/inventory"),

    getPurchaseRequests: () =>
        getJson<PurchaseRequest[]>("/api/purchase-requests"),

    getAuditEvents: () => getJson<AuditEvent[]>("/api/audit-events"),

    createPurchaseRequest: (request: CreatePurchaseRequest) =>
        postJson<PurchaseRequest, CreatePurchaseRequest>("/api/purchase-requests", request),
};