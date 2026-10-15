# PALINODIA / パリノディア

**Physics Prototype 0.1** — a self-contained, playable third-person Unity demo for an energy-conversion 3D story action game. No proprietary assets, models or Asset Store imports are required. The single demo room is built from primitives at runtime by `DemoBootstrap`.

> **Status:** Source code and Unity scene included. Not yet compiled or Play-tested in Unity Editor on this machine. First Editor run is the acceptance test.

## Requirements

- Unity **6000.0.67f1** (Unity 6 LTS), or a compatible Unity 6 editor. In Unity Hub use **Add project from disk** and select **this repository root** containing `Assets/`, `Packages/` and `ProjectSettings/`.
- Built-in 3D renderer. This is not a URP project. Standard material shaders are generated at runtime.
- Legacy Input enabled; `ProjectSettings/ProjectSettings.asset` sets `activeInputHandler: 2` (Both). If you get the message about the old Input Manager being disabled, change **Project Settings → Player → Active Input Handling → Both**, and restart the Editor.
- Desktop keyboard + mouse (Windows targeted).

## Start

1. Extract or clone this repository and open its **root directory** as a Unity project from Unity Hub.
2. In Unity's Project pane, double-click `Assets/Scenes/PhysicsLab.unity`.
3. Press **Play**. `DemoBootstrap` generates the test laboratory and all interactable objects at runtime; there is no need to install packages or import 3D assets. Do not expect the room objects to exist in the editor hierarchy before Play.
4. Disable **both pursuit drones** by thermal overload or kinetic shock, then walk into the unlocked exit ring. Press **R** to restart.

## Controls

| Control | Action |
|---|---|
| WASD | Move relative to camera |
| Mouse | Rotate follow camera (cursor captured; Esc releases) |
| Space | Jump |
| E (hold) | Draw thermal energy from the glowing orange heat core under crosshair (max 7m) |
| Left mouse | Kinetic energy pulse at aimed object; pushes real rigidbodies |
| Right mouse | Thermal energy pulse at aimed object; heats bodies in a small area |
| Tab (hold) | Scan aim target's temperature, machine status and energy |
| R | Reload the laboratory |
| Esc | Release / recapture cursor by left-clicking |

## Demo content

- Third-person `CharacterController`, orbiting camera, crosshair HUD, energy bar, temperature, prompts and objective tracking.
- Finite energy sources, capped energy reservoir, transfer efficiencies/loss, thermal target state, heat exchange with a stage-average ambient.
- Multi-target pulses with falloff and wall occlusion; kinetic blasts use Rigidbody impulse and explicit kinetic energy accounting.
- Two physically driven enemy drones that move toward the player and emit local heat bursts. Machines shut down if their electronics overheat or shock exceeds tolerance. Player collapses if body temperature becomes dangerous.
- Movable metallic test spheres, segmented room walls, modular props, automatic door opening and goal trigger.
- `Assets/Tests/Editor` tests validate core energy arithmetic in Unity Test Runner.

## Repository structure

```text
Assets/
  Scenes/PhysicsLab.unity        # playable scene with startup component
  Scripts/Simulation/           # environment, bodies, reservoir, energy exchange
  Scripts/Gameplay/             # actor controls, drones, objective, demo setup
  Scripts/Presentation/         # HUD and pulse visuals
  Tests/Editor/                 # edit-mode unit tests
Docs/PHYSICS_DESIGN.md          # units, conservation assumptions, limitations
Docs/PROJECT_SOURCE.md          # original project constraints and story canon
```

## GitHub setup if using the ZIP

This ZIP also carries a `.git` repository with an initial local commit, when extracted with hidden directories retained. Create an **empty** GitHub repository named `palinodia` (don't check "Add README"), then from inside the extracted root:

```bash
git remote add origin https://github.com/YOUR_USERNAME/palinodia.git
git push -u origin main
```

If `.git` is not present after extraction:

```bash
git init -b main
git add .
git commit -m "feat: PALINODIA playable physics lab prototype"
git remote add origin https://github.com/YOUR_USERNAME/palinodia.git
git push -u origin main
```

## Next priorities

Play-test the Unity project, fix any version/package/import issues, tune camera and pulse feel, add more physical interactions, then implement one story-driven vertical slice. Narrative names and non-canonical concepts are deliberately **not** hard-coded.
