import { Switch } from "@fluentui/react-components";
import {
  formatNumber,
  ParameterSlider,
  useDefaultStyles,
  usePortLayouts,
  type DelayChannel,
  type WidgetProps,
} from "@micser/web-sdk";
import { useTranslation } from "../i18n";

/** The engine's DelayModule.MaxTime and MaxChannelTime, in seconds. */
const maxTime = 2;
const maxChannelTime = 0.1;

const milliseconds = (seconds: number) =>
  `${formatNumber(seconds * 1000, { minimumFractionDigits: 1, maximumFractionDigits: 1 })} ms`;

export function DelayWidget({ module, setState }: WidgetProps<"Delay">) {
  const styles = useDefaultStyles();
  const { t } = useTranslation();
  const state = module.state;
  const layouts = usePortLayouts(module.id);
  const perChannel = state.channels.length > 0;
  // the input's channels in the last block; before that, or while it carries nothing, the module's channel count or stereo
  const channelCount = layouts?.inputs.Input?.channelCount || module.channelCount || 2;
  const shownChannels = Math.max(channelCount, state.channels.length);

  const setChannel = (index: number, time: number) => {
    const channels: DelayChannel[] = Array.from({ length: Math.max(state.channels.length, index + 1) }, (_, i) => ({
      time: state.channels[i]?.time ?? 0,
    }));
    setState({ ...state, channels: channels.with(index, { time }) });
  };

  return (
    <div className={styles.column}>
      <ParameterSlider
        label={t("delay.time")}
        value={state.time}
        min={0}
        max={maxTime}
        step={0.0001}
        format={milliseconds}
        onChange={(time) => setState({ ...state, time })}
      />
      <Switch
        label={t("delay.perChannel")}
        checked={perChannel}
        onChange={(_, data) =>
          setState({
            ...state,
            channels: data.checked ? Array.from({ length: channelCount }, () => ({ time: 0 })) : [],
          })
        }
      />
      {perChannel &&
        Array.from({ length: shownChannels }, (_, index) => (
          <ParameterSlider
            key={index}
            label={t("delay.channel", { number: index + 1 })}
            value={state.channels[index]?.time ?? 0}
            min={0}
            max={maxChannelTime}
            step={0.0001}
            format={(seconds) => `+${milliseconds(seconds)}`}
            onChange={(time) => setChannel(index, time)}
          />
        ))}
    </div>
  );
}
