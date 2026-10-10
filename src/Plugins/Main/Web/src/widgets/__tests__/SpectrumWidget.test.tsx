import { EngineConnection } from "@micser/web-sdk";
import { createTestQueryClient, testModule, TestProviders } from "@micser/web-sdk/testing";
import { expect, test, vi } from "vitest";
import { render } from "vitest-browser-react";
import { SpectrumWidget } from "../SpectrumWidget";

test("draws the module's spectrum while shown", async () => {
  const queryClient = createTestQueryClient();
  const connection = new EngineConnection(queryClient);
  const unsubscribe = vi.fn();
  let push: ((data: unknown) => void) | undefined;
  const subscribe = vi.spyOn(connection, "subscribe").mockImplementation((_, listener) => {
    push = listener;
    return unsubscribe;
  });

  const module = testModule("Spectrum", {}, { id: "spectrum-7" });
  const screen = await render(
    <TestProviders queryClient={queryClient} connection={connection}>
      <SpectrumWidget module={module} setState={() => {}} />
    </TestProviders>
  );

  expect(subscribe).toHaveBeenCalledWith("spectrum-7", expect.any(Function));

  const canvas = screen.container.querySelector("canvas")!;
  const stroke = vi.spyOn(CanvasRenderingContext2D.prototype, "stroke");
  push?.({ frequencyResolution: 10, magnitudes: Array.from({ length: 2400 }, (_, bin) => 1 / (bin + 1)) });

  await expect.poll(() => stroke.mock.calls.length).toBeGreaterThan(0);
  // drawn at the canvas's size
  expect(canvas.clientWidth).toBeGreaterThan(0);
  expect(Math.abs(canvas.width - canvas.clientWidth * window.devicePixelRatio)).toBeLessThanOrEqual(1);

  await screen.unmount();
  expect(unsubscribe).toHaveBeenCalled();
});

test("fills the height it's given, at least 120 px", async () => {
  const queryClient = createTestQueryClient();
  const connection = new EngineConnection(queryClient);
  vi.spyOn(connection, "subscribe").mockImplementation(() => () => {});
  const module = testModule("Spectrum", {});
  const renderIn = (height: number) =>
    render(
      <TestProviders queryClient={queryClient} connection={connection}>
        <div style={{ display: "flex", flexDirection: "column", height, width: 300 }}>
          <SpectrumWidget module={module} setState={() => {}} />
        </div>
      </TestProviders>
    );

  const tall = await renderIn(300);
  const canvas = tall.container.querySelector("canvas")!;
  expect(canvas.clientHeight).toBe(300);
  await expect.poll(() => canvas.height).toBe(Math.round(300 * window.devicePixelRatio));
  await tall.unmount();

  const low = await renderIn(50);
  expect(low.container.querySelector("canvas")!.clientHeight).toBe(120);
});
