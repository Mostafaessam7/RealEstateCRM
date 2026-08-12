import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Modal } from "./Modal";

describe("Modal", () => {
  it("renders its title and children with dialog semantics", () => {
    render(
      <Modal title="Delete unit" onClose={vi.fn()}>
        <p>Are you sure?</p>
      </Modal>,
    );

    const dialog = screen.getByRole("dialog", { name: "Delete unit" });
    expect(dialog).toHaveAttribute("aria-modal", "true");
    expect(screen.getByText("Are you sure?")).toBeInTheDocument();
  });

  it("calls onClose when Escape is pressed", () => {
    const onClose = vi.fn();
    render(
      <Modal title="Test" onClose={onClose}>
        <p>Body</p>
      </Modal>,
    );

    fireEvent.keyDown(document, { key: "Escape" });

    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("calls onClose when clicking the overlay, but not when clicking inside the dialog content", async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    render(
      <Modal title="Test" onClose={onClose}>
        <p>Body content</p>
      </Modal>,
    );

    await user.click(screen.getByText("Body content"));
    expect(onClose).not.toHaveBeenCalled();

    await user.click(screen.getByRole("dialog"));
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("moves initial focus into the dialog", () => {
    render(
      <Modal title="Test" onClose={vi.fn()}>
        <button type="button">First action</button>
        <button type="button">Second action</button>
      </Modal>,
    );

    // The Close (X) button is the first focusable element in DOM order.
    expect(screen.getByRole("button", { name: "Close" })).toHaveFocus();
  });

  it("traps Tab focus within the dialog (wraps from last back to first)", async () => {
    const user = userEvent.setup();
    render(
      <Modal title="Test" onClose={vi.fn()}>
        <button type="button">Only action</button>
      </Modal>,
    );

    const closeButton = screen.getByRole("button", { name: "Close" });
    const actionButton = screen.getByRole("button", { name: "Only action" });

    expect(closeButton).toHaveFocus();
    await user.tab();
    expect(actionButton).toHaveFocus();
    await user.tab();
    // Wraps back to the first focusable element instead of escaping the dialog.
    expect(closeButton).toHaveFocus();
  });

  it("restores focus to the triggering element on unmount", () => {
    const trigger = document.createElement("button");
    trigger.textContent = "Open modal";
    document.body.appendChild(trigger);
    trigger.focus();
    expect(trigger).toHaveFocus();

    const { unmount } = render(
      <Modal title="Test" onClose={vi.fn()}>
        <p>Body</p>
      </Modal>,
    );
    expect(trigger).not.toHaveFocus();

    unmount();

    expect(trigger).toHaveFocus();
    document.body.removeChild(trigger);
  });
});
