import { describe, expect, it, vi } from "vitest";
import { api, ApiError } from "./api";
import { jsonResponse } from "./test/fakeApi";

describe("api", () => {
  it("uses the backend error message for business rule violations", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => jsonResponse({ error: "Quantity must be greater than zero." }, 400)));

    const request = api.createPurchaseRequest({
      customerId: "c",
      assetId: "a",
      quantity: 0,
      requestedPrice: 100,
      requestedBy: "Kris",
    });

    await expect(request).rejects.toThrow(new ApiError("Quantity must be greater than zero.", 400));
    await expect(request).rejects.toMatchObject({ status: 400 });
  });

  it("falls back to a generic message when the error body is not JSON", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => new Response("Not found", { status: 404 })));

    await expect(api.getAssets()).rejects.toThrow("GET /api/assets failed with status 404");
  });

  it("sends JSON bodies with a content type for PATCH requests", async () => {
    const fetchMock = vi.fn(async () => jsonResponse({ id: "1", status: 1 }));
    vi.stubGlobal("fetch", fetchMock);

    await api.approvePurchaseRequest("1", { performedBy: "Kris" });

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringMatching(/\/api\/purchase-requests\/1\/approve$/),
      {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ performedBy: "Kris" }),
      },
    );
  });
});
