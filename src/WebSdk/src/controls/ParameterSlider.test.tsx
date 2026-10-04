import { FluentProvider, webLightTheme } from "@fluentui/react-components";
import { expect, test, vi } from "vitest";
import { userEvent } from "vitest/browser";
import { render } from "vitest-browser-react";
import { ParameterSlider, type ParameterSliderProps } from "./ParameterSlider";

async function renderSlider(props: Partial<ParameterSliderProps>) {
  const onChange = vi.fn<(value: number) => void>();
  const screen = await render(
    <FluentProvider theme={webLightTheme}>
      <ParameterSlider label="Gain" value={0} min={-60} max={24} step={0.5} onChange={onChange} {...props} />
    </FluentProvider>
  );

  const slider = screen.getByRole("slider", { name: "Gain" });
  return { screen, slider, onChange };
}

async function press(key: string) {
  await userEvent.keyboard(`{${key}}`);
}

test("shows the label and the formatted value", async () => {
  const { screen } = await renderSlider({ value: -6, format: (v) => `${v} dB` });

  await expect.element(screen.getByText("Gain")).toBeVisible();
  await expect.element(screen.getByText("-6 dB")).toBeVisible();
});

test("shows the value with its unit without a format", async () => {
  const { screen } = await renderSlider({ value: 1.23456, unit: "dB" });

  await expect.element(screen.getByText("1.23 dB")).toBeVisible();
});

test("a keyboard step on a linear scale is exactly one step", async () => {
  const { slider, onChange } = await renderSlider({ value: 0 });

  slider.element().focus();
  await press("ArrowRight");

  expect(onChange).toHaveBeenLastCalledWith(0.5);
});

test("a logarithmic scale reaches its ends", async () => {
  const { slider, onChange } = await renderSlider({
    min: 20,
    max: 20000,
    value: 1000,
    logarithmic: true,
    step: undefined,
  });

  slider.element().focus();
  await press("End");
  expect(onChange).toHaveBeenLastCalledWith(20000);

  await press("Home");
  expect(onChange).toHaveBeenLastCalledWith(20);
});

test("a logarithmic scale puts the geometric mean in the middle", async () => {
  const { slider } = await renderSlider({ min: 20, max: 20000, value: Math.sqrt(20 * 20000), logarithmic: true });

  await expect.element(slider).toHaveValue("500");
});
