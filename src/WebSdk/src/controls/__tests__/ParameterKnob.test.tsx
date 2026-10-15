import { FluentProvider, webLightTheme } from "@fluentui/react-components";
import { useState } from "react";
import { expect, test, vi } from "vitest";
import { userEvent } from "vitest/browser";
import { render } from "vitest-browser-react";
import { parseHertz } from "../../lib/labels";
import { knobDragDistance, ParameterKnob, type ParameterKnobProps } from "../ParameterKnob";

/** A knob that keeps its value like a widget, so drags and edits see their own changes. */
function StatefulKnob({ onChange, value: initial, ...props }: ParameterKnobProps) {
  const [value, setValue] = useState(initial);
  return (
    <ParameterKnob
      {...props}
      value={value}
      onChange={(v) => {
        setValue(v);
        onChange(v);
      }}
    />
  );
}

async function renderKnob(props: Partial<ParameterKnobProps>) {
  const onChange = vi.fn<(value: number) => void>();
  const screen = await render(
    <FluentProvider theme={webLightTheme}>
      <StatefulKnob label="Gain" value={0} min={-60} max={24} step={0.5} onChange={onChange} {...props} />
    </FluentProvider>
  );

  const knob = screen.getByRole("slider", { name: "Gain" });
  return { screen, knob, onChange };
}

async function press(key: string) {
  await userEvent.keyboard(`{${key}}`);
}

/** Drags the knob vertically: positive distances go up. */
function drag(knob: Element, distances: number[], shiftKey = false) {
  let y = 500;
  knob.dispatchEvent(new PointerEvent("pointerdown", { bubbles: true, button: 0, clientY: y }));
  for (const distance of distances) {
    y -= distance;
    window.dispatchEvent(new PointerEvent("pointermove", { clientY: y, shiftKey }));
  }

  window.dispatchEvent(new PointerEvent("pointerup", { clientY: y }));
}

test("shows the label and the formatted value", async () => {
  const { screen, knob } = await renderKnob({ value: -6, format: (v) => `${v} dB` });

  await expect.element(screen.getByText("Gain")).toBeVisible();
  await expect.element(screen.getByText("-6 dB")).toBeVisible();
  await expect.element(knob).toHaveAttribute("aria-valuetext", "-6 dB");
});

test("shows the value with its unit without a format", async () => {
  const { screen } = await renderKnob({ value: 1.23456, unit: "dB" });

  await expect.element(screen.getByText("1.23 dB")).toBeVisible();
});

test("a keyboard step on a linear scale is exactly one step", async () => {
  const { knob, onChange } = await renderKnob({ value: 0 });

  knob.element().focus();
  await press("ArrowRight");
  expect(onChange).toHaveBeenLastCalledWith(0.5);

  await press("ArrowDown");
  expect(onChange).toHaveBeenLastCalledWith(0);
});

test("a logarithmic scale reaches its ends", async () => {
  const { knob, onChange } = await renderKnob({ min: 20, max: 20000, value: 1000, logarithmic: true, step: undefined });

  knob.element().focus();
  await press("End");
  expect(onChange).toHaveBeenLastCalledWith(20000);

  await press("Home");
  expect(onChange).toHaveBeenLastCalledWith(20);
});

test("dragging up half the drag distance reaches the middle of the scale", async () => {
  const { knob, onChange } = await renderKnob({ min: 20, max: 20000, value: 20, logarithmic: true, step: undefined });

  drag(knob.element(), [knobDragDistance / 4, knobDragDistance / 4]);

  expect(onChange.mock.lastCall?.[0]).toBeCloseTo(Math.sqrt(20 * 20000), 6);
});

test("dragging down lowers the value and stops at the minimum", async () => {
  const { knob, onChange } = await renderKnob({ value: 0 });

  drag(knob.element(), [-knobDragDistance * 2]);

  expect(onChange).toHaveBeenLastCalledWith(-60);
});

