import eslint from "@eslint/js";
import { QUALITY_LIMITS } from "./scripts/quality-config.mjs";

export default [
  {
    ignores: ["**/dist/**", "**/coverage/**", "**/node_modules/**"]
  },
  eslint.configs.recommended,
  {
    files: ["scripts/**/*.{js,mjs,cjs}", "tests/**/*.{js,mjs,cjs}", "apps/**/tests/**/*.{js,mjs,cjs}", "*.config.js"],
    languageOptions: {
      globals: {
        console: "readonly",
        process: "readonly",
        Buffer: "readonly",
        structuredClone: "readonly"
      }
    }
  },
  {
    files: ["apps/viewer/src/**/*.{js,mjs,cjs}"],
    languageOptions: {
      globals: {
        document: "readonly"
      }
    }
  },
  {
    files: ["**/*.{js,mjs,cjs}"],
    rules: {
      "curly": ["error", "all"],
      "eqeqeq": ["error", "always"],
      "max-lines": ["error", { "max": QUALITY_LIMITS.fileLines[".js"], "skipBlankLines": false, "skipComments": false }],
      "max-lines-per-function": ["error", { "max": QUALITY_LIMITS.functionLines, "skipBlankLines": true, "skipComments": true }],
      "no-var": "error",
      "prefer-const": "error"
    }
  }
];
