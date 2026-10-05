import { execFileSync, spawn } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { join, resolve } from "node:path";
import { setTimeout as delay } from "node:timers/promises";

export const root = resolve(import.meta.dirname, "../..");

const engineStartTimeout = 60_000;

export interface Server {
  url: string;
  stop(): Promise<void>;
}

/**
 * Builds the engine and returns its assembly. Workers start it from there, so they don't build it at the same time.
 */
export function buildEngine(configuration: string) {
  const output = execFileSync(
    "dotnet",
    ["build", "src/Engine", "--configuration", configuration, "-getProperty:TargetPath"],
    { cwd: root, encoding: "utf8", stdio: ["ignore", "pipe", "inherit"] }
  );
  return output.trim().split(/\r?\n/).at(-1)!;
}

/**
 * Starts an engine on a free port with its own config and plugin folders in the directory, without a token. It's ready when it has
 * written its discovery file, which has the port.
 */
export async function startEngine(assembly: string, directory: string): Promise<Server> {
  const discoveryPath = join(directory, "engine.json");
  const engine = spawn("dotnet", [assembly], {
    cwd: root,
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: "Development",
      Urls: "http://127.0.0.1:0",
      Engine__ConfigPath: join(directory, "config.json"),
      Engine__DiscoveryPath: discoveryPath,
      Engine__PluginsPath: join(directory, "plugins"),
      Engine__RequireToken: "false",
      Engine__SingleInstance: "false",
    },
    stdio: ["ignore", "ignore", "pipe"],
  });

  let errors = "";
  engine.stderr.on("data", (chunk: Buffer) => (errors += chunk.toString()));
  const exited = new Promise<number | null>((resolve) => engine.once("exit", resolve));
  const stop = async () => {
    if (engine.exitCode === null) {
      engine.kill();
      await exited;
    }
  };

  const deadline = Date.now() + engineStartTimeout;
  while (!existsSync(discoveryPath)) {
    if (engine.exitCode !== null || Date.now() > deadline) {
      await stop();
      throw new Error(`The engine didn't start (exit code ${engine.exitCode}).\n${errors}`);
    }

    await delay(100);
  }

  const { url } = JSON.parse(readFileSync(discoveryPath, "utf8")) as { url: string };
  return { url, stop };
}

/**
 * Starts the UI's Vite dev server on a free port, proxying to the engine. Each server has its own dependency cache, which the servers
 * would otherwise write at the same time.
 */
export async function startWeb(engineUrl: string, cacheDir: string): Promise<Server> {
  const { createServer } = await import("vite");
  // vite.config.ts reads it
  process.env.MICSER_ENGINE_URL = engineUrl;
  const web = resolve(root, "src/Web");
  const server = await createServer({
    root: web,
    configFile: resolve(web, "vite.config.ts"),
    cacheDir,
    logLevel: "error",
    server: { host: "127.0.0.1", port: 0, strictPort: true, forwardConsole: false },
  });
  await server.listen();

  return { url: server.resolvedUrls!.local[0].replace(/\/$/, ""), stop: () => server.close() };
}
