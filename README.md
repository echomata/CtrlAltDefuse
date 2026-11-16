# Ctrl+Alt+Defuse

A single-player 3D bomb-defusal game where your only manual is on a virus-infected computer that fights you for every answer.

## Project status

Initial Unity project setup. Gameplay systems are planned and have not been implemented yet.

## Game concept

The player switches between a bomb and a computer at a fixed desk. The computer holds the manual, but viruses interrupt access with popups, cursor hijacks, and other distractions.

- Defuse the Wires, Code Lock, and Lever Pull modules within five minutes.
- Three incorrect actions, or running out of time, ends the round.
- The manual's rules are always truthful; the infected interface makes them difficult to access.

## Open the project

1. Install Unity Hub and Unity Editor **6000.6.9f1**.
2. Clone this repository.
3. In Unity Hub, choose **Add project from disk** and select the repository root, which contains `Assets`, `Packages`, and `ProjectSettings`.
4. Open the project with the matching Editor version and let Unity import its packages and assets.
5. Open `Assets/Scenes/MainScene.unity`.

The project uses the Universal Render Pipeline and Unity's Input System.

## Working together

- Use the same Unity Editor version across the team.
- Keep `Assets`, their `.meta` files, `Packages`, and `ProjectSettings` in version control.
- Unity regenerates `Library`, `Temp`, `Logs`, and other local caches; `.gitignore` excludes them.
- Use a feature branch for each task and pull teammates' changes before starting new work.
- Coordinate scene and prefab edits to reduce merge conflicts.
