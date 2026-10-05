import {
  Button,
  Caption1,
  Dropdown,
  Field,
  Option,
  SpinButton,
  Spinner,
  Subtitle2,
  Text,
  makeStyles,
  tokens,
} from "@fluentui/react-components";
import { useState } from "react";
import { useTranslation } from "../i18n";
import { shell, type CableLayout, type DriverState } from "../shell";

const maxCables = 16;

const layouts: CableLayout[] = ["stereo", "5.1", "7.1"];

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
  layouts: {
    display: "grid",
    gridTemplateColumns: "auto 160px minmax(0, 1fr)",
    alignItems: "center",
    columnGap: tokens.spacingHorizontalM,
    rowGap: tokens.spacingVerticalXS,
  },
  layout: {
    minWidth: "160px",
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
function CableLayoutRow({
  number,
  layout,
  formatsMatch,
}: {
  number: number;
  layout: CableLayout;
  formatsMatch: boolean;
}) {
  const styles = useStyles();
  const { t } = useTranslation();

  return (
    <>
      <Text>{t("cables.cable", { number })}</Text>
      <Dropdown
        className={styles.layout}
        aria-label={t("cables.layoutOf", { number })}
        value={t(`cables.layouts.${layout}`)}
        selectedOptions={[layout]}
        onOptionSelect={(_, data) => {
          if (data.optionValue && data.optionValue !== layout) {
            shell?.setCableLayout(number, data.optionValue as CableLayout);
          }
        }}
      >
        {layouts.map((value) => (
          <Option key={value} value={value}>
            {t(`cables.layouts.${value}`)}
          </Option>
        ))}
      </Dropdown>
      {formatsMatch ? <span /> : <Caption1 className={styles.warning}>{t("cables.notApplied")}</Caption1>}
    </>
  );
}

export function VirtualCablesSettings({ driver }: VirtualCablesSettingsProps) {
  const styles = useStyles();
  const { t } = useTranslation();
  const status = driver.status;
  const [count, setCount] = useState(status?.cableCount ?? 1);
  const [reportedCount, setReportedCount] = useState(status?.cableCount);

  // follow the count the driver reports, e.g. after a change
  if (status && status.cableCount !== reportedCount) {
    setReportedCount(status.cableCount);
    setCount(status.cableCount);
  }

  const countField = (
    <Field label={t("cables.count")} className={styles.count}>
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
      <Subtitle2>{t("cables.title")}</Subtitle2>
      <Caption1 className={styles.hint}>{t("cables.description")}</Caption1>
      {driver.isBusy ? (
        <Spinner size="tiny" labelPosition="after" label={t("cables.changing")} />
      ) : !status ? (
        <Caption1 className={styles.warning}>{t("cables.statusUnreadable")}</Caption1>
      ) : !status.installed ? (
        <div className={styles.row}>
          {countField}
          <Button appearance="primary" onClick={() => shell?.installDriver(count)}>
            {t("cables.install")}
          </Button>
        </div>
      ) : (
        <>
          <Caption1>
            {t("cables.driver", { version: status.installedVersion })}
            {status.problem !== null && (
              <span className={styles.warning}>{t("cables.problem", { problem: status.problem })}</span>
            )}
          </Caption1>
          <div className={styles.row}>
            {countField}
            <Button disabled={count === status.cableCount} onClick={() => shell?.setCableCount(count)}>
              {t("cables.apply")}
            </Button>
            {status.updateAvailable && (
              <Button appearance="primary" onClick={() => shell?.updateDriver()}>
                {t("cables.updateTo", { version: status.bundledVersion })}
              </Button>
            )}
            <Button appearance="subtle" onClick={() => shell?.uninstallDriver()}>
              {t("cables.uninstall")}
            </Button>
          </div>
          {status.cables.length > 0 && (
            <>
              <div className={styles.layouts}>
                {status.cables.map((cable, index) => (
                  <CableLayoutRow
                    key={index}
                    number={index + 1}
                    layout={cable.layout}
                    formatsMatch={cable.formatsMatch}
                  />
                ))}
              </div>
              <Caption1 className={styles.hint}>{t("cables.surroundHint")}</Caption1>
            </>
          )}
        </>
      )}
    </>
  );
}
