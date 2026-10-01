import { makeStyles, tokens } from "@fluentui/react-components";
import { useModuleData, type WidgetProps } from "@micser/web-sdk";
import { useEffect, useRef } from "react";

interface Spectrum {
  frequencyResolution: number;
  magnitudes: number[];
}

const width = 280;
const height = 120;
const minFrequency = 20;
const minDecibels = -100;

const useStyles = makeStyles({
  canvas: {
    display: "block",
    width: `${width}px`,
    height: `${height}px`,
    borderRadius: tokens.borderRadiusMedium,
    backgroundColor: tokens.colorNeutralBackground3,
  },
});

/**
 * Draws the spectrum on a logarithmic frequency axis (20 Hz to Nyquist) and a dB axis (-100 to 0 dB).
 */
export function SpectrumWidget({ module }: WidgetProps<"Spectrum">) {
  const styles = useStyles();
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const spectrum = useModuleData<Spectrum>(module.id);

  useEffect(() => {
    const canvas = canvasRef.current;
    const context = canvas?.getContext("2d");
    if (!canvas || !context) {
      return;
    }

    const scale = window.devicePixelRatio;
    canvas.width = width * scale;
    canvas.height = height * scale;
    context.setTransform(scale, 0, 0, scale, 0, 0);
    context.clearRect(0, 0, width, height);

    if (!spectrum || spectrum.magnitudes.length < 2) {
      return;
    }

    const style = getComputedStyle(canvas);
    context.strokeStyle = style.getPropertyValue("--colorBrandForeground1").trim() || "#479ef5";
    context.lineWidth = 1.5;

    const maxFrequency = (spectrum.magnitudes.length - 1) * spectrum.frequencyResolution;
    const logRange = Math.log(maxFrequency / minFrequency);

    context.beginPath();
    let started = false;
    for (let bin = 1; bin < spectrum.magnitudes.length; bin++) {
      const frequency = bin * spectrum.frequencyResolution;
      if (frequency < minFrequency) {
        continue;
      }

      const decibels = Math.max(minDecibels, 20 * Math.log10(spectrum.magnitudes[bin] || 1e-10));
      const x = (Math.log(frequency / minFrequency) / logRange) * width;
      const y = (decibels / minDecibels) * height;

      if (started) {
        context.lineTo(x, y);
      } else {
        context.moveTo(x, y);
        started = true;
      }
    }

    context.stroke();
  }, [spectrum]);

  return <canvas ref={canvasRef} className={styles.canvas} />;
}
