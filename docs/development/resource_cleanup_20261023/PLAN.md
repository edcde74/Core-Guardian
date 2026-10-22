# Sprite resource cleanup plan

Goal: remove unreferenced sprite/image assets from Unity Assets, after a verified local recovery commit; report reusable candidates separately. Preserve current scene, runtime resource loads, animations, art, settings and user files.

Scope: image source assets and their .meta only. No audio/font/code/material/scene deletion. Existing serialized references, code resource loads, editor/test direct paths and the user's atlas are protection evidence. Uncertain references are retained and reported.

- [ ] Audit live scene dependencies, all serialized asset dependencies, runtime/editor/test resource paths and atlas inputs. Save the exact keep/delete manifest and hashes outside Assets.
- [ ] Preserve current unsaved scene state and record a real pre-deletion local commit covering every proposed deletion, including its .meta. Record unrelated settings hashes without committing privacy/settings changes.
- [ ] Report high-reuse candidates with reasons and recovery paths. Delete only the verified manifest through Unity AssetDatabase, with state/hash/reference guards.
- [ ] Verify kept image hashes, dependency closure, dynamic resource paths, missing references and unchanged scene/settings state. No broad gameplay/performance rerun or build.
- [ ] Update handover and completion ledger; commit cleanup and evidence locally. No push.

Ruling: current serialized references anywhere in Assets count as connected, including prefabs/animations and user recovery scenes. This may retain more images than a build-only sweep but prevents breaking connected assets. Editor/test path-only references are also retained. Reusable unconnected images remain recoverable in Git and are reported before removal.
