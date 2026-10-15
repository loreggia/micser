import { makeStyles, mergeClasses, tokens } from "@fluentui/react-components";
import { useEffect, useRef, useState, type KeyboardEvent, type PointerEvent as ReactPointerEvent } from "react";
import { useLanguage } from "../i18n/i18n";
import { useTranslation } from "../i18n/translations";
import { formatNumber, parseNumber } from "../lib/labels";
import { ParameterLabel } from "./ParameterLabel";

const useStyles = makeStyles({
  root: {
    // rows of the parent's grid (useDefaultStyles().knobs), so knobs and values line up when a label wraps
    display: "grid",
    gridRow: "span 3",
    gridTemplateRows: "subgrid",
    rowGap: tokens.spacingVerticalXXS,
    justifyItems: "center",
    minWidth: 0,
    padding: tokens.spacingHorizontalXXS,
    borderRadius: tokens.borderRadiusMedium,
    // around label, knob and value, also after a click; inside the box, so a scrolling widget doesn't clip it
    ":focus-within": {
      outline: `${tokens.strokeWidthThin} solid ${tokens.colorCompoundBrandStroke}`,
      outlineOffset: `calc(-1 * ${tokens.strokeWidthThin})`,
    },
  },
  /** Label, knob and value in one row. */
  inline: {
    gridRow: "auto",
    // as wide as its content, so the focus outline wraps only the control
    width: "fit-content",
    display: "flex",
    alignItems: "center",
    gap: tokens.spacingHorizontalXS,
  },
  label: {
    alignSelf: "end",
    textAlign: "center",
    maxWidth: "100%",
    overflowWrap: "break-word",
    hyphens: "auto",
  },
  knob: {
    width: "40px",
    height: "40px",
    cursor: "ns-resize",
    touchAction: "none",
    userSelect: "none",
    outline: "none",
  },
  inlineLabel: {
    alignSelf: "center",
    textAlign: "start",
  },
  inlineKnob: {
    flexShrink: 0,
    width: "32px",
    height: "32px",
  },
  inlineValue: {
    alignSelf: "center",
    minWidth: "3.5em",
    textAlign: "start",
  },
  knobDisabled: {
    cursor: "default",
  },
  svg: {
    display: "block",
    width: "100%",
    height: "100%",
    fill: "none",
    strokeLinecap: "round",
  },
  track: {
    stroke: tokens.colorNeutralStroke1,
    strokeWidth: 3.5,
  },
  trackDisabled: {
    stroke: tokens.colorNeutralStrokeDisabled,
  },
  fill: {
    stroke: tokens.colorCompoundBrandBackground,
    strokeWidth: 3.5,
  },
  pointer: {
    stroke: tokens.colorNeutralForeground1,
    strokeWidth: 2,
  },
  /** The fill and the pointer. */
  markDisabled: {
    stroke: tokens.colorNeutralForegroundDisabled,
  },
  value: {
    alignSelf: "start",
    height: "20px",
    lineHeight: "20px",
    textAlign: "center",
    color: tokens.colorNeutralForeground2,
    fontSize: tokens.fontSizeBase200,
    fontVariantNumeric: "tabular-nums",
    whiteSpace: "nowrap",
    cursor: "text",
  },
  valueDisabled: {
    color: tokens.colorNeutralForegroundDisabled,
    cursor: "default",
  },
  input: {
    boxSizing: "border-box",
    width: "100%",
    minWidth: "4em",
    height: "20px",
    paddingInline: tokens.spacingHorizontalXXS,
    border: `${tokens.strokeWidthThin} solid ${tokens.colorNeutralStroke1}`,
    borderBottom: `${tokens.strokeWidthThick} solid ${tokens.colorCompoundBrandStroke}`,
    borderRadius: tokens.borderRadiusSmall,
    backgroundColor: tokens.colorNeutralBackground1,
    color: tokens.colorNeutralForeground1,
    fontFamily: tokens.fontFamilyBase,
    fontSize: tokens.fontSizeBase200,
    fontVariantNumeric: "tabular-nums",
    textAlign: "center",
    outline: "none",
  },
  /** As wide as the value, instead of the knob's column. */
  inlineInput: {
    width: "5em",
  },
  invalid: {
    borderBottomColor: tokens.colorPaletteRedBorder2,
  },
});

/** Positions of a logarithmic knob, and of a linear one without a step. */
const defaultPositions = 1000;

/** Vertical pixels of a drag across the whole range; Shift makes it ten times longer. */
export const knobDragDistance = 200;
const fineDrag = 0.1;

/** The knob's arc, in degrees clockwise from the top. */
const startAngle = -135;
const sweep = 270;
const radius = 15;

const point = (angle: number, r: number) => {
  const radians = (angle * Math.PI) / 180;
  return { x: 20 + r * Math.sin(radians), y: 20 - r * Math.cos(radians) };
};

