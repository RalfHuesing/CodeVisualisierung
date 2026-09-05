import { describe, expect, it } from "vitest";
import { checkFileSize, countPhysicalLines } from "../scripts/check-code-size.mjs";

describe("code size checks", () => {
  it("counts physical lines without counting the final line break", () => {
    expect(countPhysicalLines("one\ntwo\n")).toBe(2);
    expect(countPhysicalLines("one\r\ntwo")).toBe(2);
    expect(countPhysicalLines("")).toBe(0);
  });

  it("accepts files at their configured limit", () => {
    const text = Array.from({ length: 500 }, () => "code").join("\n");
    expect(checkFileSize("example.js", text)).toBeNull();
  });

  it("reports files above their configured limit", () => {
    const text = Array.from({ length: 501 }, () => "code").join("\n");
    expect(checkFileSize("example.js", text)).toEqual({
      filePath: "example.js",
      lineCount: 501,
      limit: 500
    });
  });

  it("ignores file types without a project limit", () => {
    expect(checkFileSize("example.json", "{}")).toBeNull();
  });
});
