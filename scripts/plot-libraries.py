"""Renders docs/benchmarks/libraries.png from the LibrariesBenchmark CSV report.

Usage: python3 scripts/plot-libraries.py tests/LibrariesBenchmark/artifacts/results/LibrariesBenchmark.Libraries-report.csv
Requires matplotlib (e.g. in a venv: python3 -m venv .venv && .venv/bin/pip install matplotlib).
"""
import csv
import sys
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np

SOLAR, INK, DEEP, SKY, MIST = "#F59E0B", "#0B1F33", "#1E3A5F", "#38BDF8", "#94A3B8"
LIBRARIES = [("AvroConvert", "AvroConvert", SOLAR), ("ApacheAvro", "Apache.Avro", DEEP), ("NewtonsoftJson", "Newtonsoft.Json", MIST)]
RECORDS = [(1, "1 record"), (1000, "1 000 records"), (20000, "20 000 records")]
OPERATIONS = [("Serialize", "Serialize"), ("Deserialize", "Deserialize")]


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
            library, operation = row["Method"].split("_")
            rows[(library, operation, int(row["Records"]))] = (parse_time_us(row["Mean"]), parse_kb(row["Allocated"]))
    return rows


def fmt(value: float) -> str:
    if value >= 10_000:
        return f"{value / 1000:,.0f}k"
    return f"{value:,.1f}" if value < 100 else f"{value:,.0f}"


def plot(rows, out: Path):
    fig, axes = plt.subplots(2, 3, figsize=(15, 9.5))
    fig.suptitle("AvroConvert 4.0 vs Apache.Avro vs Newtonsoft.Json", fontsize=15, color=INK, fontweight="bold")
    x = np.arange(len(OPERATIONS))
    width = 0.26
    metrics = [(0, "mean time [µs]", "Execution time"), (1, "allocated [KB]", "Memory allocated per call")]
    for (metric_index, ylabel, row_title), row_axes in zip(metrics, axes):
        for ax, (records, label) in zip(row_axes, RECORDS):
            for i, (key, name, color) in enumerate(LIBRARIES):
                values = [rows[(key, op, records)][metric_index] for op, _ in OPERATIONS]
                bars = ax.bar(x + (i - 1) * width, values, width, color=color, label=name)
                for bar, value, (op, _) in zip(bars, values, OPERATIONS):
                    text = fmt(value)
                    base = rows[("AvroConvert", op, records)][metric_index]
                    if key != "AvroConvert":
                        text += f"\n{value / base:.1f}×"
                    ax.annotate(text, (bar.get_x() + bar.get_width() / 2, value), ha="center", va="bottom",
                                fontsize=7.5, color=INK, xytext=(0, 2), textcoords="offset points")
            ax.set_title(f"{row_title} · {label}", color=INK, fontsize=11)
            ax.set_ylabel(ylabel, color=INK)
            ax.set_yscale("log")
            ax.set_xticks(x, [name for _, name in OPERATIONS])
            ax.grid(axis="y", alpha=0.25, which="both")
            ax.margins(y=0.25)
            for spine in ("top", "right"):
                ax.spines[spine].set_visible(False)
    axes[0][0].legend(frameon=False, fontsize=9, loc="upper left")
    fig.text(0.5, 0.005, "BenchmarkDotNet, .NET 10, Apple M1 Max · log scale · lower is better · × = relative to AvroConvert",
             ha="center", fontsize=9, color=DEEP)
    fig.tight_layout(rect=(0, 0.02, 1, 0.96))
    fig.savefig(out, dpi=150)
    print("wrote", out)


def main():
    csv_path = Path(sys.argv[1])
    out_dir = Path(__file__).resolve().parent.parent / "docs" / "benchmarks"
    plot(load(csv_path), out_dir / "libraries.png")


if __name__ == "__main__":
    main()
