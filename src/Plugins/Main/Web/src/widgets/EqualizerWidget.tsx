import {
  Button,
  Caption1,
  Dropdown,
  makeStyles,
  mergeClasses,
  Option,
  tokens,
  Tooltip,
} from "@fluentui/react-components";
import { AddRegular } from "@fluentui/react-icons/svg/add";
import { ArrowImportRegular } from "@fluentui/react-icons/svg/arrow-import";
import { DeleteRegular } from "@fluentui/react-icons/svg/delete";
import {
  biquadResponse,
  decibels,
  formatNumber,
  FrequencyResponse,
  hertz,
  highPass,
  InfoTip,
  highShelf,
  lowPass,
  lowShelf,
  ParameterKnob,
  parseHertz,
  peakingEq,
  useDefaultStyles,
  useSampleRate,
  type Biquad,
  type EqualizerBand,
  type EqualizerBandType,
  type WidgetProps,
} from "@micser/web-sdk";
import { useRef, useState } from "react";
import { maxBands, parseEqualizerApo } from "../equalizerApo";
import { useTranslation } from "../i18n";

const useStyles = makeStyles({
  band: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalS,
    paddingBottom: tokens.spacingVerticalS,
    borderBottom: `${tokens.strokeWidthThin} solid ${tokens.colorNeutralStroke2}`,
  },
  bandHeader: {
    display: "flex",
    alignItems: "center",
    gap: tokens.spacingHorizontalXS,
    color: tokens.colorNeutralForeground2,
  },
  bandName: {
    flexShrink: 0,
    marginInlineEnd: tokens.spacingHorizontalXS,
  },
  actions: {
    display: "flex",
    gap: tokens.spacingHorizontalS,
  },
  message: {
    color: tokens.colorNeutralForeground3,
  },
  error: {
    color: tokens.colorStatusDangerForeground1,
  },
});

const bandTypes: EqualizerBandType[] = ["Peaking", "LowShelf", "HighShelf", "LowPass", "HighPass"];

/** A band "Add band" adds, also the values Delete resets a band's knobs to. */
const newBand: EqualizerBand = { type: "Peaking", frequency: 1000, gain: 0, q: 1.41 };

/** Whether the band's gain applies: not to low-pass and high-pass bands. */
const hasGain = (type: EqualizerBandType) => type !== "LowPass" && type !== "HighPass";

function bandBiquad(sampleRate: number, band: EqualizerBand): Biquad {
  // the engine limits the frequency to below the Nyquist frequency the same way
  const frequency = Math.min(band.frequency, sampleRate * 0.45);
  switch (band.type) {
    case "LowShelf":
      return lowShelf(sampleRate, frequency, band.q, band.gain);
    case "HighShelf":
      return highShelf(sampleRate, frequency, band.q, band.gain);
    case "LowPass":
      return lowPass(sampleRate, frequency, band.q);
    case "HighPass":
      return highPass(sampleRate, frequency, band.q);
    default:
      return peakingEq(sampleRate, frequency, band.q, band.gain);
  }
}

type ImportResult = { error: true } | { error: false; bands: number; skipped: number };

