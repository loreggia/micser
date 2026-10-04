import { EngineConnection } from "@micser/web-sdk";
import { createTestQueryClient, testModule, TestProviders } from "@micser/web-sdk/testing";
import { expect, test, vi } from "vitest";
import { render } from "vitest-browser-react";
import { SpectrumWidget } from "./SpectrumWidget";

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
  expect(canvas.width).toBe(280 * window.devicePixelRatio);

  await screen.unmount();
  expect(unsubscribe).toHaveBeenCalled();
});
