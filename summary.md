# Session Summary

## Project context

- Unity project using **Mirror** for networking + a custom **Epic Online
  Services (EOS)** layer for login and lobby/matchmaking.
- Goal stated by the user: turn this project into a reusable **template**
  for future games.

## What we did

### 1. Answered: how to list players currently in a room

Two separate player lists exist in this project:
- **Lobby roster (pre-game)**: `LobbyActions.Players` (static list,
  auto-synced), with `LobbyActions.OnPlayerListChanged` event to refresh UI.
- **In-match connections (Mirror)**: `NetworkServer.connections` /
  `NetworkClient.connection`. `GameNetworkManager.cs` tracks a private
  `waitingPlayers` list server-side before the match starts.

### 2. Reorganized the folder structure into a clean template layout

Moved files (via `git mv`, preserving Unity `.meta` GUIDs) into:

```
Assets/
  CoreTemplate/   DO NOT EDIT — reusable EOS + Mirror plumbing
    Scripts/, Prefabs/, Scenes/ (Login.unity), Editor/
  YourGame/       EDIT THIS — game-specific content
    Scripts/ (PlayerController.cs), Scenes/ (SampleScene.unity),
    Prefabs/ (Player.prefab), Art/ (Cute_Characters_Pack_1)
  ThirdParty/     DO NOT EDIT — vendored packages
    Mirror/, EOS SDK/, Plugins/
  Settings/, Resources/, TutorialInfo/   (left unchanged)
```

Also fixed the two places that hardcoded old scene paths so the project
still builds correctly:
- `ProjectSettings/EditorBuildSettings.asset`
- `ProjectSettings/ProjectSettings.asset` (`templateDefaultScene`)

Note: moving `Assets/Mirror` via `git mv` hit a Windows file-lock error
partway through; finished with PowerShell `Move-Item` instead. End result
is identical — git still detected it as a clean rename after `git add -A`.

**Status: all changes are staged (`git add -A` run) but not committed.**
Was told not to commit unless asked.

### 3. Wrote two documentation files

- `Assets/_README_STRUCTURE.md` — explains the `CoreTemplate` /
  `YourGame` / `ThirdParty` split, what's safe to edit, and how to reuse
  this project as a template for a new game.
- `Assets/CoreTemplate/HOW_TO_EOS_LOBBY.md` — beginner-friendly walkthrough
  of the login + lobby system (birthday-party analogy: login = show
  invitation, lobby = wait in entrance room, match start = walk into the
  party). Covers setup steps, EOS credentials needed from the Epic dev
  portal, lobby create/join/leave code, listing players in the room,
  the `playersToStart` auto-start mechanic, an event cheat-sheet, and a
  troubleshooting section.

### 4. Verified lobby movement sync works across machines

Lobby system confirmed working end-to-end, tested across two separate
computers — players can join the same lobby and see each other moving in
real time.

## Open items / things to revisit

- Reorganization is staged but **uncommitted** — confirm it looks right in
  the Unity Editor before committing.
- `Assets/TutorialInfo/` is leftover Unity URP template content, flagged as
  safe to delete but not removed.

## Company update — 2026-09-02

- Restructured the Unity project into a clean, reusable template layout so
  it can be spun up for future games without rework:
  - `CoreTemplate/` — the EOS + Mirror networking core (login, lobby,
    matchmaking, host migration) — locked, not meant to be edited per-game
  - `YourGame/` — game-specific code, scenes, art — the only folder devs
    should touch per project
  - `ThirdParty/` — vendored packages (Mirror, EOS SDK) kept isolated from
    our code
- Fixed project build settings so scenes still load correctly after the move
- Wrote onboarding docs for the team:
  - Structure guide explaining what's editable vs. fixed and how to fork
    this as a template for new games
  - Step-by-step, beginner-friendly guide to the EOS login + lobby system
    (setup, credentials, code snippets for creating/joining lobbies and
    showing the player list)
- **Lobby system is working end-to-end and tested across two separate
  computers** — players can join the same lobby and see each other moving
  in real time
- Documented how to pull the live list of players currently in a
  lobby/room for UI work
- All changes are staged in git but **not yet committed**, pending a final
  review pass
