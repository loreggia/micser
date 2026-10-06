import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { EngineApiError, engineFetch, setAccessToken } from "../engineFetch";

const fetch = vi.fn<typeof globalThis.fetch>();

beforeEach(() => {
  vi.stubGlobal("fetch", fetch);
});

afterEach(() => {
  vi.unstubAllGlobals();
  fetch.mockReset();
  setAccessToken(undefined);
});

function sentHeaders() {
  return new Headers(fetch.mock.calls[0][1]?.headers);
}

test("returns the parsed body", async () => {
  fetch.mockResolvedValue(Response.json({ gain: 3 }));

  await expect(engineFetch("/api/modules/1", { method: "GET" })).resolves.toEqual({ gain: 3 });
});

test("returns undefined for an empty body", async () => {
  fetch.mockResolvedValue(new Response(null, { status: 204 }));

  await expect(engineFetch("/api/modules/1", { method: "DELETE" })).resolves.toBeUndefined();
});

test("sends the access token and keeps the other headers", async () => {
  fetch.mockResolvedValue(new Response());
  setAccessToken("secret");

  await engineFetch("/api/modules", { headers: { "Content-Type": "application/json" } });

  expect(sentHeaders().get("Authorization")).toBe("Bearer secret");
  expect(sentHeaders().get("Content-Type")).toBe("application/json");
});

test("sends no authorization header without a token", async () => {
  fetch.mockResolvedValue(new Response());

  await engineFetch("/api/modules", {});

  expect(sentHeaders().has("Authorization")).toBe(false);
});

test("throws the problem details of an error response", async () => {
  const errors = { "state.gain": ["The field gain must be between -60 and 24."] };
  fetch.mockResolvedValue(Response.json({ title: "Validation failed", errors }, { status: 400 }));

  const error = await engineFetch("/api/modules/1", {}).catch((e: unknown) => e);

  expect(error).toBeInstanceOf(EngineApiError);
  expect(error).toMatchObject({ status: 400, message: "Validation failed", errors });
});

test("prefers the detail and falls back to the status text", async () => {
  fetch.mockResolvedValueOnce(Response.json({ title: "Conflict", detail: "The connection exists." }, { status: 409 }));
  fetch.mockResolvedValueOnce(new Response(null, { status: 404, statusText: "Not Found" }));

  await expect(engineFetch("/api/connections", {})).rejects.toThrow("The connection exists.");
  await expect(engineFetch("/api/modules/2", {})).rejects.toMatchObject({ message: "Not Found", errors: {} });
});
