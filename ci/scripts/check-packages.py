#!/usr/bin/env python3
"""
HEAppE NuGet Package Audit & Outdated Check
Scans the solution for vulnerable, deprecated, and outdated packages.
Generates terminal logs, GitHub Actions annotations, and GitHub Step Summary.
"""

import json
import os
import subprocess
import sys
from typing import Any, Dict, List, Optional, Set, Tuple


def run_dotnet_list(solution_path: str, args: List[str]) -> Dict[str, Any]:
    cmd = ["dotnet", "list", solution_path, "package"] + args + ["--format", "json"]
    proc = subprocess.run(
        cmd,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
    )
    stdout = proc.stdout
    start = stdout.find("{")
    end = stdout.rfind("}")
    if start != -1 and end != -1:
        try:
            return json.loads(stdout[start : end + 1])
        except json.JSONDecodeError:
            pass
    return {}


def parse_semver(v: str) -> Tuple[int, int, int]:
    parts = []
    # Strip pre-release or build metadata if present (e.g. 10.0.0-preview...)
    clean_v = v.split("-")[0].split("+")[0]
    for seg in clean_v.split("."):
        try:
            parts.append(int(seg))
        except ValueError:
            parts.append(0)
    while len(parts) < 3:
        parts.append(0)
    return (parts[0], parts[1], parts[2])


def classify_update(curr: str, latest: str) -> str:
    c_maj, c_min, c_patch = parse_semver(curr)
    l_maj, l_min, l_patch = parse_semver(latest)
    if l_maj > c_maj:
        return "Major"
    elif l_min > c_min:
        return "Minor"
    elif l_patch > c_patch:
        return "Patch"
    return "Other"


def collect_vulnerabilities(data: Dict[str, Any]) -> List[Dict[str, Any]]:
    # Group by (package_id, version)
    grouped: Dict[Tuple[str, str], Dict[str, Any]] = {}
    severity_rank = {"Critical": 4, "High": 3, "Moderate": 2, "Low": 1}

    for proj in data.get("projects", []):
        proj_name = os.path.basename(proj.get("path", ""))
        for fw in proj.get("frameworks", []):
            packages = fw.get("topLevelPackages", []) + fw.get("transitivePackages", [])
            for pkg in packages:
                vulns = pkg.get("vulnerabilities", [])
                if not vulns:
                    continue
                pkg_id = pkg.get("id", "")
                ver = pkg.get("resolvedVersion") or pkg.get("requestedVersion") or "Unknown"
                key = (pkg_id, ver)
                if key not in grouped:
                    grouped[key] = {
                        "id": pkg_id,
                        "version": ver,
                        "severities": set(),
                        "advisories": set(),
                        "projects": set(),
                        "max_severity_rank": 0,
                    }
                grouped[key]["projects"].add(proj_name)
                for v in vulns:
                    sev = v.get("severity", "Unknown")
                    url = v.get("advisoryurl", "")
                    grouped[key]["severities"].add(sev)
                    if url:
                        grouped[key]["advisories"].add(url)
                    rank = severity_rank.get(sev, 0)
                    if rank > grouped[key]["max_severity_rank"]:
                        grouped[key]["max_severity_rank"] = rank

    result = list(grouped.values())
    result.sort(key=lambda x: (x["max_severity_rank"], x["id"]), reverse=True)
    return result


def collect_deprecated(data: Dict[str, Any]) -> List[Dict[str, Any]]:
    grouped: Dict[str, Dict[str, Any]] = {}

    for proj in data.get("projects", []):
        proj_name = os.path.basename(proj.get("path", ""))
        for fw in proj.get("frameworks", []):
            packages = fw.get("topLevelPackages", []) + fw.get("transitivePackages", [])
            for pkg in packages:
                reasons = pkg.get("deprecationReasons")
                alt = pkg.get("alternativePackage", {})
                if reasons or alt:
                    pkg_id = pkg.get("id", "")
                    if pkg_id not in grouped:
                        grouped[pkg_id] = {
                            "id": pkg_id,
                            "version": pkg.get("resolvedVersion") or pkg.get("requestedVersion") or "",
                            "reasons": set(reasons) if reasons else set(),
                            "alternative": alt.get("id") if alt else "",
                            "projects": set(),
                        }
                    grouped[pkg_id]["projects"].add(proj_name)

    result = list(grouped.values())
    result.sort(key=lambda x: x["id"])
    return result


