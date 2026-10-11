import { describe, expect, it } from "vitest";
import { positionToVolume, volumeToPosition } from "../volume";

describe("volume slider scale", () => {
  it("maps the ends to silence and unity gain", () => {
    expect(positionToVolume(0)).toBe(0);
    expect(positionToVolume(100)).toBe(1);
    expect(volumeToPosition(0)).toBe(0);
    expect(volumeToPosition(1)).toBe(100);
  });

  it("is cubic, like the desktop's volume control", () => {
    expect(positionToVolume(30)).toBeCloseTo(0.027, 6);
    expect(volumeToPosition(0.027)).toBe(30);
    // a desktop volume of 20 % is no longer below the first step
    expect(volumeToPosition(0.008)).toBe(20);
  });

  it("round-trips every position", () => {
    for (let position = 0; position <= 100; position++) {
      expect(volumeToPosition(positionToVolume(position))).toBe(position);
    }
  });

  it("clamps out-of-range values", () => {
    expect(volumeToPosition(2)).toBe(100);
    expect(volumeToPosition(-1)).toBe(0);
    expect(positionToVolume(150)).toBe(1);
  });
});
