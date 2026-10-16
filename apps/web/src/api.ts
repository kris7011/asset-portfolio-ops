const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5107";

export type RiskIndicatorSeverity = "Low" | "Medium" | "High";

export type RiskIndicator = {
    type: string;
    severity: RiskIndicatorSeverity;
    title: string;
    message: string;
};

export type PurchaseRequestStatus = 0 | 1 | 2 | 3;

export const purchaseRequestStatusLabels: Record<PurchaseRequestStatus, string> = {
    0: "Pending",
    1: "Approved",
    2: "Rejected",
    3: "Completed",
};

export type UpdatePurchaseRequestStatus = {
    performedBy: string;
};

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
    status: PurchaseRequestStatus;
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

export class ApiError extends Error {
    readonly status: number;

    constructor(message: string, status: number) {
        super(message);
        this.name = "ApiError";
        this.status = status;
    }
}

async function readErrorMessage(response: Response, method: string, path: string): Promise<string> {
    // The API returns business rule violations as { "error": "..." } with status 400.
    try {
        const body: unknown = await response.json();

        if (body && typeof body === "object" && "error" in body && typeof body.error === "string") {
            return body.error;
        }
    } catch {
        // Not a JSON body; fall back to a generic message.
    }

    return `${method} ${path} failed with status ${response.status}`;
}

async function requestJson<TResponse>(method: string, path: string, body?: unknown): Promise<TResponse> {
    const response = await fetch(`${API_BASE_URL}${path}`, {
        method,
        headers: body === undefined ? undefined : { "Content-Type": "application/json" },
        body: body === undefined ? undefined : JSON.stringify(body),
    });

    if (!response.ok) {
        throw new ApiError(await readErrorMessage(response, method, path), response.status);
    }

    return response.json() as Promise<TResponse>;
}

function getJson<T>(path: string): Promise<T> {
    return requestJson<T>("GET", path);
}

function postJson<TResponse, TBody>(path: string, body: TBody): Promise<TResponse> {
    return requestJson<TResponse>("POST", path, body);
}

function patchJson<TResponse, TBody>(path: string, body: TBody): Promise<TResponse> {
    return requestJson<TResponse>("PATCH", path, body);
}

export const api = {
    getAssets: () => getJson<Asset[]>("/api/assets"),

    getPortfolio: (customerId: string) =>
        getJson<Portfolio>(`/api/customers/${customerId}/portfolio`),

    getRiskIndicators: (customerId: string) =>
        getJson<RiskIndicator[]>(`/api/customers/${customerId}/risk-indicators`),

    getInventory: () => getJson<InventoryItem[]>("/api/inventory"),

    getPurchaseRequests: () =>
        getJson<PurchaseRequest[]>("/api/purchase-requests"),

    getAuditEvents: () => getJson<AuditEvent[]>("/api/audit-events"),

    createPurchaseRequest: (request: CreatePurchaseRequest) =>
        postJson<PurchaseRequest, CreatePurchaseRequest>("/api/purchase-requests", request),

    approvePurchaseRequest: (id: string, request: UpdatePurchaseRequestStatus) =>
        patchJson<PurchaseRequest, UpdatePurchaseRequestStatus>(
            `/api/purchase-requests/${id}/approve`,
            request
        ),

    rejectPurchaseRequest: (id: string, request: UpdatePurchaseRequestStatus) =>
        patchJson<PurchaseRequest, UpdatePurchaseRequestStatus>(
            `/api/purchase-requests/${id}/reject`,
            request
        ),

    completePurchaseRequest: (id: string, request: UpdatePurchaseRequestStatus) =>
        patchJson<PurchaseRequest, UpdatePurchaseRequestStatus>(
            `/api/purchase-requests/${id}/complete`,
            request
        ),
};