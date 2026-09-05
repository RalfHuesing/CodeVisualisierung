import path from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";
import { findDirectoryFileViolations } from "../scripts/check-structure.mjs";
import { QUALITY_LIMITS } from "../scripts/quality-config.mjs";

const projectRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");

describe("repository structure", () => {
  it("keeps every code directory below the file limit", () => {
    expect(findDirectoryFileViolations(projectRoot, QUALITY_LIMITS.directoryFiles)).toEqual([]);
  });
});
