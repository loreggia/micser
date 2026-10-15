import { Button, FluentProvider, webLightTheme } from "@fluentui/react-components";
import { expect, test } from "vitest";
import { render } from "vitest-browser-react";
import { InfoTip } from "../InfoTip";
import { ParameterLabel } from "../ParameterLabel";
import { ShowHelpContext } from "../showHelp";

async function renderInfo(showHelp: boolean) {
  return render(
    <FluentProvider theme={webLightTheme}>
      <ShowHelpContext.Provider value={showHelp}>
        <ParameterLabel info="Where compression starts.">Threshold</ParameterLabel>
        <Button>Mode</Button>
        <InfoTip info="Which channel goes where." label="Mode" />
      </ShowHelpContext.Provider>
    </FluentProvider>
  );
}

test("an info tip explains a control in a popover", async () => {
  const screen = await renderInfo(true);

  await screen.getByRole("button", { name: "About Mode" }).click();

  await expect.element(screen.getByText("Which channel goes where.")).toBeVisible();
});

test("while help is hidden, labels have no info buttons and info tips show nothing", async () => {
  const screen = await renderInfo(false);

  await expect.element(screen.getByText("Threshold")).toBeVisible();
  await expect.element(screen.getByRole("button", { name: "Threshold information" })).not.toBeInTheDocument();
  await expect.element(screen.getByRole("button", { name: "About Mode" })).not.toBeInTheDocument();
});
