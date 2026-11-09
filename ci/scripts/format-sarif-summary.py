#!/usr/bin/env python3
"""
HEAppE Security Scan SARIF Summary Formatter
Parses SARIF output from Gitleaks, Trivy, and Semgrep.
Generates a GitHub Step Summary markdown section and optionally propagates exit codes.
"""

import argparse
import json
import os
import sys
from typing import Any, Dict, List


def parse_sarif(sarif_path: str) -> List[Dict[str, Any]]:
    if not os.path.exists(sarif_path):
        return []
    try:
        with open(sarif_path, "r", encoding="utf-8") as f:
            data = json.load(f)
    except Exception as e:
        print(f"Warning: Could not parse SARIF file {sarif_path}: {e}", file=sys.stderr)
        return []

    runs = data.get("runs", [])
    if not runs:
        return []
    return runs[0].get("results", [])


def format_gitleaks_summary(results: List[Dict[str, Any]], title: str) -> str:
    lines = [f"### {title}\n"]
    if not results:
        lines.append("✅ **No secrets or credentials detected across scanned commits.**\n")
        lines.append("> Any known test fixtures and template keys are safely covered by `.gitleaksignore` and `.gitleaks.toml`.\n")
    else:
        lines.append(f"🚨 **Found {len(results)} potential secret(s) or exposed credential(s):**\n")
        lines.append("| Rule | File | Line | Details |")
        lines.append("| :--- | :--- | :--- | :--- |")
        for r in results:
            rule_id = r.get("ruleId", "unknown")
            msg = r.get("message", {}).get("text", "").replace("\n", " ").strip()
            locs = r.get("locations", [])
            uri = ""
            line_num = "-"
            if locs:
                ploc = locs[0].get("physicalLocation", {})
                uri = ploc.get("artifactLocation", {}).get("uri", "")
                line_num = str(ploc.get("region", {}).get("startLine", "-"))
            lines.append(f"| `{rule_id}` | `{uri}` | {line_num} | {msg} |")
        lines.append("")
        lines.append("> 💡 *If these are intentional mock fixtures or false positives, add their fingerprints to `.gitleaksignore`.*")
        lines.append("")
    return "\n".join(lines)


def format_trivy_summary(results: List[Dict[str, Any]], title: str) -> str:
    lines = [f"### {title}\n"]
    if not results:
        lines.append("✅ **No CRITICAL or HIGH vulnerabilities or misconfigurations detected.**\n")
    else:
        lines.append(f"⚠️ **Found {len(results)} finding(s):**\n")
        lines.append("| Severity | ID / Rule | Target | Message |")
        lines.append("| :--- | :--- | :--- | :--- |")
        for r in results:
            rule_id = r.get("ruleId", "unknown")
            level = r.get("level", "warning").upper()
            msg = r.get("message", {}).get("text", "").replace("\n", " ").strip()
            if len(msg) > 120:
                msg = msg[:117] + "..."
            locs = r.get("locations", [])
            uri = ""
            if locs:
                ploc = locs[0].get("physicalLocation", {})
                uri = ploc.get("artifactLocation", {}).get("uri", "")
            lines.append(f"| **{level}** | `{rule_id}` | `{uri}` | {msg} |")
        lines.append("")
    return "\n".join(lines)


def format_semgrep_summary(results: List[Dict[str, Any]], title: str) -> str:
    lines = [f"### {title}\n"]
    if not results:
        lines.append("✅ **No SAST security issues or vulnerabilities detected.**\n")
    else:
        lines.append(f"⚠️ **Found {len(results)} static analysis finding(s):**\n")
        lines.append("| Severity | Rule | Location | Description |")
        lines.append("| :--- | :--- | :--- | :--- |")
        for r in results:
            rule_id = r.get("ruleId", "unknown")
            level = r.get("level", "warning").upper()
            msg = r.get("message", {}).get("text", "").replace("\n", " ").strip()
            if len(msg) > 120:
                msg = msg[:117] + "..."
            locs = r.get("locations", [])
            loc_str = ""
            if locs:
                ploc = locs[0].get("physicalLocation", {})
                uri = ploc.get("artifactLocation", {}).get("uri", "")
                line_num = ploc.get("region", {}).get("startLine", "")
                loc_str = f"`{uri}:{line_num}`" if line_num else f"`{uri}`"
            lines.append(f"| **{level}** | `{rule_id}` | {loc_str} | {msg} |")
        lines.append("")
    return "\n".join(lines)


def main():
    parser = argparse.ArgumentParser(description="Format SARIF report as GitHub Step Summary")
    parser.add_argument("--tool", required=True, choices=["gitleaks", "trivy", "semgrep"])
    parser.add_argument("--sarif", required=True, help="Path to SARIF JSON file")
    parser.add_argument("--title", required=True, help="Summary title header")
    parser.add_argument("--exit-code", default="0", help="Original exit code of the scanner tool")

    args = parser.parse_args()

    results = parse_sarif(args.sarif)

    if args.tool == "gitleaks":
        md = format_gitleaks_summary(results, args.title)
    elif args.tool == "trivy":
        md = format_trivy_summary(results, args.title)
    else:
        md = format_semgrep_summary(results, args.title)

    step_summary = os.getenv("GITHUB_STEP_SUMMARY")
    if step_summary:
        try:
            with open(step_summary, "a", encoding="utf-8") as f:
                f.write(md + "\n\n")
            print(f"Appended {args.tool} summary to {step_summary}")
        except Exception as e:
            print(f"Warning: Failed to write to GITHUB_STEP_SUMMARY: {e}", file=sys.stderr)
    else:
        print(md)

    # Propagate scanner exit code
    try:
        code = int(args.exit_code)
    except ValueError:
        code = 0

    if code != 0:
        sys.exit(code)


if __name__ == "__main__":
    main()
