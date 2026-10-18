# Final build benchmark verification

Reran the same six-case standalone harness after the issue #5360 culling-cache correction, using the final Release build from commit 159d63c79. The measured library SHA-256 is 0EC66AEBBD005E67D4F1DF7B49A8D8D55C3BEA771199F1F0FA37487784F70DEA, matching the current library build.

The final version and the cached version before the culling correction ran back-to-back. The correction is confined to the conservative cull branch; shared tab canvases bypass that branch.

| Tabs | Controls/tab | Before culling correction, ms/redraw | Final, ms/redraw | Allocated MiB/redraw (identical) | Control visits/redraw (identical) |
|---:|---:|---:|---:|---:|---:|
| 1 | 1 | 22.22 | 21.05 | 38.09 | 1 |
| 1 | 50 | 25.54 | 25.36 | 39.12 | 50 |
| 4 | 1 | 27.32 | 26.16 | 40.67 | 1 |
| 4 | 50 | 29.58 | 28.09 | 41.88 | 50 |
| 20 | 1 | 37.57 | 35.74 | 56.27 | 1 |
| 20 | 50 | 43.18 | 39.16 | 58.23 | 50 |

All six cases have exactly the same allocated bytes and control-visit counts before and after the culling correction. Page content redraw counts remain zero. No timing regression was observed in the paired run; small timing differences should not be attributed to the correction.

The largest final case is 39.16 ms/redraw, comparable to the previous cached measurement of 39.68 ms/redraw. Relative to the earlier original-correctness-fix measurement (88.79 ms/redraw and 103.93 MiB/redraw), the final case still shows approximately 56% less redraw time and 44% fewer allocated bytes. Control visits remain 50 instead of 1,000.

An initial final-build run (final.json) was slower while host CPU load was elevated (sampled at 79%). The paired runs were performed to check against the previous binary under current conditions; a later CPU-load sample was 22%. This is a local synthetic in-memory benchmark, not a measurement of terminal frame rate.

Raw paired measurements: pre-cull-fix-paired.json and final-paired.json. Initial run: final.json. Final Release benchmark build log: build-final-benchmark.log. Same warmup, five-batch median, screen size, disabled tiering, and disabled driver I/O as optimized-results.md.
