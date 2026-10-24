# 윤곽 샘플링 작업 기록

- 승인: OUTLINE_PLAN의 제품 shader1개, 대상 픽셀/호출수 검사와 적128/타워15 원본·후보 각90프레임. 빌드/실행파일/push/커밋/보류 후보 활성화 없음.
- HEAD1de8ced, codex/shared-body-mask. 기존 미커밋과 같은 연결 Unity를 유지해야 하므로 새 worktree/Unity 인스턴스 생성 없이 해당 shader만 분리한 원본 사본+hash로 보호한다.
- 기존 C# 제품 코드는 이번 수정 대상이 아니다. Shader 원본 diff 없음 확인, scene/설정5파일 hash 저장.
- 초기 실제 상태: stopped/idle/clean, Deep/recording/FrameDebugger OFF, GPU Profiler supported=false(OS=true)/D3D12. GPU module을 켜거나 API를 바꾸지 않는다.
- Ruling: TDD의 실패 질문은 출력 변경이 아니라 중복 샘플/불필요 순회 축소다. 기존 shader 본문을 테스트에서만 계측한 GPU 렌더로 논리적 샘플 호출수 계약을 먼저 실패시킨다. 계측판은 GPU 하드웨어 fetch수/실제 성능 증거가 아니며, 실제 제품 출력은 별도 원본/후보 픽셀 비교로 검사한다.
- Ruling: 테스트용 원본 shader 이름만 고유 Hidden 이름으로 바꿔 저장한다. 계측 shader는 ShaderUtil 메모리 컴파일/파괴로 제한, 추가 제품 keyword나 실행 중 계측 분기는 넣지 않는다.
- 원본 일반90프레임 시작. 이후 결과와 단계별 기록을 이어 적는다.
- 원본 정상90/gap0/종료복원. 새 테스트4개 원본2pass2fail→중앙3pass1fail→조기종료4pass. 원본/중앙/최종 소스 각각 보존.
- 실제 원본 Attack/Buff4방향×4위상×단독/겹침64쌍, 강조1px+선택2px 중첩 차이0. 실제mask Point/Clamp/mip없음 확인. 기존 관련2case pass.
- 후보 일반90/gap0/종료복원. 평균23.053→24.653ms(+6.94%), 평균FPS43.38→40.56(-6.49%), 후보GC수거1/3.49MB급증. GPU 미지원으로 shader 인과효과 확정 불가. 추가측정 없음.
- 계획의 악화 기준대로 제품shader 원본사본으로 바이트복구/import확인. 중앙-only는 별도 실전 성능 미측정이라 채택하지 않음.
- 독립review의 고립float neighbor검사 공백을 보완. 후보를 고유 Hidden 이름 테스트fixture로 격리하고 해당fixture대상5/5pass. Runtime에 후보를 켠 상태라고 하지 않음.
- 최종실제상태 stopped/clean7roots/Profiler관련OFF/시간복원/nativebatchOFF/mask entries·bindings·pages0. 제품shaderdiff0, 원본SHA일치. 보존·링크검사와 인수인계 갱신 후 종료.
