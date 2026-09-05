import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

export const FILE_LIMITS = Object.freeze({
  ".cjs": 500,
  ".css": 400,
  ".html": 300,
  ".js": 500,
  ".mjs": 500,
  ".ts": 500,
  ".tsx": 500
});

const IGNORED_DIRECTORIES = new Set([
  ".git",
  "coverage",
  "dist",
  "node_modules",
  "vendor"
]);

export function countPhysicalLines(text) {
  const normalizedText = text.replaceAll("\r\n", "\n");
  if (normalizedText.length === 0) {
    return 0;
  }

  const lines = normalizedText.split("\n");
  return normalizedText.endsWith("\n") ? lines.length - 1 : lines.length;
}

export function checkFileSize(filePath, text) {
  const extension = path.extname(filePath).toLowerCase();
  const limit = FILE_LIMITS[extension];

  if (limit === undefined) {
    return null;
  }

  const lineCount = countPhysicalLines(text);
  if (lineCount <= limit) {
    return null;
  }

  return {
    filePath,
    lineCount,
    limit
  };
}

export function collectSourceFiles(directory) {
  const files = [];

  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    if (entry.isDirectory() && IGNORED_DIRECTORIES.has(entry.name)) {
      continue;
    }

    const entryPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      files.push(...collectSourceFiles(entryPath));
      continue;
    }

    if (FILE_LIMITS[path.extname(entry.name).toLowerCase()] !== undefined) {
      files.push(entryPath);
    }
  }

  return files;
}

export function findViolations(rootDirectory) {
  return collectSourceFiles(rootDirectory)
    .map((filePath) => checkFileSize(filePath, fs.readFileSync(filePath, "utf8")))
    .filter((violation) => violation !== null);
}

function main() {
  const rootDirectory = process.cwd();
  const violations = findViolations(rootDirectory);

  if (violations.length === 0) {
    console.log("Code size check passed.");
    return;
  }

  for (const violation of violations) {
    const relativePath = path.relative(rootDirectory, violation.filePath);
    console.error(`${relativePath}: ${violation.lineCount} lines; limit is ${violation.limit}.`);
  }

  process.exitCode = 1;
}

const invokedFile = process.argv[1] === undefined ? "" : path.resolve(process.argv[1]);
const currentFile = fileURLToPath(import.meta.url);
if (invokedFile === currentFile) {
  main();
}
