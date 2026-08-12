import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { RecentActivity, timeAgo } from "./RecentActivity";
import { useLeads } from "../leads/leadsApi";
import { useDeals } from "../deals/dealsApi";

vi.mock("../leads/leadsApi", () => ({ useLeads: vi.fn() }));
vi.mock("../deals/dealsApi", () => ({ useDeals: vi.fn() }));

const mockUseLeads = vi.mocked(useLeads);
const mockUseDeals = vi.mocked(useDeals);

function renderWithRouter() {
  return render(
    <MemoryRouter>
      <RecentActivity />
    </MemoryRouter>,
  );
}

describe("timeAgo", () => {
  it("returns 'just now' for a timestamp seconds ago", () => {
    expect(timeAgo(new Date().toISOString())).toBe("just now");
  });

  it("returns minutes for a timestamp under an hour old", () => {
    expect(timeAgo(new Date(Date.now() - 5 * 60_000).toISOString())).toBe("5m ago");
  });

  it("returns hours for a timestamp under a day old", () => {
    expect(timeAgo(new Date(Date.now() - 3 * 60 * 60_000).toISOString())).toBe("3h ago");
  });

  it("returns days for a timestamp under a month old", () => {
    expect(timeAgo(new Date(Date.now() - 2 * 24 * 60 * 60_000).toISOString())).toBe("2d ago");
  });

  it("falls back to a calendar date for anything a month or older", () => {
    const oldDate = new Date(Date.now() - 60 * 24 * 60 * 60_000);
    expect(timeAgo(oldDate.toISOString())).toBe(oldDate.toLocaleDateString());
  });
});

describe("RecentActivity", () => {
  it("shows a loading state while either query is in flight", () => {
    mockUseLeads.mockReturnValue({ data: undefined, isLoading: true, isError: false } as never);
    mockUseDeals.mockReturnValue({ data: undefined, isLoading: false, isError: false } as never);

    renderWithRouter();

    expect(screen.getByText("Loading…")).toBeInTheDocument();
  });

  it("shows an error state when either query fails", () => {
    mockUseLeads.mockReturnValue({ data: undefined, isLoading: false, isError: true } as never);
    mockUseDeals.mockReturnValue({ data: undefined, isLoading: false, isError: false } as never);

    renderWithRouter();

    expect(screen.getByText("Couldn't load recent activity.")).toBeInTheDocument();
  });

  it("shows an empty state when there are no leads or deals", () => {
    mockUseLeads.mockReturnValue({ data: { items: [] }, isLoading: false, isError: false } as never);
    mockUseDeals.mockReturnValue({ data: { items: [] }, isLoading: false, isError: false } as never);

    renderWithRouter();

    expect(screen.getByText(/Nothing yet/)).toBeInTheDocument();
  });

  it("merges leads and deals into one feed, newest first, capped at 6", () => {
    const leads = Array.from({ length: 5 }, (_, i) => ({
      id: `lead-${i}`,
      fullName: `Lead ${i}`,
      status: "New",
      createdAt: new Date(Date.now() - i * 60_000).toISOString(),
    }));
    const deals = Array.from({ length: 5 }, (_, i) => ({
      id: `deal-${i}`,
      dealValue: 100000 + i,
      status: "Pending",
      createdAt: new Date(Date.now() - (i + 0.5) * 60_000).toISOString(),
    }));

    mockUseLeads.mockReturnValue({ data: { items: leads }, isLoading: false, isError: false } as never);
    mockUseDeals.mockReturnValue({ data: { items: deals }, isLoading: false, isError: false } as never);

    renderWithRouter();

    // The most recent lead (index 0, no offset) must be the very first item in the merged feed.
    const feedItems = screen.getAllByRole("listitem");
    expect(feedItems).toHaveLength(6);
    expect(feedItems[0]).toHaveTextContent("New lead");
    expect(feedItems[0]).toHaveTextContent("Lead 0");
  });

  it("links each lead item to its detail page", () => {
    mockUseLeads.mockReturnValue({
      data: { items: [{ id: "lead-1", fullName: "Amara Osei", status: "New", createdAt: new Date().toISOString() }] },
      isLoading: false,
      isError: false,
    } as never);
    mockUseDeals.mockReturnValue({ data: { items: [] }, isLoading: false, isError: false } as never);

    renderWithRouter();

    expect(screen.getByRole("link")).toHaveAttribute("href", "/leads/lead-1");
  });
});
