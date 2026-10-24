# Attack side-walk draw batching — approved bounded implementation

Start HEAD: 1de8ced. User approved the in-chat design and implementation, then separately approved turning the previous Deep Profile setting OFF. Verified stopped/idle/clean, recording OFF, Deep OFF after reload.

Final status: manual meshes failed pixel/order equivalence. With user approval, the implementation changed to native batching preserving original Sprite/Renderer identities. Tests 7/7 and pixel pairs 16/16 passed. One warm15/sample90 pair (128 enemies/15 towers) reduced Draw 1579.52 -> 1020.43 (35.40%). Candidate remains OFF: marker 3.812ms, GC +238200B/frame, and exceptional resource/restoration paths need correction. The tasks below were performed using this final native design; original mesh wording is historical. See README.md/native_summary.json. Further code changes or Unity runs require user approval.

Scope: Attack OriginalWalk five existing parts only. Original textures, Transform animation and mask sources remain. New Texture2DArray sampling plus combined meshes grouped by original sorting layer/order, source material and rendering layer. Foreign transparent overlaps at the same key fall back. Hit flash/unknown property blocks, unsupported cameras/materials and other roles/directions remain original. Existing A/B and SpriteAtlas holds remain.

Tasks:
- [ ] Add focused tests for eligible registration, shared draw groups, unsupported material/foreign-overlap fallback, and restoration.
- [ ] Implement Attack-only registration, common source array, GPU part matrices, camera-scoped source hiding and cleanup. Default candidate OFF until quality/performance evidence.
- [ ] Verify matching rendered pixels at representative side phases and overlaps. No redesign or source image modification.
- [ ] Short identical 128 enemies / 15 towers comparison: warm15/sample90, 1 observation each (bounded diagnostic, not statistical/target-device certification). Valid wrapped LastValue recorders only; no raw call-tree export, no Deep Profile. Automatic stop/restore on completion, error, or deadline. Record min/max/avg FPS, frame time, standard/SRP Draw, SetPass, batch CPU and memory delta.

Constraints: one combat scene, no scene YAML/settings edits, no builds/executable/push. Preserve existing uncommitted/untracked files. No automatic commit because AGENTS requires separate permission. Any new unexpected state/error follows the stop-and-ask rule; do not expand diagnostics.
