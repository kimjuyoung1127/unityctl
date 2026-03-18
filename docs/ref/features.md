# unityctl 기능 요약

Unity 에디터를 터미널과 AI로 제어하는 CLI + MCP 도구.

## 에디터 제어
- 연결 확인 (ping, status), 컴파일 체크, 설치된 에디터 탐색
- 플레이 모드 제어 (start / pause / resume / stop)
- 플레이어/프로젝트 설정 읽기·쓰기

## 씬 & 오브젝트
- GameObject 생성·삭제·이름변경·이동·활성화
- Component 추가·제거·속성 변경
- 씬 열기·생성·저장
- 씬 스냅샷 & 속성 단위 diff

## 에셋 & 프리팹
- 에셋 생성·복사·이동·삭제·리임포트·폴더 생성
- 프리팹 생성·적용·언팩·속성 수정

## 머티리얼 · 애니메이션 · UI
- 머티리얼 속성·셰이더 변경
- 애니메이션 클립·컨트롤러 생성
- Canvas·UI 엘리먼트 생성, RectTransform 조절

## 빌드 & 테스트
- 멀티 플랫폼 빌드 (Win/Mac/Linux/Android/iOS/WebGL)
- 고스트 모드 (--dry-run) — 빌드 전 19개 항목 사전 검증
- EditMode / PlayMode 테스트 실행

## 패키지 · Undo · Exec
- 패키지 추가·제거·목록
- Undo / Redo (모든 write 명령 Ctrl+Z 가능)
- 임의 C# 코드 실행 (exec)

## 모니터링
- 실시간 스트리밍 (콘솔 로그, 컴파일, 하이러키 변경)
- 비행 기록기 — 모든 명령 이력·소요시간·에러 자동 기록
- 세션 관리

## AI 에이전트 연동 (MCP)
- MCP 서버 내장 (13개 도구)
- 39개 write 명령 allowlist (unityctl_run)
- 스키마 온디맨드 조회 — AI가 사용 가능한 명령을 스스로 탐색

## 인프라
- IPC 통신 ~100ms / 배치 폴백 (CI·헤드리스)
- Windows · macOS · Linux
- 총 71개+ 명령 (read 32 + write 39)
