import { CableLayout, type UpdateCablesRequest, type VirtualCablesDto } from "@micser/web-sdk";
import { createTestQueryClient, TestProviders } from "@micser/web-sdk/testing";
import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";
import { page, userEvent } from "vitest/browser";
import { render } from "vitest-browser-react";
import { EngineCablesSettings } from "../VirtualCablesSettings";

let requests: UpdateCablesRequest[];

beforeEach(() => {
  requests = [];
  const fetch = window.fetch.bind(window);
  vi.spyOn(window, "fetch").mockImplementation(async (input, init) => {
    if (String(input) !== "/api/cables" || init?.method !== "PUT") {
      return fetch(input, init);
    }

    const request = JSON.parse(String(init.body)) as UpdateCablesRequest;
    requests.push(request);
    return Response.json({ isSupported: true, cables: request.cables } satisfies VirtualCablesDto);
  });
});

afterEach(() => {
  vi.restoreAllMocks();
});

async function renderSettings(cables: VirtualCablesDto["cables"]) {
  return render(
    <TestProviders queryClient={createTestQueryClient()}>
      <EngineCablesSettings cables={{ isSupported: true, cables }} />
    </TestProviders>
  );
}

describe("EngineCablesSettings", () => {
  test("shows each cable's layout", async () => {
    await renderSettings([{ layout: CableLayout.Stereo }, { layout: CableLayout.Surround71 }]);

    await expect.element(page.getByRole("spinbutton", { name: "Number of cables" })).toHaveValue("2");
    await expect.element(page.getByRole("combobox", { name: "Channel layout of cable 1" })).toHaveTextContent("Stereo");
    await expect
      .element(page.getByRole("combobox", { name: "Channel layout of cable 2" }))
      .toHaveTextContent("7.1 surround");
    await expect.element(page.getByRole("button", { name: "Apply" })).toBeDisabled();
  });

  test("adds stereo cables and keeps the others", async () => {
    await renderSettings([{ layout: CableLayout.Surround51 }]);

    await page.getByRole("spinbutton", { name: "Number of cables" }).click();
    await userEvent.keyboard("{ArrowUp}{ArrowUp}");
    await page.getByRole("button", { name: "Apply" }).click();

    await expect
      .poll(() => requests)
      .toEqual([
        {
          cables: [{ layout: CableLayout.Surround51 }, { layout: CableLayout.Stereo }, { layout: CableLayout.Stereo }],
        },
      ]);
  });

  test("removes cables from the end", async () => {
    await renderSettings([{ layout: CableLayout.Stereo }, { layout: CableLayout.Surround71 }]);

    await page.getByRole("spinbutton", { name: "Number of cables" }).click();
    await userEvent.keyboard("{ArrowDown}");
    await page.getByRole("button", { name: "Apply" }).click();

    await expect.poll(() => requests).toEqual([{ cables: [{ layout: CableLayout.Stereo }] }]);
  });

  test("changes a cable's layout right away", async () => {
    await renderSettings([{ layout: CableLayout.Stereo }, { layout: CableLayout.Stereo }]);

    await page.getByRole("combobox", { name: "Channel layout of cable 2" }).click();
    await page.getByRole("option", { name: "5.1 surround" }).click();

    await expect
      .poll(() => requests)
      .toEqual([{ cables: [{ layout: CableLayout.Stereo }, { layout: CableLayout.Surround51 }] }]);
  });
});
