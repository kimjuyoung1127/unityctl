# README 시각 자료 캡처 가이드

README를 더 임팩트 있게 만들기 위한 캡처 목록.
SVG 다이어그램은 이미 생성됨. 아래는 직접 캡처해야 하는 항목.

## 이미 생성된 SVG (docs/assets/)

| 파일 | 설명 | README 위치 |
|------|------|-------------|
| `agent-loop.svg` | AI Agent 자동화 루프 (Plan/Execute/Verify/Diagnose) | The Solution 섹션 |
| `project-validate.svg` | project-validate JSON 출력 터미널 | Build Verification Pipeline 섹션 |
| `token-efficiency.svg` | 토큰 효율 비교 바 차트 (83x less) | Token Efficiency 섹션 |
| `editor-list.svg` | editor list 출력 (기존) | Terminal Output 섹션 |
| `log-table.svg` | log 출력 (기존) | Terminal Output 섹션 |
| `tools.svg` | tools 출력 (기존) | Terminal Output 섹션 |

## 캡처 필요 항목

### 1. Unity Before/After 스크린샷 (최우선)

빈 씬에서 unityctl로 게임 레벨을 구성하는 과정의 결과물.

**Before 캡처:**
```bash
# 새 씬 열기
unityctl scene create --name "DemoLevel" --project C:\Users\gmdqn\robotapp
```
- Unity Editor에서 빈 씬 스크린샷 (Scene View)

**After 캡처:**
```bash
# 레벨 구성
P="C:\Users\gmdqn\robotapp"
unityctl mesh create-primitive --type Plane --name "Floor" --scale "[20,1,20]" --project $P
unityctl mesh create-primitive --type Cube --name "Wall_Left" --position "[-10,2,0]" --scale "[0.5,4,20]" --project $P
unityctl mesh create-primitive --type Cube --name "Wall_Right" --position "[10,2,0]" --scale "[0.5,4,20]" --project $P
unityctl mesh create-primitive --type Sphere --name "Player" --position "[0,1,0]" --project $P
unityctl mesh create-primitive --type Cube --name "Obstacle1" --position "[3,0.5,5]" --project $P
unityctl mesh create-primitive --type Cube --name "Obstacle2" --position "[-2,0.5,-3]" --scale "[2,1,1]" --project $P
unityctl mesh create-primitive --type Cylinder --name "Pillar" --position "[5,1.5,-5]" --scale "[0.5,3,0.5]" --project $P
unityctl scene save --project $P
```
- Unity Editor에서 구성된 레벨 스크린샷 (Scene View, 약간 위에서 내려다보는 앵글)
- **파일명**: `docs/assets/before-after.png` 또는 나란히 합친 이미지

**README 배치 위치**: "What AI Agents Can Build" 섹션 상단

### 2. MCP 대화 GIF (최고 임팩트)

Claude Code에서 unityctl MCP를 통해 씬을 만드는 실제 대화 녹화.

**녹화 도구**: [ScreenToGif](https://www.screentogif.com/) 또는 Windows Game Bar (Win+G)

**시나리오 스크립트:**
```
사용자: "Create a simple platformer level with a floor, some obstacles, and a player"
Claude: [unityctl_run으로 scene create]
Claude: [unityctl_run으로 mesh create-primitive x5]
Claude: [unityctl_query로 scene hierarchy 확인]
Claude: [unityctl_query로 project-validate 확인]
Claude: "Level created with 5 objects. Project validation passed."
```

**팁:**
- 해상도: 1280x720 이상
- 프레임: 15fps (파일 크기 절감)
- 길이: 15-30초 (길면 지루함)
- 속도: 실제 속도의 2-3배 (빠른 응답 강조)
- **파일명**: `docs/assets/mcp-demo.gif`

**README 배치 위치**: 히어로 바로 아래 또는 "The Solution" 섹션

### 3. doctor 진단 스크린샷

```bash
# 정상 상태
unityctl doctor --project C:\Users\gmdqn\robotapp --json

# 비정상 상태 (Unity 닫은 상태에서)
unityctl doctor --project C:\Users\gmdqn\robotapp --json
```

- 터미널 스크린샷 (VSCode 터미널 또는 Windows Terminal)
- **파일명**: `docs/assets/doctor-output.png`

**README 배치 위치**: 비교표 "Diagnostics" 행 근처

### 4. scene-diff 출력 스크린샷 (선택)

```bash
# 스냅샷 찍기
unityctl scene snapshot --project $P --json > before.json

# 뭔가 변경 후
unityctl gameobject move --target "Player" --position "[5,1,5]" --project $P

# diff
unityctl scene diff --project $P --json
```

- diff 결과의 property-level 변경 표시
- **파일명**: `docs/assets/scene-diff.png`

## README에 이미지 배치 코드

캡처 후 README.md에 추가할 마크다운:

```markdown
<!-- 히어로 아래 (MCP GIF) -->
<p align="center">
  <img src="docs/assets/mcp-demo.gif" alt="AI agent building a Unity scene via MCP" width="720">
</p>

<!-- What AI Agents Can Build 상단 (Before/After) -->
<p align="center">
  <img src="docs/assets/before-after.png" alt="Empty scene to platformer level via unityctl" width="680">
</p>
```

## 우선순위

1. **Before/After 스크린샷** — 가장 쉽고 즉시 설득력 있음 (5분)
2. **MCP 대화 GIF** — 가장 임팩트 크지만 녹화+편집 필요 (30분)
3. **doctor 스크린샷** — 차별점 강조 (5분)
4. **scene-diff 스크린샷** — 보너스 (5분)
