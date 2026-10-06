import { execFileSync, spawn } from "node:child_process";
import { copyFileSync, cpSync, existsSync, mkdirSync, readdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { setTimeout as delay } from "node:timers/promises";

export const root = resolve(import.meta.dirname, "../..");

const engineStartTimeout = 60_000;

export interface Server {
  url: string;
  stop(): Promise<void>;
}

/** Builds a project and returns an MSBuild property of it, e.g. its output path. */
function build(project: string, configuration: string, property: string) {
  const output = execFileSync(
    "dotnet",
    // without a target, -getProperty only evaluates the project
    ["build", project, "--configuration", configuration, "-t:Build", `-getProperty:${property}`],
    {
      cwd: root,
      encoding: "utf8",
      stdio: ["ignore", "pipe", "inherit"],
    }
  );
  return output.trim().split(/\r?\n/).at(-1)!;
}

/**
 * Builds the engine and returns its assembly. Workers start it from there, so they don't build it at the same time.
 */
export function buildEngine(configuration: string) {
  return build("src/Engine", configuration, "TargetPath");
}

/**
 * Builds the engine tests' plugin (module type "Test") and packs it with the widget bundle in plugin/web as a plugin package (.zip)
 * in the directory. Returns the package's path.
 */
export function buildTestPluginPackage(configuration: string, directory: string) {
  const project = resolve(root, "tests/Engine/TestPlugin");
  const output = build(project, configuration, "TargetDir");
  const content = join(directory, "Test");
  mkdirSync(content, { recursive: true });

  for (const file of readdirSync(output).filter((name) => /\.(dll|json)$/.test(name))) {
    copyFileSync(join(output, file), join(content, file));
  }

  const manifest = JSON.parse(readFileSync(join(project, "plugin.json"), "utf8")) as Record<string, string>;
  writeFileSync(join(content, "plugin.json"), JSON.stringify({ ...manifest, web: "web/index.js" }));
  cpSync(resolve(import.meta.dirname, "plugin/web"), join(content, "web"), { recursive: true });

  const packagePath = join(directory, "Test.zip");
  execFileSync("pwsh", [
    "-NoProfile",
    "-Command",
    `[IO.Compression.ZipFile]::CreateFromDirectory('${content}', '${packagePath}')`,
  ]);
  return packagePath;
}

export interface Engine extends Server {
  /** Stops the engine and starts it again at the same URL, e.g. to apply plugin changes. */
  restart(): Promise<void>;
}

/**
 * Starts an engine on a free port with its own config and plugin folders in the directory, without a token.
 */
export async function startEngine(assembly: string, directory: string): Promise<Engine> {
  let current = await launchEngine(assembly, directory, "http://127.0.0.1:0");
  const url = current.url;

  return {
    url,
    stop: () => current.stop(),
    restart: async () => {
      await current.stop();
      current = await launchEngine(assembly, directory, url);
    },
  };
}

/**
 * Starts an engine process. It's ready when it has written its discovery file, which has the port it got.
 */
async function launchEngine(assembly: string, directory: string, url: string): Promise<Server> {
  const discoveryPath = join(directory, "engine.json");
  // a killed engine leaves its file behind
  rmSync(discoveryPath, { force: true });
  const engine = spawn("dotnet", [assembly], {
    // a published engine serves the UI from wwwroot in its folder
    cwd: dirname(assembly),
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: "Development",
      Urls: url,
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
  const kill = async () => {
    if (engine.exitCode === null) {
      engine.kill();
      await exited;
    }
  };

  const deadline = Date.now() + engineStartTimeout;
  while (!existsSync(discoveryPath)) {
    if (engine.exitCode !== null || Date.now() > deadline) {
      await kill();
      throw new Error(`The engine didn't start (exit code ${engine.exitCode}).\n${errors}`);
    }

    await delay(100);
  }

  const { url: actualUrl } = JSON.parse(readFileSync(discoveryPath, "utf8")) as { url: string };

  // gracefully, so it saves its state, as when the shell stops it
  const stop = async () => {
    if (engine.exitCode !== null) {
      return;
    }

    try {
      await fetch(`${actualUrl}/api/engine/shutdown`, { method: "POST" });
      await Promise.race([exited, delay(10_000)]);
    } finally {
      await kill();
    }
  };

  return { url: actualUrl, stop };
}

/**
 * Starts the UI's Vite dev server on a free port, proxying to the engine. Each server has its own dependency cache, which the servers
 * would otherwise write at the same time.
 */
export async function startWeb(engineUrl: string, cacheDir: string): Promise<Server> {
  const { createLogger, createServer } = await import("vite");
  // vite.config.ts reads it
  process.env.MICSER_ENGINE_URL = engineUrl;
  const web = resolve(root, "src/Web");
  // proxy errors are expected while a test restarts the engine
  const logger = createLogger("error");
  const logError = logger.error;
  logger.error = (message, options) => {
    if (!/proxy (socket )?error/.test(message)) {
      logError(message, options);
    }
  };
  const server = await createServer({
    root: web,
    configFile: resolve(web, "vite.config.ts"),
    cacheDir,
    customLogger: logger,
    server: { host: "127.0.0.1", port: 0, strictPort: true, forwardConsole: false },
  });
  await server.listen();

  return { url: server.resolvedUrls!.local[0].replace(/\/$/, ""), stop: () => server.close() };
}