export function EqualizerWidget({ module, setState, defaultState }: WidgetProps<"Equalizer">) {
  const defaultStyles = useDefaultStyles();
  const styles = useStyles();
  const { t } = useTranslation();
  const state = module.state;
  const bands = state.bands;
  const sampleRate = useSampleRate();
  const fileInput = useRef<HTMLInputElement>(null);
  const [imported, setImported] = useState<ImportResult>();
  const setBand = (index: number, band: EqualizerBand) => setState({ ...state, bands: bands.with(index, band) });
  const filters = bands.map((band) => bandBiquad(sampleRate, band));
  const typeLabel = (type: EqualizerBandType) => t(`equalizer.types.${type}`);

  const importFile = async (file: File) => {
    const result = parseEqualizerApo(await file.text());
    if (!result) {
      setImported({ error: true });
      return;
    }

    setState({ bands: result.bands, preamp: result.preamp });
    setImported({ error: false, bands: result.bands.length, skipped: result.skipped });
  };

  return (
    <div className={defaultStyles.column}>
      <FrequencyResponse
        label={t("equalizer.response")}
        response={(frequency) => state.preamp + biquadResponse(filters, sampleRate, frequency)}
        minDecibels={-24}
        maxDecibels={24}
        markers={bands.map((band) => band.frequency)}
      />
      <div className={defaultStyles.knobs}>
        <ParameterKnob
          label={t("equalizer.preamp")}
          info={t("equalizer.info.preamp")}
          value={state.preamp}
          min={-24}
          max={24}
          step={0.1}
          format={decibels}
          defaultValue={defaultState?.preamp}
          onChange={(preamp) => setState({ ...state, preamp })}
        />
      </div>
      {bands.map((band, index) => (
        <div key={index} className={styles.band}>
          <div className={styles.bandHeader}>
            <span className={styles.bandName}>{t("equalizer.band", { number: index + 1 })}</span>
            <Dropdown
              className={mergeClasses(defaultStyles.dropdown, defaultStyles.fill)}
              size="small"
              aria-label={t("equalizer.type", { number: index + 1 })}
              value={typeLabel(band.type)}
              selectedOptions={[band.type]}
              onOptionSelect={(_, data) => setBand(index, { ...band, type: data.optionValue as EqualizerBandType })}
            >
              {bandTypes.map((type) => (
                <Option key={type} value={type}>
                  {typeLabel(type)}
                </Option>
              ))}
            </Dropdown>
            <InfoTip info={t("equalizer.info.type")} label={t("equalizer.type", { number: index + 1 })} />
            <Button
              size="small"
              appearance="subtle"
              icon={<DeleteRegular />}
              aria-label={t("equalizer.removeBand", { number: index + 1 })}
              onClick={() => setState({ ...state, bands: bands.toSpliced(index, 1) })}
            />
          </div>
          <div className={defaultStyles.knobs}>
            <ParameterKnob
              label={t("equalizer.frequency")}
              info={t("equalizer.info.frequency")}
              value={band.frequency}
              min={20}
              max={20000}
              logarithmic
              format={hertz}
              parse={parseHertz}
              defaultValue={newBand.frequency}
              onChange={(frequency) => setBand(index, { ...band, frequency })}
            />
            <ParameterKnob
              label={t("equalizer.gain")}
              info={t("equalizer.info.gain")}
              value={band.gain}
              min={-24}
              max={24}
              step={0.5}
              format={decibels}
              disabled={!hasGain(band.type)}
              defaultValue={newBand.gain}
              onChange={(gain) => setBand(index, { ...band, gain })}
            />
            <ParameterKnob
              label={t("equalizer.q")}
              info={t("equalizer.info.q")}
              value={band.q}
              min={0.1}
              max={20}
              logarithmic
              format={(v) => formatNumber(v, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
              defaultValue={newBand.q}
              onChange={(q) => setBand(index, { ...band, q })}
            />
          </div>
        </div>
      ))}
      <div className={styles.actions}>
        <Button
          size="small"
          icon={<AddRegular />}
          disabled={bands.length >= maxBands}
          onClick={() => setState({ ...state, bands: [...bands, newBand] })}
        >
          {t("equalizer.addBand")}
        </Button>
        <Tooltip content={t("equalizer.importHint")} relationship="description">
          <Button size="small" icon={<ArrowImportRegular />} onClick={() => fileInput.current?.click()}>
            {t("equalizer.import")}
          </Button>
        </Tooltip>
      </div>
      {imported && (
        <Caption1 role="status" className={mergeClasses(styles.message, imported.error && styles.error)}>
          {imported.error
            ? t("equalizer.importFailed")
            : imported.skipped > 0
              ? t("equalizer.importedSkipped", {
                  imported: t("equalizer.imported", { count: imported.bands }),
                  skipped: imported.skipped,
                })
              : t("equalizer.imported", { count: imported.bands })}
        </Caption1>
      )}
      <input
        ref={fileInput}
        type="file"
        accept=".txt,text/plain"
        hidden
        aria-label={t("equalizer.import")}
        onChange={(event) => {
          const file = event.target.files?.[0];
          event.target.value = "";
          if (file) {
            void importFile(file);
          }
        }}
      />
    </div>
  );
}
