# Project Structure — EOS + Mirror Template

This project is meant to be reused as a starting point for future multiplayer
games (Epic Online Services for lobbies/matchmaking + Mirror for networking).
The folders under `Assets/` are split into three kinds, so it's obvious at a
glance what you're allowed to touch.

```
Assets/
  CoreTemplate/   <-- DO NOT EDIT. The reusable EOS + Mirror plumbing.
  YourGame/       <-- EDIT THIS. Your game's own code, scenes, art.
  ThirdParty/     <-- DO NOT EDIT. Vendored packages (Mirror, EOS SDK, etc).
  Settings/       Unity render pipeline settings (rarely touched).
  Resources/      Empty, reserved for Resources.Load() assets if ever needed.
  TutorialInfo/   Leftover from Unity's URP template — safe to delete.
```

## `Assets/CoreTemplate/` — DO NOT EDIT

The generic login → lobby → networked-game plumbing. This is what makes the
template a template — every future game reuses it as-is.

- `Scripts/` — EOS login, lobby creation/join/search, player list syncing
  (`EOSManager`, `EOSTransport`, `LobbyActions`, `GameNetworkManager`,
  `HostMigrationController`, `VoiceChat`, etc).
- `Prefabs/` — `EOS Manager.prefab` (EOS SDK bootstrap) and
  `Network Manager.prefab` (Mirror `NetworkManager` + EOS transport).
- `Scenes/` — `Login.unity`, the entry-point scene that handles sign-in and
  the lobby browser/create/join UI.
- `Editor/` — editor-only tooling for the above (build helpers, icons).

If you find a bug here, fix it — but treat it as a fix that should flow back
into the template for every future game, not a one-off hack for the current
game.

See `HOW_TO_EOS_LOBBY.md` in this folder for a beginner-friendly walkthrough
of how login and the lobby system work and how to use them.

## `Assets/YourGame/` — EDIT THIS

Everything specific to the game you're currently building. Safe to delete,
rewrite, or replace entirely per project.

- `Scripts/` — gameplay code, e.g. `PlayerController.cs` (movement/visuals
  for the spawned player object).
- `Scenes/` — `SampleScene.unity`, the actual gameplay scene loaded after a
  match starts. Replace with your real game scene(s).
- `Prefabs/` — `Player.prefab`, the networked player prefab spawned by
  `GameNetworkManager`. Swap its model/components per game, just keep it
  wired to `GameNetworkManager`'s player prefab slot.
- `Art/` — art asset packs (character models, animations, etc). Currently
  `Cute_Characters_Pack_1`, a placeholder — replace with your game's art.

## `Assets/ThirdParty/` — DO NOT EDIT

Vendored third-party packages, kept separate so it's obvious they came from
somewhere else and shouldn't be hand-edited. Update by reimporting/upgrading
the package, not by editing files in place.

- `Mirror/` — the Mirror networking library.
- `EOS SDK/` — Epic Online Services native SDK.
- `Plugins/` — native plugin binaries the SDK depends on.

## Using this as a template for a new game

1. Duplicate/clone the repo (or use it as a GitHub template repo).
2. Delete everything under `Assets/YourGame/` and build your own game there.
3. Point `GameNetworkManager`'s player prefab at your new `Player.prefab`.
4. Set your own EOS Product/Sandbox/Deployment credentials on the
   `EOS Manager` prefab.
5. Leave `Assets/CoreTemplate/` and `Assets/ThirdParty/` alone unless you're
   fixing a bug that should apply to every game built from this template.
