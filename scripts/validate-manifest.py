#!/usr/bin/env python3
"""Validate manifest.json for the PopularTracks Jellyfin plugin repo.

Usage:
    validate-manifest.py [MANIFEST] [--new-version VERSION --zip ZIP_PATH]

Always checks structural invariants. With --new-version/--zip it additionally
asserts the newest entry matches the release being cut and that its checksum
equals the md5 of the built zip.
"""
import argparse
import hashlib
import json
import re
import sys

EXPECTED_GUID = "f24f31a3-b1a9-4f70-97c9-4b25a8863a59"
REQUIRED_TOP_LEVEL = ("guid", "name", "description", "overview", "owner", "category", "versions")
REQUIRED_VERSION_FIELDS = ("version", "targetAbi", "sourceUrl", "checksum", "timestamp", "changelog")
CHECKSUM_RE = re.compile(r"^[a-f0-9]{32}$")


def fail(msg):
    print(f"MANIFEST INVALID: {msg}", file=sys.stderr)
    sys.exit(1)


def version_tuple(version):
    parts = version.split(".")
    if len(parts) != 4 or not all(p.isdigit() for p in parts):
        fail(f"version {version!r} is not a numeric 4-tuple")
    return tuple(int(p) for p in parts)


def md5_of(path):
    digest = hashlib.md5()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            digest.update(chunk)
    return digest.hexdigest()


def validate_plugin(plugin):
    for field in REQUIRED_TOP_LEVEL:
        if field not in plugin:
            fail(f"missing required top-level field {field!r}")
    if plugin["guid"] != EXPECTED_GUID:
        fail(f"guid {plugin['guid']!r} != expected {EXPECTED_GUID!r}")
    versions = plugin["versions"]
    if not isinstance(versions, list) or not versions:
        fail("versions must be a non-empty array")
    for entry in versions:
        for field in REQUIRED_VERSION_FIELDS:
            if field not in entry or not entry[field]:
                fail(f"version entry {entry.get('version', '?')!r} missing field {field!r}")
        if not CHECKSUM_RE.match(entry["checksum"]):
            fail(f"checksum {entry['checksum']!r} for {entry['version']} is not a lowercase md5")
    tuples = [version_tuple(e["version"]) for e in versions]
    for newer, older in zip(tuples, tuples[1:]):
        if newer <= older:
            fail("versions are not strictly descending (duplicate or misordered entries)")
    return versions


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", nargs="?", default="manifest.json")
    parser.add_argument("--new-version", help="version tag being released")
    parser.add_argument("--zip", help="path to the built plugin zip")
    args = parser.parse_args()

    try:
        with open(args.manifest, encoding="utf-8") as f:
            data = json.load(f)
    except (OSError, ValueError) as exc:
        fail(f"cannot read/parse {args.manifest}: {exc}")

    if not isinstance(data, list) or len(data) != 1 or not isinstance(data[0], dict):
        fail("manifest must be a JSON array containing exactly one plugin object")

    versions = validate_plugin(data[0])

    if args.new_version:
        newest = versions[0]
        if newest["version"] != args.new_version:
            fail(f"newest entry {newest['version']!r} != release tag {args.new_version!r}")
        if not args.zip:
            fail("--new-version requires --zip")
        actual = md5_of(args.zip)
        if newest["checksum"] != actual:
            fail(f"newest checksum {newest['checksum']} != md5 of built zip {actual}")

    print(f"manifest OK: {len(versions)} version entries, newest {versions[0]['version']}")


if __name__ == "__main__":
    main()
