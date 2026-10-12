# Large tile center-to-center movement — 2026-10-08

- Implemented the rule recorded in Notion's `플레이어 전투 및 이동`: one key press moves from the current visible floor tile center to the adjacent tile center.
- B review scene: `Assets/Scenes/ArtReview_20260930_B_Revision2.unity` uses movement tiles of 2×2 base grid cells, origin (0,0).
- Player starts at world (7,5), logical tile origin (6,4). Child visual offset now only centers the feet on the movement anchor.
- Other scenes retain the component's default 1×1 movement unit. No scene YAML was edited manually.
- Existing press-once input and 0.12-second interpolation are retained.
- Every intermediate swept footprint and destination checks ground, enemy path, core and occupied tower cells. Moves cannot skip blocked cells.
- Three static review tower footprints were registered in the existing hidden occupancy map.
- Verification: initial regression harness exposed old half-slab movement; all 11 cases passed after implementation. Existing EditMode 1/1 and PlayMode 3/3 tests passed. Unity compile reported 0 errors and 0 warnings.
- Actual scene in Play mode: (7,5) → (7,7) → (7,5) completed; rightward crossing into the enemy path was rejected. Saved-scene reopening retained centered graphics.
- Windows executable was not rebuilt.
- Reusable check: `unity command run_script --file docs/art/TestSlabMovement.cs --entry TestSlabMovement.Main --format json`.
