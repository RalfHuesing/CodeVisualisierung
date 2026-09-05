import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { QUALITY_LIMITS } from "./quality-config.mjs";

const IGNORED_DIRECTORIES = new Set([
  ".git",
  "coverage",
  "dist",
  "node_modules",
  "vendor"
]);

const SOURCE_EXTENSIONS = new Set(Object.keys(QUALITY_LIMITS.fileLines));

export function countSourceFiles(directory) {
  return fs.readdirSync(directory, { withFileTypes: true })
    .filter((entry) => entry.isFile())
    .filter((entry) => SOURCE_EXTENSIONS.has(path.extname(entry.name).toLowerCase()))
    .length;
}

export function collectDirectories(directory) {
  const directories = [directory];

  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    if (!entry.isDirectory() || IGNORED_DIRECTORIES.has(entry.name)) {
      continue;
    }

    directories.push(...collectDirectories(path.join(directory, entry.name)));
  }

  return directories;
}

export function findDirectoryFileViolations(rootDirectory, fileLimit = QUALITY_LIMITS.directoryFiles) {
  return collectDirectories(rootDirectory)
    .map((directory) => ({ directory, fileCount: countSourceFiles(directory) }))
    .filter(({ fileCount }) => fileCount > fileLimit)
    .map(({ directory, fileCount }) => ({ directory, fileCount, fileLimit }));
}

function main() {
  const violations = findDirectoryFileViolations(process.cwd());

  if (violations.length === 0) {
    console.log("Directory structure check passed.");
    return;
  }

  for (const violation of violations) {
    console.error(`${violation.directory}: ${violation.fileCount} source files; limit is ${violation.fileLimit}.`);
  }

  process.exitCode = 1;
}

const invokedFile = process.argv[1] === undefined ? "" : path.resolve(process.argv[1]);
const currentFile = fileURLToPath(import.meta.url);
if (invokedFile === currentFile) {
  main();
}
