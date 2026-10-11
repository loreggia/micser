/**
 * The volume slider's scale, as in PipeWire's and PulseAudio's volume controls: the gain is the cube of the slider position, so the
 * position follows loudness instead of amplitude (50 % is about -18 dB, 10 % -60 dB). A module following the system volume shows the
 * same percentage as the desktop's volume control. The engine's `volume` stays the linear gain.
 */

/** The slider position (0..100) of a linear gain (0..1). */
export function volumeToPosition(volume: number): number {
  return Math.round(Math.cbrt(Math.min(Math.max(volume, 0), 1)) * 100);
}

/** The linear gain (0..1) of a slider position (0..100). */
export function positionToVolume(position: number): number {
  return (Math.min(Math.max(position, 0), 100) / 100) ** 3;
}
