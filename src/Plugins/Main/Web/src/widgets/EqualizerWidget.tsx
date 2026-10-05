import { Button, makeStyles, tokens } from "@fluentui/react-components";
import { AddRegular, DeleteRegular } from "@fluentui/react-icons";
import {
  decibels,
  formatNumber,
  hertz,
  ParameterSlider,
  useDefaultStyles,
  type EqualizerBand,
  type WidgetProps,
} from "@micser/web-sdk";
import { useTranslation } from "../i18n";

const useStyles = makeStyles({
  band: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalXS,
    paddingBottom: tokens.spacingVerticalS,
    borderBottom: `${tokens.strokeWidthThin} solid ${tokens.colorNeutralStroke2}`,
  },
  bandHeader: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    color: tokens.colorNeutralForeground2,
  },
});

const maxBands = 32;

export function EqualizerWidget({ module, setState }: WidgetProps<"Equalizer">) {
  const defaultStyles = useDefaultStyles();
  const styles = useStyles();
  const { t } = useTranslation();
  const bands = module.state.bands;
  const setBand = (index: number, band: EqualizerBand) => setState({ bands: bands.with(index, band) });

  return (
    <div className={defaultStyles.column}>
      {bands.map((band, index) => (
        <div key={index} className={styles.band}>
          <div className={styles.bandHeader}>
            <span>{t("equalizer.band", { number: index + 1 })}</span>
            <Button
              size="small"
              appearance="subtle"
              icon={<DeleteRegular />}
              aria-label={t("equalizer.removeBand", { number: index + 1 })}
              onClick={() => setState({ bands: bands.toSpliced(index, 1) })}
            />
          </div>
          <ParameterSlider
            label={t("equalizer.frequency")}
            value={band.frequency}
            min={20}
            max={20000}
            logarithmic
            format={hertz}
            onChange={(frequency) => setBand(index, { ...band, frequency })}
          />
          <ParameterSlider
            label={t("equalizer.gain")}
            value={band.gain}
            min={-24}
            max={24}
            step={0.5}
            format={decibels}
            onChange={(gain) => setBand(index, { ...band, gain })}
          />
          <ParameterSlider
            label={t("equalizer.q")}
            value={band.q}
            min={0.1}
            max={20}
            logarithmic
            format={(v) => formatNumber(v, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
            onChange={(q) => setBand(index, { ...band, q })}
          />
        </div>
      ))}
      <Button
        size="small"
        icon={<AddRegular />}
        disabled={bands.length >= maxBands}
        onClick={() => setState({ bands: [...bands, { frequency: 1000, gain: 0, q: 1.41 }] })}
      >
        {t("equalizer.addBand")}
      </Button>
    </div>
  );
}
