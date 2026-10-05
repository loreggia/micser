import type { DriverState, DriverStatus } from "../../../src/Web/src/shell";
import { developmentShell, expect, FakeShell, test, type Graph } from "../fixtures";

const installedDriver: DriverStatus = {
  installed: true,
  problem: null,
  installedVersion: "1.0.0.0",
  bundledVersion: "1.0.0.0",
  cableCount: 2,
  updateAvailable: false,
  cables: [
    { layout: "stereo", formatsMatch: true },
    { layout: "stereo", formatsMatch: true },
  ],
};

function driver(status: Partial<DriverStatus> | null, isBusy = false): DriverState {
  return { isBusy, status: status && { ...installedDriver, ...status } };
}

async function openSettings(graph: Graph) {
  await graph.open();
  await graph.page.getByRole("button", { name: "Settings" }).click();
  return graph.page.getByRole("dialog", { name: "Settings" });
}

test("in a browser, the shell's controls are hidden", async ({ graph }) => {
  const dialog = await openSettings(graph);

  await expect(dialog.getByText("Plugins", { exact: true })).toBeVisible();
  await expect(dialog.getByRole("button", { name: "Restart engine process" })).toHaveCount(0);
  await expect(dialog.getByRole("button", { name: "Check for updates" })).toHaveCount(0);
  await expect(dialog.getByText("Virtual audio cables")).toHaveCount(0);
});

test("a development shell shows none of its controls either", async ({ graph }) => {
  const shell = await FakeShell.install(graph.page, developmentShell);
  const dialog = await openSettings(graph);

  await expect(dialog.getByRole("button", { name: "Restart engine process" })).toHaveCount(0);
  await expect(dialog.getByRole("button", { name: "Check for updates" })).toHaveCount(0);
  await expect(dialog.getByText("Virtual audio cables")).toHaveCount(0);
  expect(await shell.messages()).toEqual([]);
});

test.describe("updates", () => {
  test("a downloaded update is installed from the toolbar", async ({ graph }) => {
    const shell = await FakeShell.install(graph.page, { ...developmentShell, canUpdate: true, pendingUpdate: "2.0.0" });
    await graph.open();

    await graph.page.getByRole("button", { name: "Update to 2.0.0" }).click();

    expect(await shell.messages()).toEqual([{ type: "installUpdate" }]);
  });

  test("the update button appears when the shell has downloaded an update", async ({ graph }) => {
    const shell = await FakeShell.install(graph.page, { ...developmentShell, canUpdate: true });
    await graph.open();
    await expect(graph.page.getByRole("button", { name: /^Update to/ })).toHaveCount(0);

    await shell.setState({ ...developmentShell, canUpdate: true, pendingUpdate: "2.0.0" });

    await expect(graph.page.getByRole("button", { name: "Update to 2.0.0" })).toBeVisible();
  });

  test("the settings check for updates and show the result", async ({ graph }) => {
    const state = { ...developmentShell, version: "1.2.0", canUpdate: true };
    const shell = await FakeShell.install(graph.page, state);
    const dialog = await openSettings(graph);
    await expect(dialog.getByText("Micser 1.2.0")).toBeVisible();

    await dialog.getByRole("button", { name: "Check for updates" }).click();
    expect(await shell.messages()).toEqual([{ type: "checkForUpdates" }]);

    await shell.setState({ ...state, isCheckingForUpdates: true });
    await expect(dialog.getByRole("button", { name: "Checking for updates…" })).toBeDisabled();

    await shell.setState(state);
    await shell.send({ type: "updateCheck", result: "upToDate" });
    await expect(dialog.getByText("Micser is up to date.")).toBeVisible();
  });

  test("the settings install a downloaded update", async ({ graph }) => {
    const shell = await FakeShell.install(graph.page, {
      ...developmentShell,
      version: "1.2.0",
      canUpdate: true,
      pendingUpdate: "2.0.0",
    });
    const dialog = await openSettings(graph);

    await dialog.getByRole("button", { name: "Restart to update to 2.0.0" }).click();

    expect(await shell.messages()).toEqual([{ type: "installUpdate" }]);
  });
});

test("the settings restart the engine process", async ({ graph }) => {
  const shell = await FakeShell.install(graph.page, { ...developmentShell, canRestartEngine: true });
  const dialog = await openSettings(graph);

  await dialog.getByRole("button", { name: "Restart engine process" }).click();

  expect(await shell.messages()).toEqual([{ type: "restartEngine" }]);
});

