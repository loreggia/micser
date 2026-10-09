import js from "@eslint/js";
import prettier from "eslint-config-prettier/flat";
import reactHooks from "eslint-plugin-react-hooks";
import reactRefresh from "eslint-plugin-react-refresh";
import { defineConfig, globalIgnores } from "eslint/config";
import globals from "globals";
import tseslint from "typescript-eslint";

export default defineConfig([
  globalIgnores([
    ".agents",
    ".claude/skills",
    "artifacts",
    "**/dist",
    "**/bin",
    "**/obj",
    "**/node_modules",
    "**/generated",
  ]),
  {
    files: ["**/*.{ts,tsx,mts}"],
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
  {
    // Node scripts, not React
    files: ["scripts/**"],
    languageOptions: {
      globals: globals.node,
    },
    rules: {
      "react-refresh/only-export-components": "off",
    },
  },
  prettier,
]);
