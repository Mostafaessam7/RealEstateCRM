import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { LeadsPipelineChart } from "./LeadsPipelineChart";

describe("LeadsPipelineChart", () => {
  it("shows an empty state when every status count is zero (no leads yet)", () => {
    render(<LeadsPipelineChart byStatus={{}} />);

    expect(screen.getByText("No leads yet.")).toBeInTheDocument();
  });

  it("shows an empty state when byStatus has entries but they're all zero", () => {
    render(<LeadsPipelineChart byStatus={{ New: 0, Lost: 0 }} />);

    expect(screen.getByText("No leads yet.")).toBeInTheDocument();
  });

  it("renders the chart (not the empty state) once at least one status has leads", () => {
    render(<LeadsPipelineChart byStatus={{ New: 3, Contacted: 1 }} />);

    expect(screen.queryByText("No leads yet.")).not.toBeInTheDocument();
    expect(screen.getByText("Leads Pipeline")).toBeInTheDocument();
  });
});
