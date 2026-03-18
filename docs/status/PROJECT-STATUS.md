# unityctl 프로젝트 상태

최종 업데이트: 2026-03-18 (KST)
기준 문서: `CLAUDE.md`, `docs/ref/phase-roadmap.md`, `docs/DEVELOPMENT.md`

## 현재 Phase

- **Phase 0 ~ 2C**: 완료
- **Phase 3B (Flight Recorder)**: 완료
- **Phase 3A (Session Layer)**: 완료
- **Phase 4A (Ghost Mode)**: 완료
- **Phase 3C (Watch Mode)**: 완료
- **Phase 4B (Scene Diff)**: 완료
- **Phase 5 (Agent Layer)**: 완료
- **Phase 1C (CI/CD)**: 완료

**전체 Phase 완료. MCP black-box / headless sample / exec contract 기반 검증 인프라 추가 완료.**

## 라이브 검증 (robotapp2, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| `ping` | ✅ | IPC + batch 모두 동작 |
| `status` | ✅ | isCompiling, isPlaying, platform 정상 |
| `check` | ✅ | 44 assemblies, scriptCompilationFailed 정상 |
| `build --dry-run` | ✅ | 19개 preflight 항목 검증, OutputPath 문제 정확히 감지 |
| `build` (실제) | ✅ | 프로젝트 에러 정확히 캡처 (AssetDatabase 런타임 사용) |
| `test --mode edit` | ✅ | 410개 실행, 403 pass / 7 fail (프로젝트 자체 실패) |
| `exec --code` | ✅ | IPC로 C# 식 실행 (`Application.version` → "0.1") |
| `schema --format json` | ✅ | 전체 커맨드 스키마 JSON 출력 |
| `session list` | ✅ | 세션 추적 + 기록 정상 |
| `log --stats` | ✅ | NDJSON 로그 기록/쿼리 정상 |
| `scene snapshot` | ✅ | 동작 확인 (Editor 1개일 때 정상 라우팅) |
| `watch --channel console` | ✅ | IPC 스트리밍 동작, heartbeat 수신 확인 |
| `editor list` | ✅ | 설치된 에디터 자동 탐색 |
| `init` | ✅ | manifest.json 플러그인 설치 |

## Write API 라이브 검증 (My project, Unity 6000.0.64f1)

| 기능 | 상태 | 비고 |
|------|------|------|
| `play start` | ✅ | `isPlaying=true`, `isPaused=false` |
| `play pause` | ✅ | `isPlaying=true`, `isPaused=true` |
| `play stop` | ✅ | 수정 후 `isPlaying=false`, `isPaused=false` |
| `player-settings get productName` | ✅ | `"My project"` 반환 |
| `player-settings get companyName` | ✅ | `"DefaultCompany"` 확인 |
| `player-settings set companyName` | ✅ | `"TestCo"` 설정 후 read-back 확인 |
| `player-settings set companyName` 복구 | ✅ | `"DefaultCompany"`로 복구 완료 |
| `asset refresh` | ✅ | `"Asset refresh scheduled"` 응답 후 IPC 재연결 확인 |
| `gameobject create` | ✅ | `globalObjectId`, `sceneDirty`, `undoGroupName` 반환 |
| `gameobject rename` | ✅ | `TestCube → NewName` 성공 |
| `gameobject move` | ✅ | 같은 씬 내 parent 변경 성공 |
| `gameobject delete` | ✅ | fresh object 기준 삭제 성공 |
| `gameobject activate/deactivate` | ✅ | alias 기준 `active=true/false` 정상 |
| `component add` | ✅ | `componentGlobalObjectId` 반환 확인 |
| `component set-property` vector | ✅ | `Transform.m_LocalPosition` 성공 |
| `component set-property` scalar | ✅ | `Rigidbody.m_Mass = 5` 성공 |
| `component remove` | ✅ | `Rigidbody` 삭제 성공 |
| `scene save` | ✅ | active scene 저장 성공 |
| `scene save --all` | ✅ | dirty scene 1개 저장 성공 |
| `PrefabGuard` | ✅ | prefab instance child delete에 대해 structured rejection 확인 |

