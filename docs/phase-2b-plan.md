# Phase 2B 구현 플랜: IPC Transport + 프로세스 감지

## Context

현재 unityctl은 모든 커맨드를 **배치모드**로 실행합니다 (Unity를 새로 스폰 → 30~120초 지연). Phase 2B는 이미 실행 중인 Unity Editor와 **Named Pipe/UDS**로 통신하여 응답 시간을 **<200ms**로 단축하는 것이 목표입니다.

Phase 2A에서 이미 `ITransport` 인터페이스, `IpcTransport` 스텁, `CommandExecutor`의 IPC 우선 로직(주석), `Constants.GetPipeName()` 등 기반이 마련되어 있습니다.

---

## 구현 단계

### Step 1: Plugin IpcServer 구현
**파일**: `src/Unityctl.Plugin/Editor/Ipc/IpcServer.cs`

현재 빈 클래스 → 완전한 Named Pipe 서버로 구현

```
IpcServer
├── Start(string projectPath)  — 파이프 서버 시작
├── Stop()                     — 서버 중지 + 스레드 정리
├── IsRunning { get; }         — 상태 확인
└── (내부) ListenLoop()        — 백그라운드 스레드에서 실행
```

**핵심 설계:**
- `NamedPipeServerStream` 사용 (Windows: Named Pipe, Linux/macOS: .NET이 자동으로 UDS 사용)
- **동기 메서드만 사용** (Unity는 비동기 파이프 메서드 미구현 — 컴파일은 되지만 런타임 에러)
- 백그라운드 `Thread`에서 `WaitForConnection()` → `Read()` → `Write()` 루프
- 메시지 프레이밍: **4바이트 길이 헤더 (little-endian) + UTF-8 JSON 바디**
- 커맨드 디스패치: 기존 `IpcRequestRouter.Route(request)` → `CommandRegistry.Dispatch()` 재사용
- Unity API 호출이 필요한 커맨드는 `EditorApplication.update`를 통해 메인 스레드로 디스패치
- `ConcurrentQueue<Action>` 사용하여 스레드간 통신

**파이프명**: `Constants.GetPipeName(projectPath)` 사용 (SHA256 해시 기반, 이미 구현됨)

**수명 관리:**
- `EditorApplication.quitting` → Stop() 호출
- `AssemblyReloadEvents.beforeAssemblyReload` → Stop() 호출 (도메인 리로드 대응)
- `AssemblyReloadEvents.afterAssemblyReload` → Start() 재시작

