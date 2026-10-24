# PR #5729 review follow-up

Verified and switched the checkout from `feat/custom-file-dialog-icons` to `fix/tab-partial-redraw-borders` before editing. The reviewed control is the PR head `159d63c79`. The final measured library SHA-256 is `6A6318B46C66339449A4F683DFE7ED160FF9804B11944F0E4353A663540AC68F`.

## Review dispositions

1. **Content-generated lines on SubView-only redraws — fixed.** Track whether text/content drawing mutates the line canvas, including lines, exclusions, and reservations. Such views conservatively redraw their own content on a SubView-only pass instead of publishing an adornments-only cache. Ordinary Tabs pages retain the narrow redraw path. Repeated-redraw regressions cover tiled and overlapped views.
2. **Cache identity and inherited visual state — strengthened.** Match screen frame, viewport, composition mode, theme generation, scheme, effective border attribute, focus, enabled state, title, border style, and thickness. The theme generation uses the existing facade subscription, not a per-cache handler. Tests cover SuperView scheme changes, custom effective attribute changes without dirty flags, enabled/focus changes, and theme notification with clean shared views. Theme tests use the existing nonparallel static-settings collection and do not change global theme settings.
3. **Tiled-layout cost — measured.** Added a fixture with 1/6/24 shared FrameViews, 1/10 controls per frame, and a shared Line inside each frame. Junctions share boundaries; only the first button is dirtied. Clean frames' controls are not visited, and partial-redraw glyphs match both initial and forced-full rendering. Whole-canvas merging/resolution still scales with the tiled layout size; this change does not eliminate that cost.
4. **Extra canvas/event subscriptions — removed.** A lightweight `LineCanvasSnapshot` holds line references, cloned exclusions, and reserved cells. It does not construct another LineCanvas or register another static ThemeChanged handler. The subscription test detects the extra handler on the old binary and none on the final one, including remove/re-add. TitleView still participates in the same cache/composition pipeline; its frequently rebuilt snapshot no longer allocates a canvas/resolver set or handler.
5. **Disposal — fixed.** Clear the cache reference on View disposal; snapshots need no event unsubscription.
6. **Tiled reservations — clarified and tested.** Reservations are retained in the snapshot and no longer cause repeated clean-view regeneration. As before, tiled auto-join does not apply the independently-resolved overlapped reservation policy. Manual additions preserve existing overlapped reserved gaps.
7. **Direct Draw duplication — fixed.** Publish the cache after DrawComplete, retain the pending-draw/version state, and consume that contribution without regenerating its borders. A direct draw's seven lines remain seven on the next SuperView pass rather than fourteen. DrawComplete-supplied lines are included on dirty, direct, and clean passes.
8. **Lazy creation — tightened.** Create/update a cache when a shared view actually publishes a drawn contribution; clean composition reuses it.
9. **Culling comment — corrected.** The comment explicitly describes mirroring Draw's invalidation while skipping Draw.

Additional coverage includes a 24-view tiled grid with Line/FrameView junctions and scrolled viewport, transparent overlapped shared output with/without a shadow, a Popover over tiled views with a partial redraw underneath, Z-order changes, same-frame thickness changes, remove/re-add, and no leftover draw flags. Transparent-shadow glyphs and line attributes are compared against full redraw; margin color blending has existing buffer-dependent differences and is not claimed fixed here.

## Final benchmark

