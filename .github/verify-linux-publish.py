"""Reject incomplete or wrong-RID application artifacts before Docker publication."""
import json
from pathlib import Path
import re
import struct
import sys
import xml.etree.ElementTree as ET


publish = Path(sys.argv[1])
rid = sys.argv[2]
expected_machine = {"linux-x64": 62, "linux-arm64": 183}[rid]


def require(relative):
    path = publish / relative
    if not path.is_file():
        raise RuntimeError(f"Missing publish file: {path}")
    return path


# Check ELF headers, not file names: an x64 apphost/CLI in an ARM image is unusable.
for relative in ("Smartstore.Web", f"runtimes/{rid}/native/lightningcss"):
    header = require(relative).read_bytes()[:20]
    if len(header) < 20 or header[:6] != b"\x7fELF\x02\x01" or struct.unpack("<H", header[18:20])[0] != expected_machine:
        raise RuntimeError(f"Wrong ELF architecture: {relative} (expected {rid})")

deps = json.loads(require("Smartstore.Web.deps.json").read_text(encoding="utf-8"))
if not deps["runtimeTarget"]["name"].endswith(f"/{rid}"):
    raise RuntimeError(f"Wrong runtime in Smartstore.Web.deps.json: expected {rid}")

# Providers are not ProjectReferences; publishing Web alone would silently omit them.
for provider in ("SqlServer", "MySql", "PostgreSql", "Sqlite"):
    require(f"Smartstore.Data.{provider}.dll")

# Only modules enrolled in the public solution belong in the Community image.
solution = Path("Smartstore.sln").read_text(encoding="utf-8-sig")
for relative in re.findall(r'"(src\\Smartstore.Modules\\[^"\n]+\.csproj)"', solution):
    project = Path(relative.replace("\\", "/"))
    descriptor = json.loads(project.with_name("module.json").read_text(encoding="utf-8-sig"))
    module = descriptor["SystemName"]
    assembly = ET.parse(project).findtext(".//AssemblyName") or project.stem
    require(f"Modules/{module}/module.json")
    require(f"Modules/{module}/{assembly}.dll")

print(f"Verified {rid} publish: apphost, Lightning CSS, data providers and Community modules.")
