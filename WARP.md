# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

This is a **Valheim game modification (mod)** built as a C# DLL that injects into the Valheim game process. The mod provides ESP (Extra Sensory Perception) functionality to visualize AI entities in the game with bounding boxes and snaplines.

**Target Framework**: .NET Framework 4.7.2  
**Build System**: MSBuild (Visual Studio project format)  
**Game Integration**: Unity-based game (Valheim) using MonoBehaviour injection

## Build Commands

Build the project (Debug):
```powershell
msbuild modValheim.sln /p:Configuration=Debug
```

Build the project (Release):
```powershell
msbuild modValheim.sln /p:Configuration=Release
```

Alternatively, if using Visual Studio:
```powershell
# Open solution in Visual Studio
Start-Process modValheim.sln
```

Build outputs are located in:
- Debug: `bin\Debug\modValheim.dll`
- Release: `bin\Release\modValheim.dll`

## Architecture

### Core Components

1. **Loader.cs** - Mod lifecycle manager
   - `Init()`: Creates a persistent GameObject with the Mods component attached
   - `Unload()`: Destroys the mod GameObject to clean up
   - Uses `DontDestroyOnLoad()` to persist across scene changes

2. **Mods.cs** - Main mod logic (MonoBehaviour)
   - Scans for `BaseAI` game entities every frame in `OnGUI()`
   - Renders ESP visualizations for all AI entities
   - Handles input (DELETE key) to unload the mod
   - Maintains a camera reference for world-to-screen coordinate conversion

3. **Render.cs** - Low-level rendering utilities
   - Provides static methods for drawing lines, boxes, and text on screen
   - Uses Unity's IMGUI system (GUI/GUIUtility classes)
   - Handles matrix transformations for line rotation and scaling

### Injection Pattern

This mod follows a **DLL injection pattern** common in Unity game modding:

1. External injector loads the DLL into the Valheim process
2. `Loader.Init()` is called to bootstrap the mod
3. A GameObject is created with `Mods` component attached
4. The GameObject persists across scene loads via `DontDestroyOnLoad()`
5. Unity's MonoBehaviour lifecycle methods (`Start()`, `Update()`, `OnGUI()`) execute the mod logic

### ESP Rendering Flow

```
OnGUI() → FindObjectsOfType(BaseAI) → For each AI:
  ↓
DrawBoxESP() → Calculate bounds from Renderer
  ↓
WorldToScreenPoint() → Convert 3D positions to 2D screen space
  ↓
Render.DrawBox() + Render.DrawLine() → Draw ESP visuals
```

### Key Design Patterns

- **Singleton-like GameObject**: The Loader maintains a static reference to ensure only one mod instance exists
- **Component-based architecture**: Leverages Unity's MonoBehaviour for game loop integration
- **IMGUI rendering**: Uses immediate-mode GUI for overlay rendering each frame
- **Bounds-based ESP**: Calculates entity dimensions from Unity Renderer bounds

## Game-Specific Dependencies

The project references Valheim's Unity DLLs from the Steam installation directory:
```
C:\Program Files (x86)\Steam\steamapps\common\Valheim\Valheim_Data\Managed\
```

**Critical assemblies:**
- `Assembly-CSharp.dll` - Valheim's game code (contains BaseAI class)
- `assembly_valheim.dll` - Valheim-specific assembly
- `UnityEngine.*.dll` - Unity engine modules

**Note**: These DLL paths are hardcoded in the `.csproj` file and may need adjustment if Steam is installed in a different location.

## Development Notes

### Modifying ESP Behavior

- **Color changes**: Edit the `Color.red` parameter in `DrawAIESP()` or `DrawBoxESP()` methods
- **ESP visibility**: Modify the `w2sFootPos.z > 0f` check to adjust which entities are visible
- **Box dimensions**: Adjust the `widthOffset` variable in `DrawBoxESP()` (currently 2f)
- **Line thickness**: Change the width parameter in `Render.DrawBox()` and `Render.DrawLine()` calls

### Adding New Features

When extending this mod:

1. Add new methods to the `Mods` class for game logic
2. Use `Render` class utilities for any visual overlays
3. Hook into Unity lifecycle methods:
   - `Start()` - initialization
   - `Update()` - per-frame logic
   - `OnGUI()` - rendering overlays
   - `OnDestroy()` - cleanup

### Testing

1. Build the DLL using MSBuild
2. Use a DLL injector tool to load `modValheim.dll` into the Valheim process
3. Press DELETE key in-game to unload the mod
4. Check for visual ESP boxes around AI entities

### Known Issues

- The `DrawBoxESP(BaseAI ai)` method in `Mods.cs` is defined but called with wrong parameters in `OnGUI()`
- The method expects a `BaseAI` object but calls should pass calculated screen positions
- The `aiList` field is initialized but never populated with AI entities
- `DrawLine()` method creates new GameObjects each frame which could cause performance issues (should destroy or reuse)

## Unity/Valheim-Specific Considerations

- **Coordinate systems**: Unity uses left-handed Y-up coordinates; screen space has origin at bottom-left
- **BaseAI class**: Valheim's AI entity class - must have a Renderer component for bounds calculation
- **IMGUI limitations**: Runs in OnGUI() which executes multiple times per frame; minimize expensive operations
- **Scene persistence**: GameObject with `DontDestroyOnLoad()` survives across Valheim's scene transitions