function arc(from: number, to: number) {
  const [a0, a1] = from < to ? [from, to] : [to, from];
  if (a1 - a0 < 0.5) {
    return "";
  }

  const p0 = point(a0, radius);
  const p1 = point(a1, radius);
  return `M${p0.x} ${p0.y}A${radius} ${radius} 0 ${a1 - a0 > 180 ? 1 : 0} 1 ${p1.x} ${p1.y}`;
}

const clamp = (value: number, min: number, max: number) => Math.min(Math.max(value, min), max);

export interface ParameterKnobProps {
  label: string;
  value: number;
  min: number;
  max: number;
  /**
   * Distance between values of a linear knob, also the keyboard step; default: 1/100 of the range.
   */
  step?: number;

  /**
   * Logarithmic scale, e.g. for frequencies. Needs min > 0.
   */
  logarithmic?: boolean;

  /**
   * What the parameter does, in a sentence or two for users new to audio processing; shown behind an info button after the label.
   */
  info?: string;

  /**
   * Formats the value shown below the knob; default: the value with its unit.
   */
  format?: (value: number) => string;

  /**
   * Reads a typed value, the inverse of `format`; default: the number in the text (`parseNumber`). Undefined rejects the text.
   */
  parse?: (text: string) => number | undefined;
  unit?: string;

  /**
   * Dimmed and unchangeable, e.g. a parameter that doesn't apply in the current mode. It keeps its place, so a row of knobs doesn't shift.
   */
  disabled?: boolean;

  /**
   * Label, a smaller knob and the value in one row, e.g. for a control in a header row.
   */
  inline?: boolean;
  onChange: (value: number) => void;
}

/**
 * A labeled knob for a module parameter. Dragging up or down changes the value (Shift for fine steps), and double-clicking it or
 * pressing Enter opens an input for a typed value: Enter or leaving the input applies it, Escape cancels.
 *
 * Put several into a `useDefaultStyles().knobs` grid to have them side by side. Inside graph nodes, wrap controls in an element
 * with the `nodrag` class.
 */
export function ParameterKnob({
  label,
  value,
  min,
  max,
  step,
  logarithmic,
  info,
  format,
  parse = parseNumber,
  unit,
  disabled,
  inline,
  onChange,
}: ParameterKnobProps) {
  const styles = useStyles();
  const { t } = useTranslation();
  const knobRef = useRef<HTMLDivElement>(null);
  const [editing, setEditing] = useState(false);
  // formats change with the language
  useLanguage();
  const text = format ? format(value) : `${formatNumber(value, { maximumFractionDigits: 2 })}${unit ? ` ${unit}` : ""}`;

  // The knob works in integer positions: fractional steps like 1/12 aren't exact as floats.
  const positions = logarithmic ? defaultPositions : Math.round((max - min) / (step ?? (max - min) / 100));
  const toPosition = (v: number) =>
    logarithmic ? (Math.log(v / min) / Math.log(max / min)) * positions : ((v - min) / (max - min)) * positions;
  const fromPosition = (p: number) =>
    logarithmic ? min * Math.pow(max / min, p / positions) : min + (p * (max - min)) / positions;
  const position = clamp(Math.round(toPosition(value)), 0, positions);

  const setPosition = (p: number) => {
    const next = clamp(Math.round(p), 0, positions);
    if (next !== position) {
      onChange(fromPosition(next));
    }
  };

  const startDrag = (event: ReactPointerEvent) => {
    if (event.button !== 0 || editing || disabled) {
      return;
    }

    event.preventDefault();
    knobRef.current?.focus();
    let y = event.clientY;
    let current = position;
    let shown = position;
    // on the window, so the drag goes on outside the knob
    const move = (e: PointerEvent) => {
      current = clamp(
        current + ((y - e.clientY) / knobDragDistance) * positions * (e.shiftKey ? fineDrag : 1),
        0,
        positions
      );
      y = e.clientY;
      const next = Math.round(current);
      if (next !== shown) {
        shown = next;
        onChange(fromPosition(next));
      }
    };
    const end = () => {
      window.removeEventListener("pointermove", move);
      window.removeEventListener("pointerup", end);
      window.removeEventListener("pointercancel", end);
    };
    window.addEventListener("pointermove", move);
    window.addEventListener("pointerup", end);
    window.addEventListener("pointercancel", end);
  };

  const onKeyDown = (event: KeyboardEvent) => {
    if (disabled) {
      return;
    }

    const page = Math.max(1, Math.round(positions / 10));
    const moves: Partial<Record<string, number>> = {
      ArrowUp: 1,
      ArrowRight: 1,
      ArrowDown: -1,
      ArrowLeft: -1,
      PageUp: page,
      PageDown: -page,
      Home: -positions,
      End: positions,
    };
    const move = moves[event.key];
    if (move !== undefined) {
      setPosition(position + move);
    } else if (event.key === "Enter") {
      setEditing(true);
    } else {
      return;
    }

    // the graph moves selected nodes with the arrow keys
    event.preventDefault();
    event.stopPropagation();
  };

  const startEditing = () => {
    if (!editing && !disabled) {
      setEditing(true);
    }
  };

  const finishEditing = (typed: number | undefined, refocus: boolean) => {
    setEditing(false);
    if (typed !== undefined) {
      const clamped = clamp(typed, min, max);
      // linear values stay on their steps
      onChange(logarithmic ? clamped : fromPosition(Math.round(toPosition(clamped))));
    }

    if (refocus) {
      knobRef.current?.focus();
    }
  };

  const valueAngle = startAngle + (position / positions) * sweep;
  // bipolar ranges like -24 to +24 dB fill from 0
  const originAngle =
    startAngle + (clamp(Math.round(toPosition(clamp(0, min, max))), 0, positions) / positions) * sweep;
  const inner = point(valueAngle, 5);
  const outer = point(valueAngle, 12);

  return (
    <div
      className={mergeClasses(styles.root, inline && styles.inline)}
      onPointerDown={(event) => {
        // the label and the value focus the knob too; the info button and the input keep their own focus
        if (!disabled && event.target instanceof Element && !event.target.closest("button, input, [role=slider]")) {
          event.preventDefault();
          knobRef.current?.focus();
        }
      }}
    >
      <div className={mergeClasses(styles.label, inline && styles.inlineLabel)}>
        <ParameterLabel info={info} disabled={disabled}>
          {label}
        </ParameterLabel>
      </div>
      <div
        ref={knobRef}
        role="slider"
        tabIndex={disabled ? undefined : 0}
        aria-label={label}
        aria-disabled={disabled || undefined}
        aria-valuemin={min}
        aria-valuemax={max}
        aria-valuenow={value}
        aria-valuetext={text}
        // "nokey": the graph's delete shortcut ignores keys pressed here
        className={mergeClasses(styles.knob, inline && styles.inlineKnob, disabled && styles.knobDisabled, "nokey")}
        onPointerDown={startDrag}
        onDoubleClick={(event) => {
          event.stopPropagation();
          startEditing();
        }}
        onKeyDown={onKeyDown}
      >
        <svg className={styles.svg} viewBox="0 0 40 40" aria-hidden="true">
          <path
            className={mergeClasses(styles.track, disabled && styles.trackDisabled)}
            d={arc(startAngle, startAngle + sweep)}
          />
          <path
            className={mergeClasses(styles.fill, disabled && styles.markDisabled)}
            d={arc(originAngle, valueAngle)}
          />
          <line
            className={mergeClasses(styles.pointer, disabled && styles.markDisabled)}
            x1={inner.x}
            y1={inner.y}
            x2={outer.x}
            y2={outer.y}
          />
        </svg>
      </div>
      {editing && !disabled ? (
        <ValueInput
          label={t("value", { name: label })}
          initial={text}
          parse={parse}
          onDone={finishEditing}
          inputClassName={mergeClasses(styles.input, inline && styles.inlineInput)}
          invalidClassName={styles.invalid}
        />
      ) : (
        <span
          className={mergeClasses(styles.value, inline && styles.inlineValue, disabled && styles.valueDisabled)}
          onDoubleClick={(event) => {
            event.stopPropagation();
            startEditing();
          }}
        >
          {text}
        </span>
      )}
    </div>
  );
}

