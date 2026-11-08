import { Dropdown, mergeClasses, Option } from "@fluentui/react-components";
import {
  formatNumber,
  InfoTip,
  ParameterKnob,
  parseNumber,
  parsePercent,
  useDefaultStyles,
  type VoiceChangerPreset,
  type WidgetProps,
} from "@micser/web-sdk";
import { useTranslation } from "../i18n";

/**
 * The pitch and formant shifts in semitones each preset applies; mix and quality stay.
 */
const voiceChangerPresets: Record<Exclude<VoiceChangerPreset, "Custom">, { pitch: number; formant: number }> = {
  MaleToFemale: { pitch: 9, formant: 3 },
  FemaleToMale: { pitch: -6, formant: -3 },
  Child: { pitch: 10, formant: 5 },
  Monster: { pitch: -12, formant: -6 },
  Chipmunk: { pitch: 12, formant: 12 },
};

const presets: VoiceChangerPreset[] = ["Custom", "MaleToFemale", "FemaleToMale", "Child", "Monster", "Chipmunk"];

export function VoiceChangerWidget({ module, setState, defaultState }: WidgetProps<"VoiceChanger">) {
  const styles = useDefaultStyles();
  const { t } = useTranslation();
  const state = module.state;
  const presetLabel = (preset: VoiceChangerPreset) => t(`voiceChanger.presets.${preset}`);
  const semitones = (value: number) =>
    t("voiceChanger.semitones", {
      value: formatNumber(value, { signDisplay: "exceptZero", maximumFractionDigits: 1 }),
    });
  const percent = (value: number) => formatNumber(value, { style: "percent", maximumFractionDigits: 0 });

  const selectPreset = (preset: VoiceChangerPreset) =>
    setState(preset === "Custom" ? { ...state, preset } : { ...state, preset, ...voiceChangerPresets[preset] });

  return (
    <div className={styles.column}>
      <div className={styles.withInfo}>
        <Dropdown
          className={mergeClasses(styles.dropdown, styles.fill)}
          size="small"
          aria-label={t("voiceChanger.preset")}
          value={presetLabel(state.preset)}
          selectedOptions={[state.preset]}
          onOptionSelect={(_, data) => selectPreset(data.optionValue as VoiceChangerPreset)}
        >
          {presets.map((preset) => (
            <Option key={preset} value={preset}>
              {presetLabel(preset)}
            </Option>
          ))}
        </Dropdown>
        <InfoTip info={t("voiceChanger.info.preset")} label={t("voiceChanger.preset")} />
      </div>
      <div className={styles.knobs}>
        <ParameterKnob
          label={t("voiceChanger.pitch")}
          info={t("voiceChanger.info.pitch")}
          value={state.pitch}
          min={-24}
          max={24}
          step={1}
          format={semitones}
          parse={parseNumber}
          defaultValue={defaultState?.pitch}
          onChange={(pitch) => setState({ ...state, preset: "Custom", pitch })}
        />
        <ParameterKnob
          label={t("voiceChanger.formant")}
          info={t("voiceChanger.info.formant")}
          value={state.formant}
          min={-12}
          max={12}
          step={0.5}
          format={semitones}
          parse={parseNumber}
          defaultValue={defaultState?.formant}
          onChange={(formant) => setState({ ...state, preset: "Custom", formant })}
        />
        <ParameterKnob
          label={t("voiceChanger.mix")}
          info={t("voiceChanger.info.mix")}
          value={state.mix}
          min={0}
          max={1}
          step={0.01}
          format={percent}
          parse={parsePercent}
          defaultValue={defaultState?.mix}
          onChange={(mix) => setState({ ...state, mix })}
        />
        <ParameterKnob
          label={t("voiceChanger.quality")}
          info={t("voiceChanger.info.quality")}
          value={state.quality}
          min={1}
          max={10}
          step={1}
          format={(v) => String(v)}
          defaultValue={defaultState?.quality}
          onChange={(quality) => setState({ ...state, quality })}
        />
      </div>
    </div>
  );
}
