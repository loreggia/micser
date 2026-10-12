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
import { CableLayout, getUpdateCablesMutationOptions, type CableDto, type VirtualCablesDto } from "@micser/web-sdk";
import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { useTranslation } from "../i18n";
import { useNotifyError } from "../notifications";
import { shell, type CableLayout as DriverCableLayout, type DriverState } from "../shell";

const maxCables = 16;

const layouts: DriverCableLayout[] = ["stereo", "5.1", "7.1"];

/** The engine's layouts and the driver's (which name the translations), in the same order. */
const engineLayouts: Record<DriverCableLayout, CableLayout> = {
  stereo: CableLayout.Stereo,
  "5.1": CableLayout.Surround51,
  "7.1": CableLayout.Surround71,
};

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

function CableLayoutRow({
  number,
  layout,
  disabled,
  warning,
  onChange,
}: {
  number: number;
  layout: DriverCableLayout;
  disabled?: boolean;
  /** Shown next to the layout, e.g. that it isn't applied yet. */
  warning?: string;
  onChange: (layout: DriverCableLayout) => void;
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
        disabled={disabled}
        onOptionSelect={(_, data) => {
          if (data.optionValue && data.optionValue !== layout) {
            onChange(data.optionValue as DriverCableLayout);
          }
        }}
      >
        {layouts.map((value) => (
          <Option key={value} value={value}>
            {t(`cables.layouts.${value}`)}
          </Option>
        ))}
      </Dropdown>
      {warning ? <Caption1 className={styles.warning}>{warning}</Caption1> : <span />}
    </>
  );
}

/** The number of cables, kept while it's edited and reset when the reported count changes, e.g. after a change. */
function useCount(reported: number | undefined, fallback: number) {
  const [count, setCount] = useState(reported ?? fallback);
  const [reportedCount, setReportedCount] = useState(reported);

  if (reported !== undefined && reported !== reportedCount) {
    setReportedCount(reported);
    setCount(reported);
  }

  return [count, setCount] as const;
}

function CountField({
  count,
  min,
  disabled,
  onChange,
}: {
  count: number;
  min: number;
  disabled?: boolean;
  onChange: (count: number) => void;
}) {
  const styles = useStyles();
  const { t } = useTranslation();

  return (
    <Field label={t("cables.count")} className={styles.count}>
      <SpinButton
        min={min}
        max={maxCables}
        value={count}
        disabled={disabled}
        onChange={(_, data) => {
          const value = data.value ?? Number(data.displayValue);
          if (Number.isInteger(value)) {
            onChange(Math.min(Math.max(value, min), maxCables));
          }
        }}
      />
    </Field>
  );
}

export interface VirtualCablesSettingsProps {
  driver: DriverState;
}

/**
 * Installs, updates and removes the virtual audio cable driver (Windows) and sets the number of cables, through the desktop shell. Each
 * change asks for administrator rights and stops the audio engine meanwhile.
 */
export function VirtualCablesSettings({ driver }: VirtualCablesSettingsProps) {
  const styles = useStyles();
  const { t } = useTranslation();
  const status = driver.status;
  const [count, setCount] = useCount(status?.cableCount, 1);

  const countField = <CountField count={count} min={1} disabled={driver.isBusy} onChange={setCount} />;

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
                    warning={cable.formatsMatch ? undefined : t("cables.notApplied")}
                    onChange={(layout) => shell?.setCableLayout(index + 1, layout)}
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

export interface EngineCablesSettingsProps {
  cables: VirtualCablesDto;
}

/**
 * The virtual audio cables the engine creates itself (Linux, as PipeWire devices): the number of cables and each one's layout. Changes
 * apply right away, without interrupting other audio.
 */
export function EngineCablesSettings({ cables }: EngineCablesSettingsProps) {
  const styles = useStyles();
  const { t } = useTranslation();
  const notifyError = useNotifyError();
  const [count, setCount] = useCount(cables.cables.length, 0);
  const update = useMutation({
    ...getUpdateCablesMutationOptions(),
    onError: (error) => notifyError(t("cables.changeFailed"), error),
  });

  const send = (changed: CableDto[]) => update.mutate({ data: { cables: changed } });
  const toDriverLayout = (layout: CableLayout) => layouts.find((value) => engineLayouts[value] === layout) ?? "stereo";

  return (
    <>
      <Subtitle2>{t("cables.title")}</Subtitle2>
      <Caption1 className={styles.hint}>{t("cables.engineDescription")}</Caption1>
      <div className={styles.row}>
        <CountField count={count} min={0} disabled={update.isPending} onChange={setCount} />
        <Button
          disabled={count === cables.cables.length || update.isPending}
          onClick={() =>
            send([
              ...cables.cables.slice(0, count),
              ...Array.from({ length: Math.max(count - cables.cables.length, 0) }, () => ({
                layout: CableLayout.Stereo,
              })),
            ])
          }
        >
          {t("cables.apply")}
        </Button>
        {update.isPending && <Spinner size="tiny" />}
      </div>
      {cables.cables.length > 0 && (
        <>
          <div className={styles.layouts}>
            {cables.cables.map((cable, index) => (
              <CableLayoutRow
                key={index}
                number={index + 1}
                layout={toDriverLayout(cable.layout)}
                disabled={update.isPending}
                onChange={(layout) =>
                  send(cables.cables.map((c, i) => (i === index ? { layout: engineLayouts[layout] } : c)))
                }
              />
            ))}
          </div>
          <Caption1 className={styles.hint}>{t("cables.engineSurroundHint")}</Caption1>
        </>
      )}
    </>
  );
}