def collect_outdated(data: Dict[str, Any]) -> List[Dict[str, Any]]:
    grouped: Dict[str, Dict[str, Any]] = {}

    for proj in data.get("projects", []):
        proj_name = os.path.basename(proj.get("path", ""))
        for fw in proj.get("frameworks", []):
            for pkg in fw.get("topLevelPackages", []):
                pkg_id = pkg.get("id", "")
                curr = pkg.get("resolvedVersion") or pkg.get("requestedVersion") or ""
                latest = pkg.get("latestVersion", "")
                if not latest or latest == curr or latest == "Not found at the sources":
                    continue
                update_type = classify_update(curr, latest)
                if pkg_id not in grouped:
                    grouped[pkg_id] = {
                        "id": pkg_id,
                        "current": curr,
                        "latest": latest,
                        "type": update_type,
                        "projects": set(),
                    }
                grouped[pkg_id]["projects"].add(proj_name)

    type_order = {"Major": 1, "Minor": 2, "Patch": 3, "Other": 4}
    result = list(grouped.values())
    result.sort(key=lambda x: (type_order.get(x["type"], 99), x["id"]))
    return result


def format_markdown_summary(
    vulnerabilities: List[Dict[str, Any]],
    deprecated: List[Dict[str, Any]],
    outdated: List[Dict[str, Any]],
) -> str:
    md: List[str] = []
    md.append("## NuGet Package Status Report")
    md.append("")

    # 1. Security Vulnerabilities
    md.append("### Security Vulnerabilities")
    if not vulnerabilities:
        md.append("**No known package vulnerabilities detected.**")
    else:
        md.append(f"> [!WARNING]\n> Found **{len(vulnerabilities)}** package(s) with known security advisories.")
        md.append("")
        md.append("| Severity | Package | Version | Advisory | Affected Projects |")
        md.append("|:---|:---|:---|:---|:---|")
        for v in vulnerabilities:
            sevs = ", ".join(f"`{s}`" for s in sorted(v["severities"]))
            advs = ", ".join(f"[{url.split('/')[-1]}]({url})" for url in sorted(v["advisories"])) or "N/A"
            projs = f"<details><summary>{len(v['projects'])} projects</summary>{'<br>'.join(sorted(v['projects']))}</details>"
            md.append(f"| {sevs} | `{v['id']}` | `{v['version']}` | {advs} | {projs} |")
    md.append("")

    # 2. Deprecated Packages
    if deprecated:
        md.append("### Deprecated Packages")
        md.append("| Package | Current Version | Reasons | Alternative | Affected Projects |")
        md.append("|:---|:---|:---|:---|:---|")
        for d in deprecated:
            reasons = ", ".join(d["reasons"]) or "N/A"
            alt = f"`{d['alternative']}`" if d["alternative"] else "None"
            projs = f"<details><summary>{len(d['projects'])} projects</summary>{'<br>'.join(sorted(d['projects']))}</details>"
            md.append(f"| `{d['id']}` | `{d['version']}` | {reasons} | {alt} | {projs} |")
        md.append("")

    # 3. Outdated Packages
    md.append("### Outdated Packages")
    if not outdated:
        md.append("**All direct package references are up to date.**")
    else:
        majors = sum(1 for p in outdated if p["type"] == "Major")
        minors = sum(1 for p in outdated if p["type"] == "Minor")
        patches = sum(1 for p in outdated if p["type"] == "Patch")
        others = sum(1 for p in outdated if p["type"] == "Other")

        breakdown = []
        if majors:
            breakdown.append(f"**{majors}** Major")
        if minors:
            breakdown.append(f"**{minors}** Minor")
        if patches:
            breakdown.append(f"**{patches}** Patch")
        if others:
            breakdown.append(f"**{others}** Other")

        md.append(
            f"Found **{len(outdated)}** outdated direct package(s) ({', '.join(breakdown)})."
        )
        md.append("")
        md.append("<details>")
        md.append(f"<summary><b>Click to expand outdated packages list ({len(outdated)})</b></summary>")
        md.append("")
        md.append("| Update | Package | Current | Latest | Affected Projects |")
        md.append("|:---|:---|:---|:---|:---|")
        type_badge = {
            "Major": "`Major`",
            "Minor": "`Minor`",
            "Patch": "`Patch`",
            "Other": "`Other`",
        }
        for o in outdated:
            badge = type_badge.get(o["type"], f"`{o['type']}`")
            projs = f"<details><summary>{len(o['projects'])} projects</summary>{'<br>'.join(sorted(o['projects']))}</details>"
            md.append(f"| {badge} | `{o['id']}` | `{o['current']}` | `{o['latest']}` | {projs} |")
        md.append("")
        md.append("</details>")
    md.append("")

    return "\n".join(md)


