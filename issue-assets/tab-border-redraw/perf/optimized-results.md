# Cached tab border benchmark

The optimization caches clean shared line contributions. Overlapped views retain resolved cells and reserved gaps; tiled views retain geometry and exclusions for auto-join. A redraw invalidates the owning view's contribution. Screen-frame or composition-mode changes and newly added canvas lines trigger regeneration. Cache-owned canvases are disposed with their views.

## Results

Comparison against the original correctness fix, which rebuilt every clean shared view's adornments and traversed its SubViews. These are local in-memory redraw measurements, not terminal frame rates.

| Tabs | Controls/tab | Original fix, ms/redraw | Cached, ms/redraw | Less time | Allocated MiB/redraw, original → cached | Control visits/redraw |
|---:|---:|---:|---:|---:|---:|---:|
| 1 | 1 | 21.81 | 21.23 | 2.7% | 38.09 → 38.09 | 1 → 1 |
| 1 | 50 | 25.14 | 24.45 | 2.8% | 39.12 → 39.12 | 50 → 50 |
| 4 | 1 | 28.27 | 27.02 | 4.4% | 41.81 → 40.67 | 4 → 1 |
| 4 | 50 | 32.03 | 28.17 | 12.0% | 47.98 → 41.88 | 200 → 50 |
| 20 | 1 | 48.72 | 36.03 | 26.0% | 63.92 → 56.27 | 20 → 1 |
| 20 | 50 | 88.79 | 39.68 | 55.3% | 103.93 → 58.23 | 1000 → 50 |

The 20-tab/50-control case uses 55% less redraw time (2.24x faster) and 44% fewer allocated bytes. Control visits drop from 1,000 to 50. Page content redraws remain zero. Single-tab timing differences are within the observed measurement variation.

Border composition still scales with tab count. The cache retains one contribution per shared view, trading retained geometry/cells for less traversal and fewer allocations. Allocated bytes per redraw do not measure peak or retained RAM.

## Method

The same standalone harness (Program.cs and TabRedrawPerf.csproj) uses a 160 × 30 screen, top tabs with the first selected, and one dirty button. Each case warms up with 30 redraws, calibrates with 10 redraws, and measures five batches. Here each measured batch contained 10 redraws. Tiered compilation was disabled and real driver I/O was disabled. The fixture does not call terminal refresh/output.

Both implementations ran twice, in opposite orders: original → cached, then cached → original. Each table value averages the two run medians. No test suite or recording ran concurrently with measurements. The original-fix control is the assembly retained from the previous benchmark; the cached assembly was built from the current changes.

Raw data: original-fix-recheck.json, original-fix-repeat.json, optimized.json, optimized-repeat.json. Summary: optimized-comparison.json. Interactive chart: optimized-comparison.html.

## Verification

- Release solution build: passed, with existing warnings.
- Focused rendering tests: 446 passed, 3 existing skips.
- Nonparallel suite: 33 passed.
- Full parallel suite: 17,951 passed, 16 skipped, 5 failed. Four failures are the previously confirmed terminal-color failures. The additional LegacyConsoleTests.IsLegacyConsole_False_Force16Colors_True_False failure passed when the complete LegacyConsoleTests class was rerun (4 passed).
- Twelve tab-side/selection regression cases now also assert that clean tabs' controls are not visited.
- Fifteen border-change cases compare partial and forced-full redraw glyphs and attributes.
- Eight generic shared-canvas cases cover clean-view reuse, direct draws removing borders, composition mode changes, and newly added custom lines for both tiled and overlapped views.
- Inspected mouse/keyboard capture frames and raw cast: ../optimized.gif and ../optimized.cast. Tab borders and selected openings remain stable.

## Commands

From the repository root:

```powershell
dotnet build artifacts/tab-redraw/perf/TabRedrawPerf.csproj --configuration Release --no-restore
$env:DisableRealDriverIO = '1'
$env:DOTNET_TieredCompilation = '0'
try {
    Copy-Item artifacts/tab-redraw/perf/Terminal.Gui.fixed.dll artifacts/tab-redraw/perf/bin/Release/net10.0/Terminal.Gui.dll
    dotnet artifacts/tab-redraw/perf/bin/Release/net10.0/Benchmarks.dll artifacts/tab-redraw/perf/original-fix-recheck.json
    Copy-Item artifacts/tab-redraw/perf/Terminal.Gui.optimized.dll artifacts/tab-redraw/perf/bin/Release/net10.0/Terminal.Gui.dll
    dotnet artifacts/tab-redraw/perf/bin/Release/net10.0/Benchmarks.dll artifacts/tab-redraw/perf/optimized.json
    dotnet artifacts/tab-redraw/perf/bin/Release/net10.0/Benchmarks.dll artifacts/tab-redraw/perf/optimized-repeat.json
    Copy-Item artifacts/tab-redraw/perf/Terminal.Gui.fixed.dll artifacts/tab-redraw/perf/bin/Release/net10.0/Terminal.Gui.dll
    dotnet artifacts/tab-redraw/perf/bin/Release/net10.0/Benchmarks.dll artifacts/tab-redraw/perf/original-fix-repeat.json
} finally {
    Copy-Item artifacts/tab-redraw/perf/Terminal.Gui.optimized.dll artifacts/tab-redraw/perf/bin/Release/net10.0/Terminal.Gui.dll
}
```

Exact recording command:

```powershell
& ./artifacts/tab-redraw/tuirec/tuirec.exe record --binary dotnet --args 'Examples/ScenarioRunner/bin/Release/net10.0/ScenarioRunner.dll,run,Tabs Example,--driver,ansi' --output artifacts/tab-redraw/optimized.gif --cast-output artifacts/tab-redraw/optimized.cast --title 'Tabs cached border redraw' --keystrokes 'wait:1200,move:7:7,wait:500,move:12:8,wait:500,Tab,wait:500,Tab,wait:500,click:19:4,wait:600,click:7:4,wait:600,Esc' --startup-delay 1500 --input-delay 1000 --cols 120 --rows 35 --max-duration 25 --drain 500
```
