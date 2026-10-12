# B comparison placement alignment — 2026-10-08

- Scene: `Assets/Scenes/ArtReview_20260930_B_Revision2.unity`.
- Visible stone slab: 2×2 existing grid cells. This is an art placement reference, not a new movement or installation rule.
- Single, multi and support tower base centers: (9,9), (13,9), (17,9). Core base center: (15,5).
- Base offsets were measured from opaque pixels in the lower sprite region, so the long single-tower barrel does not bias its base alignment.
- Player starting feet: (7,5). Logical movement anchor remains the center of cell (6,5), at (6.5,5.5).
- Player rendering moved to `PlayerVisual_Centered`; the original renderer is disabled. Existing movement code remains unchanged and the scene's old `spriteOffset` is zero. The child carries the visual offset through movement.
- Initial alignment only centered the composition. Subsequent center-to-center movement implementation is recorded in `slab_movement_20261008.md`; the B scene now uses 2×2 movement tiles.
- B sprite scales, camera, floor tiles and route are unchanged. No Windows build was regenerated.
- Verification: editor script compiled without diagnostics; four base centers and starting feet passed coordinate checks in Edit mode, Play mode, and after reopening the saved scene.
- Screenshots: `alignment_before_20261008.png`, `alignment_after_20261008.png`, `alignment_play_20261008.png`.
- Operation: `CenterArtOnSlabs.cs`; coordinate report: `tile_center_alignment_20261008_report.json`.