interface ValueInputProps {
  label: string;
  initial: string;
  parse: (text: string) => number | undefined;
  /** The parsed value, or undefined to keep the old one; whether the knob gets the focus back. */
  onDone: (value: number | undefined, refocus: boolean) => void;
  inputClassName: string;
  invalidClassName: string;
}

function ValueInput({ label, initial, parse, onDone, inputClassName, invalidClassName }: ValueInputProps) {
  const ref = useRef<HTMLInputElement>(null);
  const done = useRef(false);
  const [text, setText] = useState(initial);
  const [invalid, setInvalid] = useState(false);

  useEffect(() => {
    ref.current?.focus();
    ref.current?.select();
  }, []);

  const finish = (value: number | undefined, refocus: boolean) => {
    // refocusing the knob blurs the input
    if (!done.current) {
      done.current = true;
      onDone(value, refocus);
    }
  };

  const parsed = () => {
    const value = parse(text);
    return value === undefined || !Number.isFinite(value) ? undefined : value;
  };

  return (
    <input
      ref={ref}
      type="text"
      inputMode="decimal"
      aria-label={label}
      aria-invalid={invalid || undefined}
      className={mergeClasses(inputClassName, invalid && invalidClassName)}
      value={text}
      onChange={(event) => {
        setText(event.target.value);
        setInvalid(false);
      }}
      onKeyDown={(event) => {
        event.stopPropagation();
        if (event.key === "Enter") {
          event.preventDefault();
          const value = parsed();
          if (value === undefined) {
            setInvalid(true);
          } else {
            finish(value, true);
          }
        } else if (event.key === "Escape") {
          event.preventDefault();
          finish(undefined, true);
        }
      }}
      // leaving the input applies a valid value, like Enter
      onBlur={() => finish(parsed(), false)}
      onPointerDown={(event) => event.stopPropagation()}
      onDoubleClick={(event) => event.stopPropagation()}
    />
  );
}
