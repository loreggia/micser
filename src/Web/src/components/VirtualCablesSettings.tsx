import {
  Button,
  Caption1,
  Field,
  SpinButton,
  Spinner,
  Subtitle2,
  makeStyles,
  tokens,
} from "@fluentui/react-components";
import { useState } from "react";
import { shell, type DriverState } from "../shell";

const maxCables = 16;

const useStyles = makeStyles({
  row: {
    display: "flex",
    alignItems: "flex-end",
    flexWrap: "wrap",
    gap: tokens.spacingHorizontalM,
  },
  count: {
    width: "120px",
  },
  hint: {
    color: tokens.colorNeutralForeground3,
  },
  warning: {
    color: tokens.colorPaletteRedForeground1,
  },
});

export interface VirtualCablesSettingsProps {
  driver: DriverState;
}

/**
 * Installs, updates and removes the virtual audio cable driver and sets the number of cables, through the desktop shell. Each change
 * asks for administrator rights and stops the audio engine meanwhile.
 */
export function VirtualCablesSettings({ driver }: VirtualCablesSettingsProps) {
  const styles = useStyles();
  const status = driver.status;
  const [count, setCount] = useState(status?.cableCount ?? 1);
  const [reportedCount, setReportedCount] = useState(status?.cableCount);

  // follow the count the driver reports, e.g. after a change
  if (status && status.cableCount !== reportedCount) {
    setReportedCount(status.cableCount);
    setCount(status.cableCount);
  }

  const countField = (
    <Field label="Number of cables" className={styles.count}>
      <SpinButton
        min={1}
        max={maxCables}
        value={count}
        disabled={driver.isBusy}
        onChange={(_, data) => {
          const value = data.value ?? Number(data.displayValue);
          if (Number.isInteger(value)) {
            setCount(Math.min(Math.max(value, 1), maxCables));
          }
        }}
      />
    </Field>
  );

  return (
    <>
      <Subtitle2>Virtual audio cables</Subtitle2>
      <Caption1 className={styles.hint}>
        Each cable is a playback device ("Cable N Input") whose sound comes out of a recording device ("Cable N
        Output"), e.g. to use Micser's output as a microphone in other apps. Changes ask for administrator rights and
        briefly stop the audio.
      </Caption1>
      {driver.isBusy ? (
        <Spinner size="tiny" labelPosition="after" label="Changing the virtual audio cables…" />
      ) : !status ? (
        <Caption1 className={styles.warning}>The driver status couldn't be read.</Caption1>
      ) : !status.installed ? (
        <div className={styles.row}>
          {countField}
          <Button appearance="primary" onClick={() => shell?.installDriver(count)}>
            Install
          </Button>
        </div>
      ) : (
        <>
          <Caption1>
            Driver {status.installedVersion}
            {status.problem !== null && (
              <span className={styles.warning}> doesn't run (problem code {status.problem}); try reinstalling it.</span>
            )}
          </Caption1>
          <div className={styles.row}>
            {countField}
            <Button disabled={count === status.cableCount} onClick={() => shell?.setCableCount(count)}>
              Apply
            </Button>
            {status.updateAvailable && (
              <Button appearance="primary" onClick={() => shell?.updateDriver()}>
                Update to {status.bundledVersion}
              </Button>
            )}
            <Button appearance="subtle" onClick={() => shell?.uninstallDriver()}>
              Uninstall
            </Button>
          </div>
        </>
      )}
    </>
  );
}