def main() -> None:
    solution = sys.argv[1] if len(sys.argv) > 1 else "HEAppE Core.sln"

    print(f"=== Running NuGet Package Audit for: {solution} ===")

    print("Checking for vulnerabilities (including transitive dependencies)...")
    vulnerable_raw = run_dotnet_list(solution, ["--vulnerable", "--include-transitive"])
    vulnerabilities = collect_vulnerabilities(vulnerable_raw)

    print("Checking for deprecated packages...")
    deprecated_raw = run_dotnet_list(solution, ["--deprecated"])
    deprecated = collect_deprecated(deprecated_raw)

    print("Checking for outdated packages...")
    outdated_raw = run_dotnet_list(solution, ["--outdated"])
    outdated = collect_outdated(outdated_raw)

    print("--------------------------------------------------")
    print(f"Summary: {len(vulnerabilities)} vulnerable, {len(deprecated)} deprecated, {len(outdated)} outdated packages.")
    print("--------------------------------------------------")

    # Terminal output
    if vulnerabilities:
        print("\n[!] Vulnerable packages:")
        for v in vulnerabilities:
            print(f"  - {v['id']} ({v['version']}): {', '.join(v['severities'])} -> {', '.join(v['advisories'])}")

    if deprecated:
        print("\n[!] Deprecated packages:")
        for d in deprecated:
            print(f"  - {d['id']} ({d['version']})")

    if outdated:
        print(f"\n[i] Top outdated packages ({min(10, len(outdated))} of {len(outdated)} shown):")
        for o in outdated[:10]:
            print(f"  - [{o['type']}] {o['id']}: {o['current']} -> {o['latest']} ({len(o['projects'])} projects)")

    # Publish to GitHub Step Summary if running in GitHub Actions
    summary_file = os.getenv("GITHUB_STEP_SUMMARY")
    if summary_file:
        md = format_markdown_summary(vulnerabilities, deprecated, outdated)
        try:
            with open(summary_file, "a", encoding="utf-8") as f:
                f.write(md + "\n")
            print(f"\nSuccessfully wrote report to GITHUB_STEP_SUMMARY ({summary_file})")
        except Exception as e:
            print(f"Warning: Failed to write to GITHUB_STEP_SUMMARY: {e}", file=sys.stderr)

    # Emit GitHub Actions workflow warning annotation
    if os.getenv("GITHUB_ACTIONS") == "true":
        warnings = []
        if vulnerabilities:
            warnings.append(f"{len(vulnerabilities)} vulnerable")
        if deprecated:
            warnings.append(f"{len(deprecated)} deprecated")
        if outdated:
            warnings.append(f"{len(outdated)} outdated")

        if warnings:
            msg = f"NuGet Package Audit: Found {', '.join(warnings)} package(s). Check Step Summary for full details."
            print(f"::warning title=NuGet Package Audit::{msg}")

    print("\nPackage check completed.")
    sys.exit(0)


if __name__ == "__main__":
    main()
