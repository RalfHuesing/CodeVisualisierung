export const QUALITY_LIMITS = Object.freeze({
  directoryFiles: 8,
  fileLines: Object.freeze({
    ".cjs": 500,
    ".css": 400,
    ".html": 300,
    ".js": 500,
    ".mjs": 500,
    ".ts": 500,
    ".tsx": 500
  }),
  functionLines: 80
});
