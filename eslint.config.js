import js from "@eslint/js";
import { defineConfig, globalIgnores } from "eslint/config";
import reactHooks from "eslint-plugin-react-hooks";
import reactRefresh from "eslint-plugin-react-refresh";
import globals from "globals";
import tseslint from "typescript-eslint";

export default defineConfig([
  globalIgnores(["artifacts", "**/dist", "**/bin", "**/obj", "**/node_modules", "**/generated"]),
  {
    files: ["**/*.{ts,tsx}"],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      globals: globals.browser,
    },
    rules: {
      "no-restricted-imports": [
        "error",
        {
          name: "@fluentui/react-icons",
          message:
            'Import icons from "@fluentui/react-icons/svg/<name>": in development, Vite loads all icons for the package index.',
        },
      ],
    },
  },
  {
    // test code isn't hot reloaded
    files: ["**/testing/**", "**/*.test.{ts,tsx}"],
    rules: {
      "react-refresh/only-export-components": "off",
    },
  },
  {
    // Playwright, not React: its fixtures call use()
    files: ["tests/E2E/**"],
    rules: {
      "react-hooks/rules-of-hooks": "off",
      "react-refresh/only-export-components": "off",
    },
  },
]);