| Layout | Views | Controls/view | Before review fixes, ms/redraw | Final, ms/redraw | Allocated MiB/redraw, before → final | Control visits/redraw |
|---|---:|---:|---:|---:|---:|---:|
| tabs | 1 | 1 | 20.61 | 18.92 | 38.092 → 38.097 | 1 |
| tabs | 1 | 50 | 20.69 | 19.66 | 39.117 → 39.122 | 50 |
| tabs | 4 | 1 | 22.27 | 21.55 | 40.670 → 40.679 | 1 |
| tabs | 4 | 50 | 22.98 | 21.53 | 41.885 → 41.894 | 50 |
| tabs | 20 | 1 | 30.49 | 29.83 | 56.270 → 56.299 | 1 |
| tabs | 20 | 50 | 34.79 | 32.59 | 58.228 → 58.258 | 50 |
| tiled | 1 | 1 | 0.19 | 0.19 | 0.236 → 0.238 | 1 |
| tiled | 1 | 10 | 0.24 | 0.25 | 0.324 → 0.325 | 10 |
| tiled | 6 | 1 | 1.17 | 1.17 | 1.369 → 1.376 | 1 |
| tiled | 6 | 10 | 1.16 | 1.24 | 1.533 → 1.539 | 10 |
| tiled | 24 | 1 | 6.58 | 6.90 | 6.839 → 6.863 | 1 |
| tiled | 24 | 10 | 6.79 | 6.94 | 7.017 → 7.041 | 10 |

Largest Tabs case: 34.79 → 32.59 ms/redraw. Largest tiled case: 6.79 → 6.94 ms/redraw (about 2.2% slower in this paired run). Exploratory runs in both orders gave 7.01 → 6.90 ms for that tiled case, demonstrating similar run-to-run timing variation. Do not attribute small timing differences to an improvement/regression from the final correction. The stronger key/snapshots add approximately 0.02–0.03 MiB/redraw in the largest fixtures (under 0.4%). Control visit counts are unchanged; all measured page-content draw counts are zero.

These are local synthetic in-memory measurements with the same 160 × 30 screen, Release build, disabled tiering/real driver I/O, 30 warmups, ten calibration iterations, and five measured batches. Iterations per batch are calibrated from 10 to 200, as in the original harness. They exclude layout and terminal refresh/output and do not measure terminal frame rate. No tests or recordings ran concurrently. The final paired rows compare the newly built old PR head against the final working-tree library; earlier exploratory runs are supplied separately and excluded from the final table.

Raw final measurements: `review-final-tabs-before.json`, `review-final-tabs-after.json`, `review-final-tiled-before.json`, `review-final-tiled-after.json`. `Program.cs` and `TabRedrawPerf.csproj` reproduce both fixtures; pass a second argument of `tiled` to select the tiled fixture. Download them into `artifacts/tab-redraw/perf/` for the relative project reference to resolve. The harness checks partial/full glyph equality after the timed batches.

## Validation

- Release solution build: passed; no new warnings in changed code. Whitespace diff check passed.
- 38 targeted cache/theme tests passed. Against the old PR binary, 10 fail: content lines (2), effective attribute changes (2), direct-draw duplication (2), theme refresh (2), extra subscription (1), retained manual reserved gaps (1). Restored the final library after the control run.
- Final full parallel run: 17,981 passed, 16 skipped, 5 failed. Four are the previously confirmed terminal-color failures. The additional Windows wide-glyph output test also differed in ANSI color encoding; its complete AllDriverTests class passed when rerun (18 passed). An earlier full run had only the four known failures. No claim that the full suite is green.
- Nonparallel suite: 33 passed.
- Integration suite: 438 passed.
- Fresh tuirec capture: `review-final.gif` / `review-final.cast`, mouse movement, focus navigation, and header clicks. Read the raw cast and inspected coalesced GIF frames before and after tab selection: borders and selected openings remain stable.

Known full-suite failures: `ViewDrawingClippingTests.Draw_WithBorderSubView_DrawsCorrectly`, `SchemeColorNoneDerivationTests.Focus_WithDarkTerminal_RendersResolvedColors`, `Focus_WithNoDefaultAttribute_FallsBackToBlackAndWhite`, `Focus_WithWhiteTerminal_RendersResolvedColors`, and the transient Windows case of `All_Drivers_When_Clipped_AddStr_Glyph_On_Second_Cell_Of_Wide_Glyph_Outputs_Correctly`.
