import { afterEach, beforeEach, expect, test } from "vitest";
import { initializeAccessToken } from "./accessToken";
import { getAccessToken, setAccessToken } from "./engineFetch";

const storageKey = "micser.accessToken";
let originalUrl: string;

beforeEach(() => {
  originalUrl = location.href;
});

afterEach(() => {
  history.replaceState(null, "", originalUrl);
  sessionStorage.removeItem(storageKey);
  setAccessToken(undefined);
});

test("takes the token from the fragment, keeps it and removes it from the address", () => {
  const { pathname, search } = location;
  history.replaceState(null, "", "#token=secret&panel=settings");

  initializeAccessToken();

  expect(getAccessToken()).toBe("secret");
  expect(sessionStorage.getItem(storageKey)).toBe("secret");
  expect(location.hash).toBe("#panel=settings");
  expect(location.pathname + location.search).toBe(pathname + search);
});

test("leaves no empty fragment behind", () => {
  history.replaceState(null, "", "#token=secret");

  initializeAccessToken();

  expect(location.href).not.toContain("#");
});

test("uses the stored token after a reload", () => {
  sessionStorage.setItem(storageKey, "stored");

  initializeAccessToken();

  expect(getAccessToken()).toBe("stored");
});

test("has no token without a fragment or a stored one", () => {
  setAccessToken("old");

  initializeAccessToken();

  expect(getAccessToken()).toBeUndefined();
});
