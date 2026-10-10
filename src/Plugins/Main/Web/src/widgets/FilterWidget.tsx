import { Dropdown, Option } from "@fluentui/react-components";
import {
  biquadResponse,
  formatNumber,
  FrequencyResponse,
  hertz,
  highPass,
  lowPass,
  ParameterSlider,
  useDefaultStyles,
  useSampleRate,
  type FilterState,
  type WidgetProps,
} from "@micser/web-sdk";
import { useTranslation } from "../i18n";

const butterworthQ = Math.SQRT1_2;

/**
 * The biquads of the engine's filter: a Butterworth cascade, one per 12 dB per octave, whose last stage is scaled by the resonance.
 */
function filterStages({ type, frequency, slope, q }: FilterState, sampleRate: number) {
  const stageCount = Math.min(Math.max(Math.trunc(slope / 12), 1), 4);
  const cutoff = Math.min(frequency, sampleRate * 0.45);
  return Array.from({ length: stageCount }, (_, stage) => {
    const butterworth = 1 / (2 * Math.cos(((2 * stage + 1) * Math.PI) / (4 * stageCount)));
    const stageQ = stage === stageCount - 1 ? (butterworth * q) / butterworthQ : butterworth;
    return type === "LowPass" ? lowPass(sampleRate, cutoff, stageQ) : highPass(sampleRate, cutoff, stageQ);
  });
}

export function FilterWidget({ module, setState }: WidgetProps<"Filter">) {
  const styles = useDefaultStyles();
  const { t } = useTranslation();
  const state = module.state;
  const sampleRate = useSampleRate();
  const stages = filterStages(state, sampleRate);

  return (
    <div className={styles.column}>
      <FrequencyResponse
        label={t("filter.response")}
        response={(frequency) => biquadResponse(stages, sampleRate, frequency)}
        minDecibels={-48}
        maxDecibels={24}
      />
      <Dropdown
        className={styles.dropdown}
        size="small"
        value={state.type === "LowPass" ? t("filter.lowPass") : t("filter.highPass")}
        selectedOptions={[state.type]}
        onOptionSelect={(_, data) => setState({ ...state, type: data.optionValue as typeof state.type })}
      >
        <Option value="HighPass">{t("filter.highPass")}</Option>
        <Option value="LowPass">{t("filter.lowPass")}</Option>
      </Dropdown>
      <ParameterSlider
        label={t("filter.frequency")}
        value={state.frequency}
        min={20}
        max={20000}
        logarithmic
        format={hertz}
        onChange={(frequency) => setState({ ...state, frequency })}
      />
      <ParameterSlider
        label={t("filter.slope")}
        value={state.slope}
        min={12}
        max={48}
        step={12}
        format={(v) => t("filter.decibelsPerOctave", { value: formatNumber(v) })}
        onChange={(slope) => setState({ ...state, slope })}
      />
      <ParameterSlider
        label={t("filter.q")}
        value={state.q}
        min={0.1}
        max={10}
        logarithmic
        format={(v) => formatNumber(v, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
        onChange={(q) => setState({ ...state, q })}
      />
    </div>
  );
}
