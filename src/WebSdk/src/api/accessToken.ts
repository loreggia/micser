import { setAccessToken } from "./engineFetch";

const storageKey = "micser.accessToken";

/**
 * Reads the access token the shell passes in the URL fragment (`#token=...`), keeps it for reloads and removes it
 * from the address. Fragments aren't sent to the server, so the token doesn't end up in request logs.
 */
export function initializeAccessToken() {
  const fragment = new URLSearchParams(window.location.hash.slice(1));
  const token = fragment.get("token");

  if (token) {
    try {
      sessionStorage.setItem(storageKey, token);
    } catch {
      // storage can be unavailable; the token still works until the next reload
    }

    fragment.delete("token");
    const hash = fragment.toString();
    history.replaceState(null, "", window.location.pathname + window.location.search + (hash ? `#${hash}` : ""));
    setAccessToken(token);
    return;
  }

  try {
    setAccessToken(sessionStorage.getItem(storageKey) ?? undefined);
  } catch {
    setAccessToken(undefined);
  }
}
