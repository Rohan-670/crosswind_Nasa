# ASTROBASE: BEYOND EARTH — Development Instructions

## Project
- Game name: ASTROBASE
- Engine: Unity 6.3 LTS
- Render pipeline: URP
- Language: C#
- Target: Windows development and browser deployment using Web Build Support
- Game type: 3D space exploration and simulation

## Main missions
1. Lunar Expedition: land on the Moon, explore, collect samples and return to Earth.
2. Red Planet Expedition: land on Mars, explore and return to Earth.
3. Solar Orbit Expedition: travel to a safe observation orbit, collect observations and return to Earth.
- Never allow the player to land on the Sun.

## Development rules
- Inspect existing files before editing them.
- Do not overwrite working scripts without explaining why.
- Build one feature at a time.
- Use beginner-friendly, readable C#.
- Use clear class and file names.
- Avoid unnecessary packages and dependencies.
- Do not modify ProjectSettings or Packages unless necessary.
- Do not create duplicate managers or duplicate scripts.
- Explain which GameObjects need scripts attached.
- Explain all Inspector references and component settings.
- Include test instructions for every feature.
- Fix compile errors before moving to another feature.
- Keep browser performance in mind.

## NASA assets and data
- Use NASA-origin assets and data for external space imagery, models and information.
- Attribute assets and follow their individual usage terms.
- Do not invent NASA API endpoints or measurements.
- Cache downloaded information and provide offline fallback.
- Never expose private API keys in a browser build.
- Treat NASA data as real only when its source and meaning are verified.

## Game simulation
- Clearly distinguish simplified game physics from real spaceflight physics.
- Include navigation routes, checkpoints and hazards.
- Solar mission is an observation mission, not a landing mission.

## Workflow
1. Inspect the current project.
2. Explain the implementation plan.
3. Make scoped changes.
4. List files created or modified.
5. Give Unity Editor setup steps.
6. Give a test checklist.
7. Wait for the feature to work before starting the next feature.