**참조 소스:**
- 웹 리서치 결과: Unity에서 Named Pipe 사용 시 동기 메서드 + 백그라운드 스레드 패턴 검증됨
- [NamedPipeServerStream in Unity](https://discussions.unity.com/t/namedpipeserverstream-in-unity/811116)
- [EditorDispatcher 패턴](https://gist.github.com/LotteMakesStuff/f1e7a0fbcb05adcbd017d6f4f0913264)

### Step 2: UnityctlBootstrap에서 IpcServer 시작
**파일**: `src/Unityctl.Plugin/Editor/Bootstrap/UnityctlBootstrap.cs`

현재:
```csharp
static UnityctlBootstrap() {
    CommandRegistry.Initialize();
    Debug.Log(...);
}
```

변경:
```csharp
static UnityctlBootstrap() {
    CommandRegistry.Initialize();
    IpcServer.Instance.Start(GetProjectPath());
    Debug.Log(...);
}
```

- `Application.dataPath`에서 프로젝트 경로 추출: `Path.GetDirectoryName(Application.dataPath)`
- **Plugin Constants 처리**: Plugin은 `Unityctl.Shared`를 직접 참조 불가 (Unity 프로젝트).
  `IpcServer` 내부에 `GetPipeName()` 로직 복제 필요 (SHA256 해시 + `unityctl_` 접두사, ~10줄)
  → 또는 `src/Unityctl.Plugin/Editor/Ipc/PipeNameHelper.cs` 새 파일로 분리

### Step 3: Core IpcTransport 구현
**파일**: `src/Unityctl.Core/Transport/IpcTransport.cs`

현재 스텁 → Named Pipe 클라이언트로 구현

```csharp
public async Task<CommandResponse> SendAsync(CommandRequest request, CancellationToken ct)
{
    var pipeName = Constants.GetPipeName(_projectPath);
    using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
    await client.ConnectAsync(Constants.IpcConnectTimeoutMs, ct);

    // 요청 직렬화 (System.Text.Json)
    var requestJson = JsonSerializer.Serialize(request, UnityctlJsonContext.Default.CommandRequest);
    var requestBytes = Encoding.UTF8.GetBytes(requestJson);

    // 길이 헤더 + 바디 전송
    var lengthBytes = BitConverter.GetBytes(requestBytes.Length);
    await client.WriteAsync(lengthBytes, ct);
    await client.WriteAsync(requestBytes, ct);
    await client.FlushAsync(ct);

    // 응답 수신: 길이 헤더 읽기 → 바디 읽기
    var responseLengthBytes = new byte[4];
    await ReadExactAsync(client, responseLengthBytes, ct);
    var responseLength = BitConverter.ToInt32(responseLengthBytes);

    var responseBytes = new byte[responseLength];
    await ReadExactAsync(client, responseBytes, ct);
    var responseJson = Encoding.UTF8.GetString(responseBytes);

    return JsonSerializer.Deserialize(responseJson, UnityctlJsonContext.Default.CommandResponse)
        ?? CommandResponse.Fail(StatusCode.UnknownError, "Failed to deserialize IPC response");
}

public async Task<bool> ProbeAsync(CancellationToken ct)
{
    // 파이프 연결만 시도하여 서버 존재 확인 (ping 전송 불필요)
    // ConnectAsync 실패 = 서버 없음 → 즉시 false 반환 (타임아웃 대기 없음)
    try {
        var pipeName = Constants.GetPipeName(_projectPath);
        using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(1000); // 1초 내 연결 시도
        await client.ConnectAsync(cts.Token);
        // 연결 성공 = 서버 존재 확인. 연결은 SendAsync에서 다시 생성.
        return true;
    } catch { return false; }
}
```

**성능 고려사항:**
- `ProbeAsync()`는 파이프 연결만 시도 (1초 타임아웃). ping 요청을 보내지 않음
- 파이프가 없으면 `ConnectAsync`가 즉시 실패 → 배치 폴백 지연 없음
- `SendAsync()`에서 새 연결 생성 (연결당 1회 요청-응답 모델 — 심플하고 안전)

**재사용**: `Constants.GetPipeName()`, `Constants.IpcConnectTimeoutMs` (5초), `UnityctlJsonContext`

### Step 4: Platform별 FindRunningUnityProcesses 구현
**파일들**:
- `src/Unityctl.Core/Platform/WindowsPlatform.cs` — `FindRunningUnityProcesses()`
- `src/Unityctl.Core/Platform/LinuxPlatform.cs` — `FindRunningUnityProcesses()`
- `src/Unityctl.Core/Platform/MacOsPlatform.cs` — `FindRunningUnityProcesses()`

**Windows**: `Process.GetProcessesByName("Unity")` → 각 프로세스의 커맨드라인에서 `-projectPath` 인자 파싱
- `System.Management`의 WMI 대신 `/proc` 스타일이 아닌 `Process.MainModule.FileName` + 커맨드라인 확인
- 또는 단순하게 `UnityLockfile` 존재 여부 + 파이프 `ProbeAsync()`로 대체 (더 안정적)

**Linux**: `/proc/{pid}/cmdline` 파싱으로 Unity 프로세스와 `-projectPath` 추출

**macOS**: `ps aux | grep Unity` 동등한 `Process.GetProcessesByName` 사용

**대안 접근 (권장)**: 프로세스 감지를 복잡하게 구현하는 대신, `CommandExecutor`에서 **파이프 Probe만으로 판단**:
1. `IpcTransport.ProbeAsync()` → ping 성공? → IPC 사용
2. 실패 → Batch 폴백

이 방식이 더 신뢰성 있음 (프로세스는 있지만 IPC 서버가 안 떴을 수도 있으므로)

### Step 5: CommandExecutor IPC 우선 로직 활성화
**파일**: `src/Unityctl.Core/Transport/CommandExecutor.cs`

현재 주석 처리된 코드 활성화:

```csharp
private async Task<CommandResponse> ExecuteOnceAsync(
    string projectPath, CommandRequest request, CancellationToken ct)
{
    // Phase 2B: IPC 먼저 시도
    await using var ipc = new IpcTransport(projectPath);
    if (await ipc.ProbeAsync(ct))
        return await ipc.SendAsync(request, ct);

    // 폴백: 배치 트랜스포트
    await using var batch = new BatchTransport(_platform, _discovery, projectPath);
    return await batch.SendAsync(request, ct);
}
```

**BatchTransport의 ProjectLocked 처리 변경**: 현재는 프로젝트가 잠기면 에러를 반환하지만, IPC가 활성화되면 잠김 = 에디터 실행 중 = IPC 가능. `CommandExecutor`가 IPC를 먼저 시도하므로 `BatchTransport`의 잠금 체크는 그대로 유지 (IPC 실패 시 배치 폴백인데, 잠겨있으면 배치도 불가 → 정확한 동작)

### Step 6: IPlatformServices.CreateIpcClientStream 구현 (선택적)
**파일들**: `WindowsPlatform.cs`, `LinuxPlatform.cs`, `MacOsPlatform.cs`

현재 `throw NotImplementedException`. IpcTransport가 직접 `NamedPipeClientStream`을 생성하므로, 이 메서드가 실제로 필요한지 재검토:
- IpcTransport에서 직접 파이프 클라이언트 생성 → `CreateIpcClientStream` 불필요
- 인터페이스에 있으므로 심플하게 구현하거나, IpcTransport가 직접 생성하는 것으로 통일

**권장**: IpcTransport가 직접 `NamedPipeClientStream` 생성. `CreateIpcClientStream`은 나중에 필요시 활용.

### Step 7: 테스트 작성
**새 파일**: `tests/Unityctl.Core.Tests/Transport/IpcTransportTests.cs`

```
- IpcTransport_ProbeAsync_NoServer_ReturnsFalse
- IpcTransport_SendAsync_NoServer_ThrowsOrFails
- IpcTransport_MessageFraming_RoundTrip (로컬 파이프 서버 mock)
- CommandExecutor_FallsBackToBatch_WhenIpcUnavailable
```

**새 파일**: `tests/Unityctl.Core.Tests/Discovery/UnityProcessDetectorTests.cs`
```
- IsEditorRunning_NoProceses_ReturnsFalse
- FindProcessForProject_NoMatch_ReturnsNull
```

기존 59개 테스트 유지 + 신규 테스트 추가

### Step 8: Plugin에 메시지 프레이밍 유틸 추가
**새 파일**: `src/Unityctl.Plugin/Editor/Ipc/MessageFraming.cs`

```csharp
public static class MessageFraming
{
    public static byte[] Frame(string json)  // 4바이트 길이 + UTF-8
    public static string ReadFrame(Stream stream)  // 동기 읽기
}
```

CLI/Core 측에도 동일 유틸 필요 → `src/Unityctl.Core/Transport/MessageFraming.cs`

---

## 수정 파일 요약

| 파일 | 변경 유형 | 설명 |
|------|----------|------|
| `src/Unityctl.Plugin/Editor/Ipc/IpcServer.cs` | **대폭 수정** | 빈 클래스 → 전체 구현 |
| `src/Unityctl.Plugin/Editor/Ipc/MessageFraming.cs` | **신규** | 메시지 프레이밍 유틸 |
| `src/Unityctl.Plugin/Editor/Bootstrap/UnityctlBootstrap.cs` | 수정 | IpcServer 시작 추가 |
| `src/Unityctl.Core/Transport/IpcTransport.cs` | **대폭 수정** | 스텁 → 전체 구현 |
| `src/Unityctl.Core/Transport/MessageFraming.cs` | **신규** | Core 측 메시지 프레이밍 |
| `src/Unityctl.Core/Transport/CommandExecutor.cs` | 수정 | IPC 우선 로직 활성화 |
| `src/Unityctl.Core/Platform/WindowsPlatform.cs` | 수정 | FindRunningUnityProcesses 구현 |
| `src/Unityctl.Core/Platform/LinuxPlatform.cs` | 수정 | FindRunningUnityProcesses 구현 |
| `src/Unityctl.Core/Platform/MacOsPlatform.cs` | 수정 | FindRunningUnityProcesses 구현 |
| `tests/Unityctl.Core.Tests/Transport/IpcTransportTests.cs` | **신규** | IPC 트랜스포트 테스트 |
| `tests/Unityctl.Core.Tests/Discovery/UnityProcessDetectorTests.cs` | **신규** | 프로세스 감지 테스트 |
| `docs/DEVELOPMENT.md` | 수정 | Phase 2B 체크리스트 업데이트 |

---

## 메시지 프로토콜 (IPC)

```
┌──────────────────────────────────────────┐
│  4 bytes (int32 LE)  │  N bytes UTF-8    │
│  = payload length    │  = JSON body      │
└──────────────────────────────────────────┘
```

- 요청: `CommandRequest` JSON (Plugin은 Newtonsoft.Json으로 역직렬화)
- 응답: `CommandResponse` JSON (Plugin은 Newtonsoft.Json으로 직렬화)
- 단일 요청-응답 모델 (연결당 1회)

---

## 재사용할 기존 코드

| 유틸리티 | 위치 | 용도 |
|---------|------|------|
| `Constants.GetPipeName()` | `Shared/Constants.cs:39` | 결정적 파이프명 생성 |
| `Constants.NormalizeProjectPath()` | `Shared/Constants.cs:25` | 경로 정규화 |
| `Constants.IpcConnectTimeoutMs` | `Shared/Constants.cs:14` | 5초 연결 타임아웃 |
| `Constants.PingTimeoutMs` | `Shared/Constants.cs:12` | 10초 ping 타임아웃 |
| `IpcRequestRouter.Route()` | `Plugin/Editor/Ipc/IpcRequestRouter.cs:12` | 요청→핸들러 디스패치 |
| `CommandRegistry.Dispatch()` | `Plugin/Editor/Commands/CommandRegistry.cs:46` | 커맨드 실행 |
| `UnityctlJsonContext` | `Shared/Serialization/JsonContext.cs` | System.Text.Json 직렬화 |
| `ITransport` | `Shared/Transport/ITransport.cs` | 트랜스포트 인터페이스 |
| `UnityProcessDetector` | `Core/Discovery/UnityProcessDetector.cs` | 프로세스 감지 (이미 구현, 플랫폼 위임) |
| `RetryPolicy` | `Core/Retry/RetryPolicy.cs` | 재시도 정책 |

---

## 구현 순서

1. **MessageFraming** (Core + Plugin) — 프로토콜 기반
2. **IpcServer** (Plugin) — 서버 구현
3. **UnityctlBootstrap** 수정 — 서버 시작
4. **IpcTransport** (Core) — 클라이언트 구현
5. **FindRunningUnityProcesses** (Platform별) — 프로세스 감지
6. **CommandExecutor** — IPC 우선 활성화
7. **테스트** — 단위 테스트 + 통합 테스트
8. **DEVELOPMENT.md** 업데이트

---

## 검증 방법

### 단위 테스트
```bash
dotnet test tests/Unityctl.Core.Tests/
dotnet test tests/Unityctl.Shared.Tests/
dotnet test tests/Unityctl.Cli.Tests/
```
- 기존 59개 테스트 전부 통과 확인
- 신규 IPC 테스트 통과 확인

### 로컬 파이프 통합 테스트
- Core.Tests에서 `NamedPipeServerStream` mock 서버를 띄우고 `IpcTransport.SendAsync()` 호출
- 메시지 프레이밍 round-trip 검증

### 수동 검증 (Unity 프로젝트 필요)
1. robotapp2에 플러그인 설치 (`unityctl init`)
2. Unity Editor 열기 → 콘솔에 `[unityctl] IPC server started on pipe: unityctl_xxxx` 확인
3. `unityctl status --project /path/to/robotapp2` → IPC로 즉시 응답 (Ready)
4. `unityctl ping --project /path/to/robotapp2` → IPC 응답 확인
5. Unity Editor 닫기 → `unityctl status` → Batch 폴백 동작 확인

### CI
- `dotnet test` 전체 통과 (IPC 서버 없이도 폴백으로 동작)
- 기존 CI 워크플로우 변경 불필요

---

## 기술 리스크 및 대응

| 리스크 | 대응 |
|--------|------|
| Unity에서 비동기 파이프 런타임 에러 | 동기 메서드 + 백그라운드 Thread 사용 (검증됨) |
| 도메인 리로드 시 파이프 서버 크래시 | AssemblyReloadEvents로 Stop/Start 관리 |
| 에디터 종료 시 스레드 미정리 → 행 | EditorApplication.quitting + Thread.Join(timeout) |
| 크로스 플랫폼 파이프명 호환 | .NET의 NamedPipeServerStream이 자동 처리 (Win=Named Pipe, Linux/Mac=UDS) |
| 프로세스 감지의 복잡성 | 파이프 Probe 우선 방식으로 단순화 (프로세스 감지는 보조적) |
