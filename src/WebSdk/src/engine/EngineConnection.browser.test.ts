import type { QueryClient } from "@tanstack/react-query";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { createTestQueryClient, testModule } from "../../testing";
import { getGetModuleQueryKey, getGetModulesQueryKey, updateModule, type ModuleDto } from "../api";
import { EngineConnection } from "./EngineConnection";

vi.mock("../api", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../api")>()),
  updateModule: vi.fn(),
  updateSubgraph: vi.fn(),
}));

const send = vi.mocked(updateModule);
const original = testModule("Gain", { gain: 0 });
let queryClient: QueryClient;
let connection: EngineConnection;

function withGain(gain: number): ModuleDto {
  return { ...original, state: { gain } };
}

function cachedGain() {
  const modules = queryClient.getQueryData<ModuleDto[]>(getGetModulesQueryKey());
  return (modules?.[0] as typeof original | undefined)?.state.gain;
}

beforeEach(() => {
  vi.useFakeTimers();
  queryClient = createTestQueryClient();
  queryClient.setQueryData(getGetModulesQueryKey(), [original]);
  connection = new EngineConnection(queryClient);
  send.mockImplementation((_, module) => Promise.resolve(module));
});

afterEach(() => {
  vi.useRealTimers();
  send.mockReset();
});

test("shows an update in the cache right away", () => {
  connection.updateModule(withGain(3));

  expect(cachedGain()).toBe(3);
  expect(queryClient.getQueryData(getGetModuleQueryKey(original.id))).toEqual(withGain(3));
  expect(send).not.toHaveBeenCalled();
});

test("sends the last of quick updates once, 80 ms after it", async () => {
  connection.updateModule(withGain(1));
  await vi.advanceTimersByTimeAsync(50);
  connection.updateModule(withGain(2));
  await vi.advanceTimersByTimeAsync(79);

  expect(send).not.toHaveBeenCalled();

  await vi.advanceTimersByTimeAsync(1);

  expect(send).toHaveBeenCalledOnce();
  expect(send).toHaveBeenCalledWith(original.id, withGain(2));
});

test("applies the engine's answer", async () => {
  send.mockResolvedValue(withGain(24));

  connection.updateModule(withGain(30));
  await vi.advanceTimersByTimeAsync(80);

  expect(cachedGain()).toBe(24);
});

test("keeps a newer local value over the answer to an older one, and sends it after", async () => {
  let answer!: (module: ModuleDto) => void;
  send.mockImplementationOnce(() => new Promise((resolve) => (answer = resolve)));

  connection.updateModule(withGain(1));
  await vi.advanceTimersByTimeAsync(80);
  connection.updateModule(withGain(2));
  await vi.advanceTimersByTimeAsync(80);

  expect(send).toHaveBeenCalledTimes(1);

  answer(withGain(1));
  await vi.advanceTimersByTimeAsync(0);

  expect(cachedGain()).toBe(2);
  expect(send).toHaveBeenCalledTimes(2);
  expect(send).toHaveBeenLastCalledWith(original.id, withGain(2));
});

test("reports a failed update and reloads the modules", async () => {
  const errors: Error[] = [];
  connection.onError((error) => errors.push(error));
  const invalidate = vi.spyOn(queryClient, "invalidateQueries");
  send.mockRejectedValue(new Error("Validation failed"));

  connection.updateModule(withGain(3));
  await vi.advanceTimersByTimeAsync(80);

  expect(errors.map((error) => error.message)).toEqual(["Validation failed"]);
  expect(invalidate).toHaveBeenCalledWith({ queryKey: getGetModulesQueryKey() });
});
