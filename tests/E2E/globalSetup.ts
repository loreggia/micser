import { buildEngine } from "./servers";

/** Builds the engine once; each worker starts its own from the build output. */
export default function globalSetup() {
  process.env.MICSER_E2E_ENGINE = buildEngine(process.env.CI ? "Release" : "Debug");
}