test.describe("virtual audio cables", () => {
  test("installs the driver with the chosen number of cables", async ({ graph }) => {
    const shell = await FakeShell.install(graph.page, {
      ...developmentShell,
      driver: driver({ installed: false, installedVersion: null, cableCount: 1, cables: [] }),
    });
    const dialog = await openSettings(graph);

    // the spin button takes typed text on Enter
    await dialog.getByRole("spinbutton", { name: "Number of cables" }).fill("3");
    await graph.page.keyboard.press("Enter");
    await dialog.getByRole("button", { name: "Install", exact: true }).click();

    expect(await shell.messages()).toEqual([{ type: "installDriver", cableCount: 3 }]);
  });

  test("changes the number of cables", async ({ graph }) => {
    const shell = await FakeShell.install(graph.page, { ...developmentShell, driver: driver({}) });
    const dialog = await openSettings(graph);
    await expect(dialog.getByText("Driver 1.0.0.0")).toBeVisible();
    const apply = dialog.getByRole("button", { name: "Apply", exact: true });
    await expect(apply).toBeDisabled();

    await dialog.getByRole("spinbutton", { name: "Number of cables" }).fill("4");
    await graph.page.keyboard.press("Enter");
    await apply.click();

    expect(await shell.messages()).toEqual([{ type: "setCableCount", cableCount: 4 }]);
  });

  test("follows the count the driver reports", async ({ graph }) => {
    const shell = await FakeShell.install(graph.page, { ...developmentShell, driver: driver({}) });
    const dialog = await openSettings(graph);

    await shell.setState({ ...developmentShell, driver: driver({ cableCount: 5 }) });

    await expect(dialog.getByRole("spinbutton", { name: "Number of cables" })).toHaveValue("5");
  });

  test("changes a cable's channel layout", async ({ graph }) => {
    const shell = await FakeShell.install(graph.page, { ...developmentShell, driver: driver({}) });
    const dialog = await openSettings(graph);

    await dialog.getByRole("combobox", { name: "Channel layout of cable 2" }).click();
    await graph.page.getByRole("option", { name: "5.1 surround" }).click();

    expect(await shell.messages()).toEqual([{ type: "setCableLayout", cable: 2, layout: "5.1" }]);
  });

  test("shows a layout that Windows hasn't applied yet", async ({ graph }) => {
    await FakeShell.install(graph.page, {
      ...developmentShell,
      driver: driver({
        cables: [
          { layout: "7.1", formatsMatch: false },
          { layout: "stereo", formatsMatch: true },
        ],
      }),
    });
    const dialog = await openSettings(graph);

    await expect(dialog.getByRole("combobox", { name: "Channel layout of cable 1" })).toHaveText("7.1 surround");
    await expect(dialog.getByText("Not applied yet; restart Windows if it asked for it.")).toHaveCount(1);
  });

  test("updates and uninstalls the driver", async ({ graph }) => {
    const shell = await FakeShell.install(graph.page, {
      ...developmentShell,
      driver: driver({ updateAvailable: true, bundledVersion: "1.1.0.0" }),
    });
    const dialog = await openSettings(graph);

    await dialog.getByRole("button", { name: "Update to 1.1.0.0" }).click();
    await dialog.getByRole("button", { name: "Uninstall" }).click();

    expect(await shell.messages()).toEqual([{ type: "updateDriver" }, { type: "uninstallDriver" }]);
  });

  test("shows a driver that doesn't run", async ({ graph }) => {
    await FakeShell.install(graph.page, { ...developmentShell, driver: driver({ problem: 10 }) });
    const dialog = await openSettings(graph);

    await expect(dialog.getByText(/doesn't run \(problem code 10\)/)).toBeVisible();
  });

  test("shows a change in progress and an unreadable status", async ({ graph }) => {
    const shell = await FakeShell.install(graph.page, { ...developmentShell, driver: driver({}, true) });
    const dialog = await openSettings(graph);
    await expect(dialog.getByText("Changing the virtual audio cables…")).toBeVisible();
    await expect(dialog.getByRole("button", { name: "Uninstall" })).toHaveCount(0);

    await shell.setState({ ...developmentShell, driver: driver(null) });

    await expect(dialog.getByText("The driver status couldn't be read.")).toBeVisible();
  });
});
