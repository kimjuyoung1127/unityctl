# unityctl 프로젝트 상태

최종 업데이트: 2026-03-18 (KST)
기준 문서: `CLAUDE.md`, `docs/ref/phase-roadmap.md`, `docs/DEVELOPMENT.md`

## 현재 Phase

- **Phase 0 ~ 1B**: 완료
- **Phase 1C**: 부분 완료
- **Phase 2A / 2A+**: 완료
- **Phase 2B**: 완료
- **Phase 2C**: 완료
- **Phase 3B**: 완료
- **Phase 3A**: 완료
- **Phase 4A**: 완료
- **Phase 3C**: 완료
- **Phase 4B**: 완료
- **Phase 5**: 완료 (2026-03-18)

## 이번 상태 반영 요약 (Phase 5 — Agent Layer)

1. **P0 Schema Command**: `unityctl schema --format json` → CommandSchema (version + commands[])
2. **P1 MCP Server**: `src/Unityctl.Mcp/` 신설 — ModelContextProtocol v1.1.0, stdio transport, 11개 도구
3. **P2 Exec Command**: `unityctl exec --project <path> --code <expr>` + ExecHandler (Plugin, Reflection 기반)
4. **P3 Workflow Runner**: `unityctl workflow run <file>` — 순차 실행, continueOnError 지원
5. WellKnownCommands 확장 (Schema, Exec, Workflow)
6. CommandCatalog 확장 (3개 신규 정의)
7. JsonContext 신규 타입 등록 (CommandSchema, WorkflowDefinition, WorkflowStep, EventEnvelope[])
8. Plugin WellKnownCommands 동기화 (Exec 추가)
9. `dotnet build unityctl.slnx` 통과 (경고 0)
10. `dotnet test unityctl.slnx` 통과 (304개)

## 자동화 검증

| 항목 | 상태 | 비고 |
|------|------|------|
| `dotnet build unityctl.slnx` | ✅ | 경고/오류 없이 통과 |
| `dotnet test unityctl.slnx` | ✅ | 총 304개 테스트 통과 |

테스트 세부:

| 프로젝트 | 통과 |
|----------|------|
| Unityctl.Shared.Tests | 60 |
| Unityctl.Core.Tests | 96 |
| Unityctl.Cli.Tests | 122 |
| Unityctl.Mcp.Tests | 7 |
| Unityctl.Integration.Tests | 19 |

## 수동 검증

기준 프로젝트: `C:\Users\gmdqn\robotapp`

| 시나리오 | 상태 | 비고 |
|----------|------|------|
| `unityctl init` | ✅ | `manifest.json`에 plugin source 추가 |
| 열린 Editor에서 `ping` | ✅ | `pong` 확인 |
| 열린 Editor에서 `status` | ✅ | `Ready` 확인 |
| 열린 Editor에서 `check` | ✅ | `Compilation check passed` 확인 |
| 열린 Editor에서 `test` (기본 wait) | ✅ | 폴링 후 404 passed, 27.7s |
| 열린 Editor에서 `test --no-wait` | ✅ | `ACCEPTED [104]` 즉시 반환 |
| 열린 Editor에서 `test --mode play` | ✅ | 경고 출력 + `ACCEPTED [104]` 즉시 반환 |
| 열린 Editor에서 `test --timeout 5` | ✅ | 타임아웃 후 `TestFailed` 반환 |
| single-flight (동일 test 2회) | ✅ | 두 번째 `Busy` 반환 |
| Unity 미실행 상태 batch fallback | ✅ | fallback 동작 확인 |
| 열린 Editor에서 `build` | ✅ | 실제 `BuildHandler` 응답 확인 |
| Unity 재시작 후 IPC 복구 | ✅ | 재시작 후 `ping/status` 회복 확인 |
| 도메인 리로드 후 자동 재연결 | 🔲 | 강한 재현/종결 검증이 아직 부족 |
| batch worker에서 IPC 미기동 확인 | 🔲 | 명시적 로그 검증 필요 |

## 남아 있는 후속 보강

- 도메인 리로드 후 IPC 자동 복구를 더 강하게 재현/종결 검증
- batch worker에서 IPC 서버가 뜨지 않는다는 점을 로그로 명시 검증
- pure IPC latency를 CLI 프로세스 시작 비용과 분리해서 다시 측정

## 즉시 다음 작업

1. Phase 2B 후속 보강 (domain reload, batch IPC 미기동 로그, latency 측정)
2. Phase 1C 잔여 (release.yml, README)
