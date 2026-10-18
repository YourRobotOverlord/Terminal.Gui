# Tab border redraw performance

The correctness fix adds measurable work on partial redraws. The baseline is the rendering code at HEAD, which draws incorrect tab borders on these updates. The fixed version adds the shared-canvas regeneration block in View.Drawing.cs.

On this machine, a quick synthetic in-memory redraw benchmark measured:

- Four tabs, one control per tab: 24.41 ms before, 30.42 ms after (1.25x).
- Four tabs, 50 controls per tab: 24.02 ms before, 30.44 ms after (1.27x). Allocations rose from approximately 39.4 MiB to 48.0 MiB per redraw.
- Twenty tabs, one control per tab: 24.85 ms before, 48.01 ms after (1.93x).
- Twenty tabs, 50 controls per tab: 29.69 ms before, 87.35 ms after (2.94x). Allocations rose from approximately 41.4 MiB to 103.9 MiB per redraw.
- Single-tab controls showed the same allocation counts and approximately 4% timing variation between versions.

The fixture uses a 160 by 30 screen, top tabs with the first tab selected, and a redraw requested by the first button in that tab. It does not perform terminal refresh/output. Each case warms up, then measures five batches; values are the median batch time and allocation count per redraw. Tiered compilation was disabled for both processes. This is a local diagnostic, not a BenchmarkDotNet result or a measurement of UICatalog frame rate.

The 20-tab/50-control fixture visited 50 controls per redraw before the fix and 1,000 after it. Page DrawingContent callbacks remained at zero in both versions. The additional work includes adornment regeneration, traversal of clean pages' SubViews, line resolution/composition, region calculations, and allocations.

The source of the added work is setting SubViewNeedsDraw on previously clean shared-canvas views. The change applies to all SuperViewRendersLineCanvas views, including tiled compositions, rather than only Tabs.

Caching clean border contributions, including reserved gaps, would avoid this extra traversal. Such a cache needs invalidation for geometry, focus, style/scheme, visibility, and other border changes.

Raw measurements: baseline.json, fixed.json, comparison.json. The standalone diagnostic source is Program.cs and its project is TabRedrawPerf.csproj. They are all ignored artifacts. The production source and fixed benchmark assembly were restored after measuring the baseline, and the Release library build passed.
