import { Dropdown, mergeClasses, Option } from "@fluentui/react-components";
import {
  formatNumber,
  InfoTip,
  ParameterKnob,
  parseNumber,
  parsePercent,
  useDefaultStyles,
  type VoiceChangerPreset,
  type VoiceChangerState,
  type VoiceEffect,
  type WidgetProps,
} from "@micser/web-sdk";
import { useTranslation } from "../i18n";

type PresetValues = Pick<VoiceChangerState, "pitch" | "formant" | "breathiness" | "wobble" | "effect">;

const plain = { breathiness: 0, wobble: 0, effect: "None" } as const;

/**
 * What each preset sets (pitch and formant in semitones); mix and quality stay.
 */
const voiceChangerPresets: Record<Exclude<VoiceChangerPreset, "Custom">, PresetValues> = {
  MaleToFemale: { ...plain, pitch: 9, formant: 3 },
  FemaleToMale: { ...plain, pitch: -6, formant: -3 },
  Child: { ...plain, pitch: 10, formant: 5 },
  Old: { pitch: -2, formant: -3, breathiness: 0.9, wobble: 0.8, effect: "None" },
  Monster: { ...plain, pitch: -12, formant: -6 },
  Chipmunk: { ...plain, pitch: 12, formant: 12 },
  Robot: { ...plain, pitch: 2, formant: -2, effect: "Robot" },
  Whisper: { ...plain, pitch: 0, formant: 0, effect: "Whisper" },
  Alien: { pitch: 3, formant: 4, breathiness: 0, wobble: 0.3, effect: "Alien" },
  Radio: { ...plain, pitch: 0, formant: 0, effect: "Radio" },
};

const presets: VoiceChangerPreset[] = [
  "Custom",
  "MaleToFemale",
  "FemaleToMale",
  "Child",
  "Old",
  "Monster",
  "Chipmunk",
  "Robot",
  "Whisper",
  "Alien",
  "Radio",
];

const effects: VoiceEffect[] = ["None", "Robot", "Whisper", "Alien", "Radio"];

export function VoiceChangerWidget({ module, setState, defaultState }: WidgetProps<"VoiceChanger">) {
  const styles = useDefaultStyles();
  const { t } = useTranslation();
  const state = module.state;
  const presetLabel = (preset: VoiceChangerPreset) => t(`voiceChanger.presets.${preset}`);
  const effectLabel = (effect: VoiceEffect) => t(`voiceChanger.effects.${effect}`);
  const semitones = (value: number) =>
    t("voiceChanger.semitones", {
      value: formatNumber(value, { signDisplay: "exceptZero", maximumFractionDigits: 1 }),
    });
  const percent = (value: number) => formatNumber(value, { style: "percent", maximumFractionDigits: 0 });

  const selectPreset = (preset: VoiceChangerPreset) =>
    setState(preset === "Custom" ? { ...state, preset } : { ...state, preset, ...voiceChangerPresets[preset] });

  // a change of what a preset sets makes the setting custom
  const setCustom = (values: Partial<PresetValues>) => setState({ ...state, preset: "Custom", ...values });

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
      <div className={styles.withInfo}>
        <Dropdown
          className={mergeClasses(styles.dropdown, styles.fill)}
          size="small"
          aria-label={t("voiceChanger.effect")}
          value={effectLabel(state.effect)}
          selectedOptions={[state.effect]}
          onOptionSelect={(_, data) => setCustom({ effect: data.optionValue as VoiceEffect })}
        >
          {effects.map((effect) => (
            <Option key={effect} value={effect}>
              {effectLabel(effect)}
            </Option>
          ))}
        </Dropdown>
        <InfoTip info={t("voiceChanger.info.effect")} label={t("voiceChanger.effect")} />
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
          onChange={(pitch) => setCustom({ pitch })}
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
          onChange={(formant) => setCustom({ formant })}
        />
        <ParameterKnob
          label={t("voiceChanger.breathiness")}
          info={t("voiceChanger.info.breathiness")}
          value={state.breathiness}
          min={0}
          max={1}
          step={0.01}
          format={percent}
          parse={parsePercent}
          defaultValue={defaultState?.breathiness}
          onChange={(breathiness) => setCustom({ breathiness })}
        />
        <ParameterKnob
          label={t("voiceChanger.wobble")}
          info={t("voiceChanger.info.wobble")}
          value={state.wobble}
          min={0}
          max={1}
          step={0.01}
          format={percent}
          parse={parsePercent}
          defaultValue={defaultState?.wobble}
          onChange={(wobble) => setCustom({ wobble })}
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