검증 메모:

- `My project`는 테스트 전에 `unityctl init`으로 `com.unityctl.bridge`를 추가했다.
- `asset refresh`는 package/domain reload 후 한동안 IPC가 끊길 수 있으나, 실측에서 `ping` 재연결까지 확인했다.
- `player-settings set`은 실제 프로젝트 설정을 변경하므로 테스트 후 원래 값으로 복구했다.
- `gameobject set-active --active false`는 CLI option binding UX가 거슬려 `gameobject activate/deactivate` alias를 추가했다.
- `component set-property`는 human-friendly field name보다 Unity serialized property path 기준이다 (`mass`가 아니라 `m_Mass`).
- `SM_Bld_Apartment_02` prefab instance child에 대해 `Write operations on prefab instances are not supported in v1.` 메시지를 실측 확인했다.

## Plugin 호환성 수정 (Unity 6)

- `WatchEventSource`: `CompilationFinishedHandler` → `Action<object>` (Unity 6 API 변경)
- `WatchEventSource`: `EditorApplication.CallbackFunction` → `Action`
- `WatchEventSource.Subscribe`: 메인 스레드로 이동 (`EditorApplication.delayCall`)
- `IpcServer`: `Environment.TickCount64` → `(long)Environment.TickCount` (Mono 호환)
- `IpcServer.WatchWriterLoop`: 즉시 heartbeat 전송 (연결 안정성)

## 벤치마크 결과 (median, ms)

| 작업 | dotnet run | published exe | Unityctl.Mcp | CoplayDev MCP |
|------|-----------|---------------|--------------|---------------|
| ping | 2015 | 304 | 100 | 1 |
| editor_state | 2095 | 303 | 100 | 100 |
| active_scene | 2094 | 304 | 99 | 100 |
| diagnostic | 2053 | 401 | 101 | 100 |

Unityctl.Mcp resident mode는 `editor_state` / `active_scene` 기준 CoplayDev와 동등한 100ms대다.
단, clean-state 재실행에서는 `active_scene` 경로가 양쪽 모두 빈 문자열이었으므로 scene-path 자체를 비교 근거로 쓰지는 않는다.

## 자동화 검증

| 항목 | 상태 | 비고 |
|------|------|------|
| `dotnet build unityctl.slnx` | ✅ | 경고/오류 없이 통과 |
| `dotnet test unityctl.slnx -c Release` | ✅ | 현재 환경에서 exit 0, `Unityctl.Core.Tests`는 application control policy 영향 가능 |

| 프로젝트 | 통과 |
|----------|------|
| Unityctl.Shared.Tests | 60 |
| Unityctl.Core.Tests | 96 |
| Unityctl.Cli.Tests | 184 |
| Unityctl.Mcp.Tests | 11 |
| Unityctl.Integration.Tests | 23 |

테스트 인벤토리 기준 합계는 **374개**다 (Write API 추가).

신규 자동 검증:

- `Unityctl.Mcp.Tests`에 built `unityctl-mcp.exe` 기준 `initialize` / `tools/list` / `unityctl_schema` / invalid tool / missing arg black-box 테스트 추가
- `Unityctl.Integration.Tests`에 repo-contained `SampleUnityProject` 기반 closed-editor `status` / `check` / `test --mode edit` / `build --dry-run` 검증 추가
- `Unityctl.Shared.Tests`에 `ExecHandler` 실제 grammar/security/parse contract 테스트 추가
- `.github/workflows/ci-dotnet.yml`에 published CLI smoke (`--help`, `schema`, `tools --json`) 추가

Write API 실측 검증:

