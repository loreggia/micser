import { FluentProvider, webLightTheme } from "@fluentui/react-components";
import { expect, test } from "vitest";
import { render } from "vitest-browser-react";
import { FrequencyResponse, type FrequencyResponseProps } from "../FrequencyResponse";

async function renderGraph(props: Partial<FrequencyResponseProps>) {
  const screen = await render(
    <FluentProvider theme={webLightTheme}>
      <FrequencyResponse label="Response" response={() => 0} minDecibels={-24} maxDecibels={24} {...props} />
    </FluentProvider>
  );

  const graph = screen.getByRole("img", { name: "Response" });
  return { screen, graph };
}

test("shows the frequency and dB grid", async () => {
  const { screen, graph } = await renderGraph({});

  await expect.element(graph).toBeVisible();
  await expect.element(screen.getByText("1 kHz", { exact: true })).toBeInTheDocument();
  await expect.element(screen.getByText("+12", { exact: true })).toBeInTheDocument();
  await expect.element(screen.getByText("-12", { exact: true })).toBeInTheDocument();
});

test("draws a flat response on the 0 dB line", async () => {
  const { graph } = await renderGraph({ height: 100 });

  const path = graph.element().querySelector("path")!.getAttribute("d")!;
  const ys = [...path.matchAll(/[ML][\d.]+,([\d.]+)/g)].map((match) => Number(match[1]));
  expect(ys.length).toBeGreaterThan(100);
  expect(new Set(ys)).toEqual(new Set([50]));
});

test("clamps values outside the dB range to its edge", async () => {
  const { graph } = await renderGraph({ response: (f) => (f < 1000 ? 100 : -100), height: 100 });

  const path = graph.element().querySelector("path")!.getAttribute("d")!;
  const ys = [...path.matchAll(/[ML][\d.]+,([\d.]+)/g)].map((match) => Number(match[1]));
  expect(new Set(ys)).toEqual(new Set([0, 100]));
});

test("marks frequencies on the curve", async () => {
  const { graph } = await renderGraph({ markers: [100, 1000], response: () => 12, height: 100 });

  const circles = graph.element().querySelectorAll("circle");
  expect(circles).toHaveLength(2);
  expect(circles[0].getAttribute("cy")).toBe("25");
});
