#!/usr/bin/env python3
"""Fix RA0017/RA0020 analyzer issues after RT 270 upgrade."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DIRS = [
    ROOT / "Content.Shared",
    ROOT / "Content.Server",
    ROOT / "Content.Client",
    ROOT / "Content.Goobstation.Shared",
    ROOT / "Content.Goobstation.Server",
    ROOT / "Content.Goobstation.Client",
    ROOT / "Content.Goobstation.Common",
    ROOT / "Content.Goobstation.UIKit",
    ROOT / "Content.Goobstation.Maths",
    ROOT / "Corvax",
    ROOT / "SponsorImplementations",
]

CLASS_RE = re.compile(
    r"^(\s*)(?:(public|internal|protected|private)\s+)?"
    r"(?:(sealed|abstract|static)\s+)?"
    r"(class|record|struct)\s+(\w+)",
    re.MULTILINE,
)

PARTIAL_CLASS_RE = re.compile(
    r"^(\s*)(?:(public|internal|protected|private)\s+)?"
    r"(?:(sealed|abstract|static)\s+)?"
    r"partial\s+(class|record|struct)\s+(\w+)",
    re.MULTILINE,
)


def file_has_data_definition(text: str) -> bool:
    return "[DataDefinition]" in text or "[Prototype(" in text


def add_partial_to_types(text: str) -> str:
    if not file_has_data_definition(text):
        return text

    lines = text.splitlines(keepends=True)
    pending_data_def = False
    pending_prototype = False
    out: list[str] = []

    for line in lines:
        stripped = line.strip()
        if stripped.startswith("[DataDefinition"):
            pending_data_def = True
        elif stripped.startswith("[Prototype"):
            pending_prototype = True

        if pending_data_def or pending_prototype:
            if " partial " in line or line.lstrip().startswith("partial "):
                pending_data_def = False
                pending_prototype = False
            else:
                m = CLASS_RE.match(line)
                if m and "partial" not in line:
                    indent, access, modifier, kind, name = m.groups()
                    access = f"{access} " if access else ""
                    modifier = f"{modifier} " if modifier else ""
                    line = f"{indent}{access}{modifier}partial {kind} {name}{line[m.end():]}"
                    pending_data_def = False
                    pending_prototype = False

        out.append(line)

    return "".join(out)


def fix_setters(text: str) -> str:
    # IdDataField / DataField auto-properties without setters
    text = re.sub(
        r"(\{ get;\s*private\s*)init(\s*\})",
        r"\1set\2",
        text,
    )
    text = re.sub(
        r"(\[IdDataField\][^\n]*\n\s*public\s+[\w<>,\s\[\]?]+\s+\w+\s*\{ get;\s*\})",
        lambda m: m.group(0)[:-1] + "; set; }",
        text,
    )
    # { get; } = default on prototype ids (single line)
    text = re.sub(
        r"(\{ get;\s*\})(\s*=\s*[^;]+;)",
        r"{ get; private set; }\2",
        text,
    )
    # { get; } without initializer on data fields - multiline context is harder; fix common readonly
    text = re.sub(
        r"(\{ get;\s*\})(\s*;)",
        r"{ get; set; }\2",
        text,
    )
    return text


def process_file(path: Path) -> bool:
    original = path.read_text(encoding="utf-8")
    updated = add_partial_to_types(fix_setters(original))
    if updated != original:
        path.write_text(updated, encoding="utf-8")
        return True
    return False


def main() -> None:
    changed = 0
    for base in DIRS:
        if not base.exists():
            continue
        for path in base.rglob("*.cs"):
            if process_file(path):
                changed += 1
    print(f"Updated {changed} files")


if __name__ == "__main__":
    main()
