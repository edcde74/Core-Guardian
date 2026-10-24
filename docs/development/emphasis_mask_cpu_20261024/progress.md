# 실행 기록

- 승인: 격리512조합 원본/후보1회, 기존원본3회조건확인후재사용, 후보warm15/sample90×3, 실패/효과부족시고정두사례최대8렌더 또는 일반Profiler warm15/sample90 1회와상위2프레임제한분석. 추가구조수정은원인/변경안을먼저보고. 예상60~90분. 필요한재로딩·자체Play/종료·임시1920선택·복원포함. 빌드/커밋/push없음.
- HEAD1de8ced, 시작제품2파일이직전시작원본과hash동일. Unity idle/clean7roots·Play/Profiler/Deep/frameDebuggerOFF·1920×1080/nativeBatchOFF. 다른검사실행없음.
- 실행계획: 기존2026-10-24-mask-cpu-preparation의A와최신격리도구를사용. 원본반복결과는같은instrumented기준/마커/부하일때만재사용.
- Ruling: 최신512도구는본체의모든Renderer를mask준비뒤끄고허용출력집합과활성집합일치를강제한다. 각사례rawRGBA/PNG를저장해hash만남기던문제를방지. 제품출력/아트정책변경은없다.
- 격리512조합 원본/후보 모두완료,mask/geometry/rect/outputhash차이0·예상밖Renderer0·빈후보출력0. 전체body합성/HUD통과로확대하지않음. 제품후보hash는직전47테스트/두사례검사보존본과동일;무관한기통과테스트재실행하지않음.
- 후보3회90개/조건정상/복원완료. Mask평균절감약0.333ms로0.5ms목표미달. CacheLookup/Output감소와BoundsCrop증가가상쇄. sample stride144B·capacity1092·배열payload157248B(객체헤더제외). 측정용후보보존뒤진단마커만추가해일반Profiler1회예정. 준비/분석스크립트실행전문법오류와hook검사는수정했으며추가측정은없음.

2026-10-24: User explicitly approved B1/B2 implementation, regressions, per-stage512 once and warm15/sample90 x3, necessary reload/own Play/temp1920/restore. B1 new input test RED: missing prepared Sprite property (1 expected failure).
B1 completed:512 exact/45 tests including runtime ownership;3x90 valid. Mask approximately4.62ms vs4.877ms, below0.5ms goal; rejected and original restored. B2 evaluated independently on original, not bundled.
B2 completed:3 RED->GREEN plus51 regressions;512 exact;3x90 valid. Mask4.455327/output1.043021ms;0.422060 total reduction, below0.5 goal and third-run overlap. Both rejected/original restored;recompile successful;10protected match;Play/compile/profilerOFF/clean7roots/buffer0..11 preserved. Final independent review: no critical/important issues; coverage limitations in B1_B2_RESULTS.md. No new failures/retries, builds/commits/push.