test("Shift makes a drag ten times finer", async () => {
  const { knob, onChange } = await renderKnob({ min: 0, max: 100, step: 1, value: 0 });

  drag(knob.element(), [knobDragDistance / 2], true);

  expect(onChange).toHaveBeenLastCalledWith(5);
});

test("a typed value is applied with Enter", async () => {
  const { screen, knob, onChange } = await renderKnob({
    min: 20,
    max: 20000,
    value: 1000,
    logarithmic: true,
    parse: parseHertz,
  });

  await knob.dblClick();
  const input = screen.getByRole("textbox", { name: "Gain value" });
  await expect.element(input).toHaveFocus();
  await userEvent.keyboard("2,5k{Enter}");

  expect(onChange).toHaveBeenLastCalledWith(2500);
  await expect.element(input).not.toBeInTheDocument();
  await expect.element(knob).toHaveFocus();
});

test("a typed value is applied when the input loses the focus", async () => {
  const { screen, onChange } = await renderKnob({ value: 0, unit: "dB" });

  await screen.getByText("0 dB").dblClick();
  await userEvent.keyboard("-12");
  (screen.getByRole("textbox").element() as HTMLElement).blur();

  expect(onChange).toHaveBeenLastCalledWith(-12);
  await expect.element(screen.getByRole("textbox")).not.toBeInTheDocument();
});

test("Escape cancels a typed value", async () => {
  const { screen, knob, onChange } = await renderKnob({ value: 0 });

  knob.element().focus();
  await press("Enter");
  await userEvent.keyboard("10{Escape}");

  expect(onChange).not.toHaveBeenCalled();
  await expect.element(screen.getByRole("textbox")).not.toBeInTheDocument();
  await expect.element(knob).toHaveFocus();
});

test("a typed value is clamped to the range and snapped to a step", async () => {
  const { knob, onChange } = await renderKnob({ value: 0 });

  await knob.dblClick();
  await userEvent.keyboard("100{Enter}");
  expect(onChange).toHaveBeenLastCalledWith(24);

  await knob.dblClick();
  await userEvent.keyboard("1.3{Enter}");
  expect(onChange).toHaveBeenLastCalledWith(1.5);
});

test("Enter keeps the input open on text without a number", async () => {
  const { screen, onChange } = await renderKnob({ value: 0 });

  await screen.getByRole("slider").dblClick();
  await userEvent.keyboard("loud{Enter}");

  await expect.element(screen.getByRole("textbox")).toHaveAttribute("aria-invalid", "true");
  expect(onChange).not.toHaveBeenCalled();
});

test("explains the parameter in the popover of an info button after the label", async () => {
  const { screen, knob } = await renderKnob({ info: "How much louder the audio gets." });

  await expect.element(knob).toBeVisible();
  await screen.getByRole("button", { name: "Gain information" }).click();

  await expect.element(screen.getByText("How much louder the audio gets.")).toBeVisible();
});

test("clicking the label or the value focuses the knob", async () => {
  const { screen, knob } = await renderKnob({ value: 0, unit: "dB" });

  await screen.getByText("Gain").click();
  await expect.element(knob).toHaveFocus();

  (knob.element() as HTMLElement).blur();
  await screen.getByText("0 dB").click();
  await expect.element(knob).toHaveFocus();
});

test("a disabled knob can't be focused, dragged or edited", async () => {
  const { screen, knob, onChange } = await renderKnob({ value: 0, disabled: true });

  await expect.element(knob).toHaveAttribute("aria-disabled", "true");
  await expect.element(knob).not.toHaveAttribute("tabindex");
  drag(knob.element(), [knobDragDistance]);
  knob.element().dispatchEvent(new MouseEvent("dblclick", { bubbles: true }));

  expect(onChange).not.toHaveBeenCalled();
  await expect.element(screen.getByRole("textbox")).not.toBeInTheDocument();
});
