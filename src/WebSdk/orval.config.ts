import { defineConfig } from "orval";

// Generates the engine API client from openapi/engine.json, which the engine build writes.
export default defineConfig({
  engine: {
    input: { target: "./openapi/engine.json" },
    output: {
      mode: "tags-split",
      target: "./src/api/generated",
      schemas: "./src/api/generated/model",
      client: "react-query",
      httpClient: "fetch",
      clean: true,
      override: {
        mutator: { path: "./src/api/engineFetch.ts", name: "engineFetch" },
        fetch: { includeHttpResponseReturnType: false },
      },
    },
  },
});
