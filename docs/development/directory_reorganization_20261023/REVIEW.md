# 독립 읽기 검토

이동 전 검토에서 RouteMeshBuilder의 구형 CombinedRoute 출력 경로가 자동 치환으로 처리되지 않는 것을 찾아 메시/재질 폴더로 나눴다. BossEye PNG와 Contact 데이터도 각각 Art/Data로 분리했다.

최종 검토 결과 차단 이슈 없음. 이동표552개 목적지 존재·원본/meta552개 보존, 허용 목록 밖 콘텐츠 변경0. 경로 변경 테스트9개는 원본에 이동표를 치환한 결과와 정확히 같다. RouteMeshBuilder는 출력 경로/파일명 구성만 변경됐다. 일반 메시/재질12개는 m_Name 외 직렬화 데이터 동일하다.

asmdef4개 콘텐츠 동일, Domain/Tests/global 코드 경계 유지. 현재9개 분류에서 소리/폰트/셰이더 오분류 없음. TilePalette prefab은 용도에 맞게 Art/Environment에 있다. 런타임 C#의 구형 Assets 경로 잔존 없음.

이는 별도 읽기 검토이며 Unity/Play/테스트를 추가 실행하지 않았다. 메인 실행자의 Resources227·assembly102·mask92·의존성552 및 EditMode22 통과를 독립 재실행한 것으로 확대하지 않는다.
