import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import App from "./App";
import { jsonResponse, purchaseRequest, stubApi } from "./test/fakeApi";

function purchaseRequestsCard() {
  return screen.getByRole("heading", { name: "Purchase requests" }).closest("section.card") as HTMLElement;
}

describe("App", () => {
  it("renders portfolio, inventory and risk indicators from the API", async () => {
    stubApi();

    render(<App />);

    expect(screen.getByText("Loading dashboard...")).toBeInTheDocument();
    expect(await screen.findByText("Emma Jensen")).toBeInTheDocument();
    expect(screen.getByText("High portfolio concentration")).toBeInTheDocument();
    expect(screen.getByText("High")).toHaveClass("severity-high");
    expect(screen.getByText("18 units · AAL-WH-01")).toBeInTheDocument();
    expect(screen.getByText("No purchase requests yet.")).toBeInTheDocument();
  });

  it("only offers the actions allowed for each purchase request status", async () => {
    stubApi({
      purchaseRequests: [
        purchaseRequest("pending-1", 0),
        purchaseRequest("approved-1", 1),
        purchaseRequest("completed-1", 3),
      ],
    });

    render(<App />);

    const card = await waitFor(purchaseRequestsCard);
    expect(within(card).getAllByRole("button", { name: "Approve" })).toHaveLength(1);
    expect(within(card).getAllByRole("button", { name: "Reject" })).toHaveLength(1);
    expect(within(card).getAllByRole("button", { name: "Complete" })).toHaveLength(1);
    expect(within(card).getByText("No actions available")).toBeInTheDocument();
  });

  it("approves a pending request and refreshes the list without unmounting the dashboard", async () => {
    let approved = false;
    let releaseRefresh: () => void = () => {};
    const refreshGate = new Promise<void>((resolve) => {
      releaseRefresh = resolve;
    });
    const { fetchMock } = stubApi({
      purchaseRequests: [purchaseRequest("pending-1", 0)],
      routes: [
        (method, path, body) => {
          if (method === "PATCH" && path === "/api/purchase-requests/pending-1/approve") {
            expect(body).toEqual({ performedBy: "Kris" });
            approved = true;
            return jsonResponse(purchaseRequest("pending-1", 1));
          }
          if (approved && method === "GET" && path === "/api/purchase-requests") {
            // Hold the refresh open so the UI can be inspected while it is in flight.
            return refreshGate.then(() => jsonResponse([purchaseRequest("pending-1", 1)]));
          }
        },
      ],
    });
    const user = userEvent.setup();

    render(<App />);
    const approveButton = await screen.findByRole("button", { name: "Approve" });
    const customerName = screen.getByText("Emma Jensen");
    await user.click(approveButton);

    await waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith(
        expect.stringContaining("/api/purchase-requests/pending-1/approve"),
        expect.objectContaining({ method: "PATCH" }),
      ),
    );
    // While the refresh is in flight the dashboard stays visible instead of the loading state.
    expect(screen.queryByText("Loading dashboard...")).not.toBeInTheDocument();
    expect(customerName).toBeInTheDocument();

    releaseRefresh();

    expect(await screen.findByRole("button", { name: "Complete" })).toBeInTheDocument();
    expect(screen.getByText("Approved")).toHaveClass("status-approved");
    expect(customerName).toBeInTheDocument();
  });

  it("shows the backend's error message when a status change is rejected", async () => {
    stubApi({
      purchaseRequests: [purchaseRequest("pending-1", 0)],
      routes: [
        (method, path) => {
          if (method === "PATCH" && path.endsWith("/reject")) {
            return jsonResponse({ error: "Cannot change purchase request status from Approved to Rejected." }, 400);
          }
        },
      ],
    });
    const user = userEvent.setup();

    render(<App />);
    await user.click(await screen.findByRole("button", { name: "Reject" }));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Cannot change purchase request status from Approved to Rejected.",
    );
  });

  it("disables the create button while a purchase request is being created", async () => {
    let resolveCreate: (response: Response) => void = () => {};
    const createResponse = new Promise<Response>((resolve) => {
      resolveCreate = resolve;
    });
    const { fetchMock } = stubApi({
      routes: [
        (method, path) => {
          if (method === "POST" && path === "/api/purchase-requests") {
            return createResponse;
          }
        },
      ],
    });
    const user = userEvent.setup();

    render(<App />);
    const createButton = await screen.findByRole("button", { name: "Create demo purchase request" });
    await waitFor(() => expect(createButton).toBeEnabled());
    await user.click(createButton);

    expect(screen.getByRole("button", { name: "Creating..." })).toBeDisabled();
    resolveCreate(jsonResponse(purchaseRequest("new-1", 0), 201));
    await waitFor(() =>
      expect(screen.getByRole("button", { name: "Create demo purchase request" })).toBeEnabled(),
    );
    expect(fetchMock.mock.calls.filter(([, init]) => init?.method === "POST")).toHaveLength(1);
  });

  it("shows an error when the dashboard cannot be loaded", async () => {
    stubApi({
      routes: [
        (_method, path) => {
          if (path === "/api/inventory") {
            return new Response("Service unavailable", { status: 503 });
          }
        },
      ],
    });

    render(<App />);

    expect(await screen.findByRole("alert")).toHaveTextContent("GET /api/inventory failed with status 503");
    expect(screen.queryByText("Loading dashboard...")).not.toBeInTheDocument();
  });
});
