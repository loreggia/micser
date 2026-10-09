#!/usr/bin/env node
/**
 * Rewrites the line endings of tracked text files in the working tree to what a git checkout would produce, from .gitattributes
 * (`eol`, `text`) and the git config (`core.autocrlf`, `core.eol`). Tools such as CodeMaid write their own line endings, and git
 * only applies the configured ones when it writes a file itself.
 *
 * Without file arguments it checks every tracked file and refreshes the index's stat cache afterwards. With file arguments (from
 * the pre-commit hook) it checks only those, skips untracked ones, and leaves the index alone. Only CRLF and LF are converted; lone
 * CRs stay.
 *
 * Usage: node scripts/repair-line-endings.mts [--dry-run] [file...]
 */
import { execFileSync } from "node:child_process";
import { readFileSync, writeFileSync } from "node:fs";
import path from "node:path";
import process from "node:process";

type Eol = "lf" | "crlf";

const args = process.argv.slice(2);
const dryRun = args.includes("--dry-run");
const filePaths = args.filter((arg) => arg !== "--dry-run");

const repoRoot = execFileSync("git", ["rev-parse", "--show-toplevel"], { encoding: "utf8" }).trim();

const git = (...gitArgs: string[]) => execFileSync("git", gitArgs, { cwd: repoRoot, encoding: "utf8" });
const gitConfig = (key: string) => {
  try {
    return git("config", "--get", key).trim().toLowerCase();
  } catch {
    return ""; // unset
  }
};

const autocrlf = gitConfig("core.autocrlf");
const coreEol = gitConfig("core.eol");
const nativeEol: Eol = process.platform === "win32" ? "crlf" : "lf";

// Mirrors git's checkout conversion (convert.c). undefined: git leaves the file as it is.
function checkoutEol(index: string, attr: string): Eol | undefined {
  const attrs = attr.split(/\s+/);
  if (attrs.includes("-text")) return undefined;
  const eolAttr = attrs.find((a) => a.startsWith("eol="))?.slice(4);
  const text = attrs.includes("text") ? "set" : attrs.includes("text=auto") || eolAttr ? "auto" : undefined;

  // "auto" converts only blobs that git sees as text without CRs
  if (text !== "set" && index !== "lf") return undefined;
  if (eolAttr === "lf" || eolAttr === "crlf") return eolAttr;
  if (autocrlf === "true") return "crlf";
  if (autocrlf === "input") return "lf";
  if (!text) return undefined;
  if (coreEol === "lf" || coreEol === "crlf") return coreEol;
  return nativeEol;
}

function convert(content: Buffer, eol: Eol): Buffer {
  const lf = content.toString("latin1").replace(/\r\n/g, "\n");
  return Buffer.from(eol === "lf" ? lf : lf.replace(/\n/g, "\r\n"), "latin1");
}

const changed: string[] = [];

for (const line of git("ls-files", "--eol", "--", ...filePaths).split("\n")) {
  const match = /^i\/(?<index>\S*)\s+w\/(?<worktree>\S*)\s+attr\/(?<attr>.*?)\s*\t(?<file>.*)$/.exec(line);
  if (!match?.groups) continue;
  const { index, worktree, attr, file } = match.groups;

  // worktree: "" for deleted files and symlinks, "-text" for binary content, "none" without line endings
  if (worktree !== "lf" && worktree !== "crlf" && worktree !== "mixed") continue;
  const eol = checkoutEol(index, attr);
  if (!eol || worktree === eol) continue;

  const fullPath = path.join(repoRoot, file);
  const content = readFileSync(fullPath);
  const converted = convert(content, eol);
  if (converted.equals(content)) continue;

  if (!dryRun) writeFileSync(fullPath, converted);
  changed.push(`${file} (${eol})`);
}

if (changed.length === 0) {
  console.log("All line endings match the git settings.");
} else {
  console.log(`${dryRun ? "Would convert" : "Converted"} ${changed.length} file(s):`);
  for (const file of changed.sort()) console.log(`  ${file}`);
  // rewritten files only differ in their stat data
  if (!dryRun && filePaths.length === 0) git("update-index", "-q", "--refresh");
}
