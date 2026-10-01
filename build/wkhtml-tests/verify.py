"""Run the real Smartstore PDF probe and retain PDFs/page images for visual review."""
import hashlib
import html
import json
import os
from pathlib import Path
import platform
import re
import struct
import subprocess
import zlib


def run(*args, timeout=90):
    result = subprocess.run(args, capture_output=True, text=True, timeout=timeout)
    with (work / "commands.log").open("a", encoding="utf-8") as log:
        log.write(f"COMMAND {args!r}\n{result.stdout}\n{result.stderr}\n")
    if result.returncode:
        raise RuntimeError(f"Command failed ({result.returncode}): {args!r}")
    return result.stdout


mode = os.environ["WKHTML_PROBE_MODE"]
rid = {"x86_64": "linux-x64", "aarch64": "linux-arm64"}[platform.machine()]
work = Path("/work/.temp/results") / rid / mode
work.mkdir(parents=True, exist_ok=True)
https_image = os.environ.get("WKHTML_HTTPS_IMAGE_URL", "")
if not https_image.startswith("https://"):
    raise RuntimeError("Set WKHTML_HTTPS_IMAGE_URL to an accessible, trusted HTTPS PNG/JPEG URL.")

# Small local PNG plus the existing Smartstore JPEG exercise both image decoders.
def chunk(kind, data):
    return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data))

png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", 96, 32, 8, 2, 0, 0, 0))
png += chunk(b"IDAT", zlib.compress((b"\0" + bytes((35, 110, 180)) * 96) * 32)) + chunk(b"IEND", b"")
(work / "logo.png").write_bytes(png)
logo = (work / "logo.png").as_uri()
jpeg = Path("/tests/fixture.jpg").as_uri()
rows = "".join(f"<tr><td>ROW-{n:03d}</td><td>Artikel {n} - Gr\u00f6\u00dfe \u00c4\u00d6\u00dc \u00e4\u00f6\u00fc \u00df</td><td>19,95 EUR</td></tr>" for n in range(1, 151))
style = "body {font-family: 'Liberation Sans', Arial; font-size: 12px;} table {width:100%;border-collapse:collapse;} td,th {border:1px solid #bbb;padding:7px;} thead {display:table-header-group;} tr {page-break-inside:avoid;}"
(work / "invoice.html").write_text(
    f'<html><head><meta charset="utf-8"><style>{style}</style></head><body>'
    f'<h1>Smartstore technical invoice fixture</h1><img width="96" src="{logo}">'
    f'<img width="96" src="{jpeg}"><p id="js">JS-NOT-RUN</p>'
    '<script>document.getElementById("js").innerHTML="JS-READY";</script>'
    f'<table><thead><tr><th>TABLE-COLUMNS</th><th>Artikel</th><th>Preis</th></tr></thead><tbody>{rows}</tbody></table>'
    '<p>FINAL-ROW-150</p></body></html>', encoding="utf-8")
(work / "header.html").write_text(
    '<html><head><meta charset="utf-8"></head><body style="font:12px Arial">HEADER-READY</body></html>', encoding="utf-8")
(work / "footer.html").write_text(
    '<html><head><meta charset="utf-8"></head><body style="font:12px Arial">'
    'FOOTER-READY Page <span id="page"></span> / <span id="total"></span>'
    '<script>var p={}; location.search.substring(1).split("&").forEach(function(x){'
    'var v=x.split("=");p[v[0]]=decodeURIComponent(v[1]||"");});'
    'document.getElementById("page").innerHTML=p.page;'
    'document.getElementById("total").innerHTML=p.topage;</script></body></html>', encoding="utf-8")
(work / "https.html").write_text(
    '<html><head><meta charset="utf-8"></head><body><h1>HTTPS-READY</h1>'
    f'<img width="200" src="{html.escape(https_image, quote=True)}"></body></html>', encoding="utf-8")

report = {"rid": rid, "mode": mode, "passed": False, "visual_review": "pending",
          "real_shop_templates": "pending", "https_image": https_image}
try:
    output = run("dotnet", "/work/.temp/probe/Probe.dll", mode, str(work), timeout=240)
    selected = re.search(r"SELECTED .* path=(.+)", output)
    if not selected:
        raise RuntimeError("Probe did not identify the selected executable.")
    executable = Path(selected.group(1).strip())
    expected = {"linux-x64": "eafc8a76b3e9912d20fb358f3296362fde76030fcaeec1cefcef30bc0db81e4b",
                "linux-arm64": "55127a0e37fb2d2bde47a658de6ebff4e57088ae66e45f7dbe909f6384971301"}[rid]
    if hashlib.sha256(executable.read_bytes()).hexdigest() != expected:
        raise RuntimeError("Selected executable differs from the pinned Docker/NuGet build.")
    if "not found" in run("ldd", str(executable)):
        raise RuntimeError("Unresolved shared libraries.")
    version = run(str(executable), "--version").strip()
    if version != "wkhtmltopdf 0.12.6.1 (with patched qt)":
        raise RuntimeError(f"Unexpected version: {version}")
    report.update(executable=str(executable), version=version, sha256=expected)

    for name in ("invoice", "https"):
        pdf = work / f"{name}.pdf"
        if not pdf.read_bytes().startswith(b"%PDF-"):
            raise RuntimeError(f"Invalid PDF: {pdf}")
        text = run("pdftotext", "-layout", str(pdf), "-")
        (work / f"{name}.txt").write_text(text, encoding="utf-8")
        pages = text.rstrip("\f\n").split("\f")
        minimum_pages = 3 if name == "invoice" else 1
        if len(pages) < minimum_pages:
            raise RuntimeError(f"Unexpected page count: {name}")
        for index, page in enumerate(pages, 1):
            if "HEADER-READY" not in page or "FOOTER-READY" not in page:
                raise RuntimeError(f"Missing header/footer: {name} page {index}")
            if not re.search(rf"Page\s+{index}\s*/\s*{len(pages)}", page):
                raise RuntimeError(f"Incorrect page number: {name} page {index}")
        if name == "invoice":
            for marker in ("JS-READY", "ROW-001", "ROW-150", "FINAL-ROW-150", "Gr\u00f6\u00dfe"):
                if marker not in text:
                    raise RuntimeError(f"Missing rendered content: {marker}")
            if any("TABLE-COLUMNS" not in page for page in pages):
                raise RuntimeError("Table column header did not repeat on all pages.")
        images = run("pdfimages", "-list", str(pdf))
        if len(re.findall(r"^\s*\d+\s+\d+\s+image\b", images, re.MULTILINE)) < (2 if name == "invoice" else 1):
            raise RuntimeError(f"PNG/JPEG/HTTPS image missing from PDF: {name}")
        run("pdftoppm", "-r", "100", "-png", str(pdf), str(work / f"{name}-page"), timeout=120)
    report["passed"] = True
finally:
    (work / "report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
print(json.dumps(report, indent=2))
