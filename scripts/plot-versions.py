"""Renders docs/benchmarks/versions-*.png from the VersionsBenchmark CSV report.

Usage: python3 scripts/plot-versions.py tests/VersionsBenchmark/artifacts/results/VersionsBenchmark.AvroConvertVersions-report.csv
Requires matplotlib (e.g. in a venv: python3 -m venv .venv && .venv/bin/pip install matplotlib).
"""
import csv
import sys
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

SOLAR, INK, DEEP, SKY = "#F59E0B", "#0B1F33", "#1E3A5F", "#38BDF8"
VERSION_ORDER = ["2.7.1", "3.2.9", "3.3.0", "3.4.8", "3.4.17", "4.0.0-preview.1"]
SCENARIOS = [
    ("Serialize_Single", "Serialize 1 record"),
    ("Deserialize_Single", "Deserialize 1 record"),
    ("Serialize_1k", "Serialize 1 000 records"),
    ("Deserialize_1k", "Deserialize 1 000 records"),
    ("Serialize_20k", "Serialize 20 000 records"),
    ("Deserialize_20k", "Deserialize 20 000 records"),
]


def parse_time_us(value: str) -> float:
    number, unit = value.replace(",", "").split()
    factor = {"ns": 1e-3, "μs": 1.0, "us": 1.0, "ms": 1e3, "s": 1e6}[unit]
    return float(number) * factor


def parse_kb(value: str) -> float:
    number, unit = value.replace(",", "").split()
    factor = {"B": 1 / 1024, "KB": 1.0, "MB": 1024.0}[unit]
    return float(number) * factor


def load(path: Path):
    rows = {}
    with path.open(encoding="utf-8-sig") as f:
        for row in csv.DictReader(f):
            if row["Mean"] == "NA":
                continue
            rows[(row["Method"], row["Job"])] = (parse_time_us(row["Mean"]), parse_kb(row["Allocated"]))
    return rows


def plot(rows, metric_index, ylabel, title, out: Path):
    fig, axes = plt.subplots(2, 3, figsize=(15, 8.5))
    fig.suptitle(title, fontsize=15, color=INK, fontweight="bold")
    for ax, (method, label) in zip(axes.flat, SCENARIOS):
        values = [rows.get((method, v), (None, None))[metric_index] for v in VERSION_ORDER]
        colors = [SOLAR if v.startswith("4.") else DEEP for v in VERSION_ORDER]
        bars = ax.bar(VERSION_ORDER, [v or 0 for v in values], color=colors)
        ax.set_title(label, color=INK, fontsize=11)
        ax.set_ylabel(ylabel, color=INK)
        ax.set_yscale("log")
        ax.tick_params(axis="x", labelrotation=30, labelsize=8)
        ax.grid(axis="y", alpha=0.25, which="both")
        for spine in ("top", "right"):
            ax.spines[spine].set_visible(False)

        baseline = rows.get((method, "3.4.17"), (None, None))[metric_index]
        for bar, value in zip(bars, values):
            if value is None:
                continue
            text = f"{value:,.1f}" if value < 100 else f"{value:,.0f}"
            if baseline and bar.get_x() == bars[-1].get_x():
                text += f"\n{baseline / value:.1f}× vs 3.4.17"
            ax.annotate(text, (bar.get_x() + bar.get_width() / 2, value), ha="center", va="bottom",
                        fontsize=7.5, color=INK, xytext=(0, 2), textcoords="offset points")
    fig.text(0.5, 0.005, "BenchmarkDotNet, .NET 10, Apple M1 Max · log scale · lower is better · 4.0 highlighted",
             ha="center", fontsize=9, color=DEEP)
    fig.tight_layout(rect=(0, 0.02, 1, 0.96))
    fig.savefig(out, dpi=150)
    print("wrote", out)


def main():
    csv_path = Path(sys.argv[1])
    out_dir = Path(__file__).resolve().parent.parent / "docs" / "benchmarks"
    rows = load(csv_path)
    plot(rows, 0, "mean time [µs]", "AvroConvert releases – execution time", out_dir / "versions-time.png")
    plot(rows, 1, "allocated [KB]", "AvroConvert releases – memory allocated per call", out_dir / "versions-memory.png")


if __name__ == "__main__":
    main()
