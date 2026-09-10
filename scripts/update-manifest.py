#!/usr/bin/env python3
"""Prepend a new version entry to manifest.json without rewriting history.

Usage: update-manifest.py VERSION ZIP_PATH

Reads the existing manifest.json from the checkout (git is the source of
truth). Missing file -> start empty with a loud warning. Invalid JSON -> fail.
targetAbi is read from build.yaml (single source of truth). Existing entries
are preserved exactly; the new entry is deduped by version (new wins) and the
array is sorted descending by numeric 4-tuple.
"""
import hashlib
import json
import re
import sys
from datetime import datetime, timezone

MANIFEST = "manifest.json"
BUILD_YAML = "build.yaml"
REPO = "johnpc/jellyfin-plugin-popular-tracks"

DEFAULT_PLUGIN = {
    "guid": "f24f31a3-b1a9-4f70-97c9-4b25a8863a59",
    "name": "PopularTracks",
    "overview": "Fixes artist \"Popular\" track ordering using real Last.fm popularity instead of local play counts.",
    "description": "PopularTracks reorders the artist \"Popular\" (top songs) list by real listening popularity from the Last.fm API, instead of Jellyfin's local PlayCount which is meaningless on servers where nobody scrobbles. It transparently intercepts the artist songs query, re-orders owned tracks by Last.fm artist.getTopTracks rank, and leaves everything else untouched. Requires only a free Last.fm API key; no client changes.",
    "owner": "johnpc",
    "category": "Metadata",
    "versions": [],
}


def fail(msg):
    print(f"ERROR: {msg}", file=sys.stderr)
    sys.exit(1)


def version_tuple(version):
    parts = version.split(".")
    if len(parts) != 4 or not all(p.isdigit() for p in parts):
        fail(f"version {version!r} is not a numeric 4-tuple")
    return tuple(int(p) for p in parts)


def read_target_abi():
    try:
        with open(BUILD_YAML, encoding="utf-8") as f:
            text = f.read()
    except OSError as exc:
        fail(f"cannot read {BUILD_YAML}: {exc}")
    match = re.search(r'^targetAbi:\s*"?([0-9.]+)"?\s*$', text, re.MULTILINE)
    if not match:
        fail(f"no targetAbi found in {BUILD_YAML}")
    return match.group(1)


def read_manifest():
    try:
        with open(MANIFEST, encoding="utf-8") as f:
            data = json.load(f)
    except FileNotFoundError:
        print(f"WARNING: {MANIFEST} missing from checkout -- starting with an "
              "EMPTY versions list. Historical releases will be absent!",
              file=sys.stderr)
        return dict(DEFAULT_PLUGIN)
    except ValueError as exc:
        fail(f"{MANIFEST} exists but is invalid JSON -- refusing to release: {exc}")
    if not isinstance(data, list) or len(data) != 1 or not isinstance(data[0], dict):
        fail(f"{MANIFEST} must be a JSON array with exactly one plugin object")
    return data[0]


def main():
    if len(sys.argv) != 3:
        fail("usage: update-manifest.py VERSION ZIP_PATH")
    version, zip_path = sys.argv[1], sys.argv[2]
    version_tuple(version)

    digest = hashlib.md5()
    with open(zip_path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            digest.update(chunk)

    plugin = read_manifest()
    new_entry = {
        "version": version,
        "changelog": f"https://github.com/{REPO}/releases/tag/{version}",
        "targetAbi": read_target_abi(),
        "sourceUrl": f"https://github.com/{REPO}/releases/download/{version}/"
                     f"jellyfin-plugin-popular-tracks-{version}.zip",
        "checksum": digest.hexdigest(),
        "timestamp": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
    }

    kept = [e for e in plugin.get("versions", []) if e.get("version") != version]
    plugin["versions"] = sorted(kept + [new_entry],
                                key=lambda e: version_tuple(e["version"]),
                                reverse=True)

    with open(MANIFEST, "w", encoding="utf-8") as f:
        json.dump([plugin], f, indent=2, ensure_ascii=False)
        f.write("\n")
    print(f"manifest updated: {len(plugin['versions'])} entries, added {version} "
          f"(targetAbi {new_entry['targetAbi']}, md5 {new_entry['checksum']})")


if __name__ == "__main__":
    main()
