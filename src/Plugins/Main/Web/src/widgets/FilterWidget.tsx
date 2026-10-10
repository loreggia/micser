import { Dropdown, Option } from "@fluentui/react-components";
import { formatNumber, hertz, ParameterSlider, useDefaultStyles, type WidgetProps } from "@micser/web-sdk";
import { useTranslation } from "../i18n";

export function FilterWidget({ module, setState }: WidgetProps<"Filter">) {
  const styles = useDefaultStyles();
  const { t } = useTranslation();
  const state = module.state;

  return (
    <div className={styles.column}>
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
