import eslint from "@eslint/js";

export default [
  {
    ignores: ["**/dist/**", "**/coverage/**", "**/node_modules/**"]
  },
  eslint.configs.recommended,
  {
    files: ["scripts/**/*.{js,mjs,cjs}", "*.config.js"],
    languageOptions: {
      globals: {
        console: "readonly",
        process: "readonly"
      }
    }
  },
  {
    files: ["**/*.{js,mjs,cjs}"],
    rules: {
      "curly": ["error", "all"],
      "eqeqeq": ["error", "always"],
      "max-lines": ["error", { "max": 500, "skipBlankLines": false, "skipComments": false }],
      "max-lines-per-function": ["error", { "max": 80, "skipBlankLines": true, "skipComments": true }],
      "no-var": "error",
      "prefer-const": "error"
    }
  }
];
