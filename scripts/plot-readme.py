"""Renders docs/benchmarks/readme.png, the small chart in README.md: 1 000 records, time per library, linear scale.

Usage: python3 scripts/plot-readme.py docs/benchmarks/2026-10-28-libraries-avroconvert-vs-apache-vs-newtonsoft.md
Requires matplotlib (e.g. in a venv: python3 -m venv .venv && .venv/bin/pip install matplotlib).
"""
import sys
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np

SOLAR, INK, DEEP, MIST = "#F59E0B", "#0B1F33", "#1E3A5F", "#94A3B8"
LIBRARIES = [("AvroConvert", "AvroConvert", SOLAR), ("ApacheAvro", "Apache.Avro", DEEP), ("NewtonsoftJson", "Newtonsoft.Json", MIST)]
OPERATIONS = ["Serialize", "Deserialize"]
RECORDS = 1000


def parse_time_us(value: str) -> float:
    number, unit = value.replace(",", "").split()
    return float(number) * {"ns": 1e-3, "μs": 1.0, "us": 1.0, "ms": 1e3, "s": 1e6}[unit]


def load(path: Path):
    """Mean times from the BenchmarkDotNet GitHub table in a raw results file."""
    rows = {}
    for line in path.read_text(encoding="utf-8").splitlines():
        cells = [c.strip().strip("*") for c in line.strip().strip("|").split("|")]
        if len(cells) < 3 or "_" not in cells[0] or not cells[1].isdigit():
            continue
        library, operation = cells[0].split("_")
        rows[(library, operation, int(cells[1]))] = parse_time_us(cells[2])
    return rows


def plot(rows, out: Path):
    fig, ax = plt.subplots(figsize=(7, 3.2))
    x = np.arange(len(OPERATIONS))
    width = 0.26
    for i, (key, name, color) in enumerate(LIBRARIES):
        values = [rows[(key, op, RECORDS)] for op in OPERATIONS]
        bars = ax.bar(x + (i - 1) * width, values, width, color=color, label=name)
        for bar, value in zip(bars, values):
            ax.annotate(f"{value:,.0f} µs".replace(",", " "), (bar.get_x() + bar.get_width() / 2, value),
                        xytext=(0, 3), textcoords="offset points", ha="center", va="bottom", fontsize=8, color=INK)

    ax.set_title(f"{RECORDS:,} records · lower is better".replace(",", " "), fontsize=10, color=INK)
    ax.set_ylabel("mean time [µs]", color=INK)
    ax.set_xticks(x, OPERATIONS)
    ax.set_ylim(0, max(rows[(k, op, RECORDS)] for k, _, _ in LIBRARIES for op in OPERATIONS) * 1.15)
    ax.spines[["top", "right"]].set_visible(False)
    ax.grid(axis="y", color="#E5E7EB", linewidth=0.8)
    ax.set_axisbelow(True)
    ax.legend(frameon=False, fontsize=9, loc="upper left")
    fig.text(0.99, 0.01, "BenchmarkDotNet, .NET 10, Apple M1 Max", ha="right", fontsize=7, color=MIST)
    fig.tight_layout()
    fig.savefig(out, dpi=200)
    print(f"wrote {out}")


if __name__ == "__main__":
    source = Path(sys.argv[1])
    plot(load(source), Path(__file__).resolve().parent.parent / "docs" / "benchmarks" / "readme.png")
