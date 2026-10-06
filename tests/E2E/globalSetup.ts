import { mkdtempSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join, resolve } from "node:path";
import { buildEngine, buildTestPluginPackage } from "./servers";

/**
 * Builds the engine, which each worker starts from the build output, and the test plugin's package. With MICSER_E2E_PUBLISHED_ENGINE (the
 * assembly of a published engine, as in CI), the workers start that engine instead, which serves the built UI.
 */
export default function globalSetup() {
  const configuration = process.env.CI ? "Release" : "Debug";
  const directory = mkdtempSync(join(tmpdir(), "micser-e2e-package-"));

  const published = process.env.MICSER_E2E_PUBLISHED_ENGINE;
  process.env.MICSER_E2E_ENGINE = published ? resolve(published) : buildEngine(configuration);
  process.env.MICSER_E2E_PLUGIN_PACKAGE = buildTestPluginPackage(configuration, directory);

  return () => rmSync(directory, { recursive: true, force: true });
}