- `My project`에서 `play`, `player-settings`, `asset refresh` IPC write path 확인
- `play stop` 후 `isPaused=false`까지 정상화 확인
- `asset refresh` timeout 이슈 수정 후 `"scheduled"` 응답 + 후속 IPC 복구 확인
- `gameobject` Phase B (`create`, `rename`, `move`, `delete`, `activate/deactivate`) 실측 성공
- `component` Phase B.5 (`add`, `set-property`, `remove`) 실측 성공
- `scene save` / `scene save --all` 실측 성공
- `PrefabGuard` 실측 성공 (`SM_Bld_Apartment_02` prefab instance child delete / component add)

## 경쟁 우위 검증 결과

### 토큰 효율 (Codex 벤치마크)

| 항목 | unityctl | CoplayDev MCP | 배율 |
|------|----------|---------------|------|
| 스키마 크기 | 5,024 B (published MCP benchmark value) | 45,705 B | **9.1x 절감** |
| CLI schema 크기 | 11,927 B | 45,705 B | **3.8x 절감** |
| 단일 status 왕복 | 467 B (Mcp) / 481 B (CLI) | N/A (직접 비교 불가) | — |
| 10회 status 누적 | 9,694 B (Mcp) / 16,737 B (CLI) | N/A (직접 비교 불가) | — |
| CoplayDev에 build 도구 없음 | ✅ build/dry-run 있음 | ❌ 없음 | — |

주석:
- `5,024 B` 값은 published `Unityctl.Mcp tools/list` benchmark 기준이다.
- in-repo black-box harness는 `initialize` / `tools/list` correctness를 검증하지만 raw payload byte capture는 아직 후속 과제다.

### Headless CI/CD (경쟁자 불가 영역)

| 시나리오 | unityctl | CoplayDev |
|----------|----------|-----------|
| Editor 없이 status | ✅ repo `SampleUnityProject`에서 `Ready` 확인 | ❌ 실측 불가 |
| Editor 없이 check | ✅ repo `SampleUnityProject`에서 `assemblies=2` 확인 | ❌ 실측 불가 |
| Editor 없이 EditMode test | ✅ repo `SampleUnityProject`에서 `1 passed` 확인 | ❌ 실측 불가 |
| Editor 없이 dry-run | ⚠️ repo `SampleUnityProject`에서 structured `BuildFailed` (`OutputPath` not writable) | ❌ 실측 불가 |

해석:
- `unityctl`의 headless batch path는 repo에 포함된 재현 가능한 샘플 프로젝트에서 `status` / `check` / `EditMode test`까지 실측으로 검증되었다.
- `build --dry-run`은 batch crash가 아니라 structured preflight 결과까지 도달함이 확인되었다.
- CoplayDev 쪽은 public quickstart가 editor-first라 동일한 closed-editor batch parity를 이 저장소에서는 실측하지 않았다.

### exec 파워 데모 (80개 커맨드 vs exec 1개)

| Unity API 호출 | 결과 |
|----------------|------|
| `PlayerPrefs.GetString` | ✅ "none" |
| `PlayerSettings.companyName` | ✅ "DefaultCompany" |
| `PlayerSettings.productName` | ✅ "robotapp2" |
| `Application.unityVersion` | ✅ "6000.0.64f1" |
| `Application.dataPath` | ✅ 정확한 경로 |
| `Application.isPlaying` | ✅ false |
| 프로퍼티 체이닝 (예: `scenes.Length`) | ❌ 미지원 (한계) |

### 에러 복구 품질

| 시나리오 | 응답 |
|----------|------|
| 잘못된 빌드 타겟 | 유효 타겟 목록 포함 JSON 에러 |
| 존재하지 않는 프로젝트 | StatusCode 200 + 명확한 메시지 |
| 잘못된 exec 코드 | 보안 제한 메시지 + 허용 네임스페이스 안내 |

## 후속 과제

1. `docs/benchmark/` 산출물 커밋 및 README 링크 연결
2. MCP raw `tools/list` payload 캡처 하네스 추가
3. macOS / Linux 실제 테스트
4. GitHub Actions CI 실행 검증
5. `dotnet tool` NuGet 패키지 배포
6. exec 프로퍼티 체이닝 지원 개선
7. write API property alias 개선 (`mass` -> `m_Mass` 등)
