# How the Login + Lobby System Works

*(written so anyone, even a total beginner, can follow it)*

## 1. What are we even building?

Think of an online game like a birthday party.

1. First you need to show your invitation at the door — that's **logging in**.
2. Then you wait in the entrance room with your friends before the party
   starts — that's the **lobby**.
3. Once everyone you need has arrived, you all walk into the party together
   — that's **starting the match**.

This project already has all three parts built for you:

| Party analogy | What it's called in code | What it does |
|---|---|---|
| Show invitation at the door | **EOS login** (`EOSManager`) | Proves who you are to Epic's servers |
| Wait in the entrance room | **Lobby** (`LobbyActions`, `EOSTransport`) | A small waiting room where players gather before the game starts |
| Walk into the party | **Mirror networking** (`GameNetworkManager`) | Actually connects everyone's games together and spawns players |

You almost never need to touch the second and third column — they already
work. You mostly just need to know **how to call them** from your own
buttons and menus.

## 2. The three LEGO blocks

```
 ┌───────────────┐        ┌───────────────┐        ┌────────────────────┐
 │  EOSManager   │  --->  │ LobbyActions  │  --->  │ GameNetworkManager  │
 │  "log me in"  │        │ "gather here" │        │ "start the match"   │
 └───────────────┘        └───────────────┘        └────────────────────┘
```

- **`EOSManager`** — logs the player into Epic's servers. You call
  `EOSManager.Initialize(...)` once, with your game's ID card (the
  credentials from the Epic dev portal). After that, `EOSManager.Initialized`
  becomes `true`.
- **`LobbyActions`** — the waiting room. It has one job: keep a live list of
  "who is here right now" (`LobbyActions.Players`) and tell you whenever
  someone joins or leaves.
- **`GameNetworkManager`** — a Mirror `NetworkManager` that waits until
  enough players are in the room, then spawns everyone into the game.

You don't need to write these three — they already exist in
`Assets/CoreTemplate/Scripts/`. This guide shows you how to *use* them.

## 3. Step-by-step: setting it up in a new scene

### Step 1 — Drop in the two prefabs

Drag these two prefabs into your login scene:

- `Assets/CoreTemplate/Prefabs/EOS Manager.prefab`
- `Assets/CoreTemplate/Prefabs/Network Manager.prefab`

That's it — they contain all the plumbing already. You never need to look
inside them unless something is broken.

### Step 2 — Get your ID card from Epic

Go to the [Epic Games Dev Portal](https://dev.epicgames.com/portal) and
create a product. It will give you 6 things:

```
Product Name       -> e.g. "MyGame"
Product ID
Client ID
Client Secret
Sandbox ID
Deployment ID
```

Plus you make up your own **Encryption Key** — any random string of exactly
64 characters (0-9 and A-F only).

### Step 3 — Fill in the login form

Find (or make) a `LoginUI` component in your scene (see
`Assets/CoreTemplate/Scripts/LoginUI.cs`) and paste the 6 values from Step 2
into its Inspector fields. Think of this like typing your Wi-Fi password
once — after this, login "just works."

### Step 4 — That's the whole setup!

When the scene starts, `LoginUI` automatically calls:

```csharp
EOSManager.Initialize(new TransportInitializeOptions()
{
    ProductName = productName,
    ProductId = productId,
    ClientId = clientId,
    ClientSecret = clientSecret,
    SandboxId = sandboxId,
    DeploymentId = deploymentId,
    EncryptionKey = encryptionKey,
    DisplayName = fallbackDisplayName,
});
```

You never have to call this yourself unless you're building a custom login
screen.

## 4. How to create or join a lobby (the waiting room)

Once `EOSManager.Initialized` is `true`, you can create or join a lobby.
Here's the entire vocabulary you need:

```csharp
// Make a new lobby, "10" means up to 10 players allowed
LobbyActions.CreateLobby("My Cool Lobby", 10);

// Look for open lobbies to join
LobbyActions.FindLobbies(results => {
    // results is a list of lobbies you could join
});

// Join one of those results
LobbyActions.JoinLobby(someLobbyFromResults);

// Leave the lobby you're in
LobbyActions.LeaveLobby();
```

Starting the host or joining as a client over the network happens
**automatically** the moment you create/join a lobby — you don't need to
call any Mirror functions yourself.

## 5. How to show "who's in the room" on screen

This is the part from the earlier question! Here's the simplest possible
example — a script that prints every player's name to the console
whenever the room changes:

```csharp
using EpicTransport;
using UnityEngine;

public class ShowPlayers : MonoBehaviour
{
    private void OnEnable()  => LobbyActions.OnPlayerListChanged += PrintPlayers;
    private void OnDisable() => LobbyActions.OnPlayerListChanged -= PrintPlayers;

    private void PrintPlayers()
    {
        Debug.Log($"There are {LobbyActions.Players.Count} players in the room:");

        foreach (LobbyPlayer player in LobbyActions.Players)
        {
            string owner = player.IsOwner ? " (host)" : "";
            Debug.Log($" - {player.DisplayName}{owner}");
        }
    }
}
```

Read it like a recipe:
1. "Whenever the player list changes, run `PrintPlayers`."
2. "For every player currently in the room, print their name."
3. If they're the lobby owner (the person who created the room), also print `(host)`.

Swap `Debug.Log` for updating a UI list and you have a real lobby screen.

## 6. How the game actually starts

`GameNetworkManager` (in `Assets/CoreTemplate/Scripts/GameNetworkManager.cs`)
watches how many players have connected. It has one number you can tweak in
the Inspector:

```csharp
[SerializeField] private int playersToStart = 2;
```

Think of it like a rollercoaster ride: it won't start moving until enough
seats are filled. As soon as that many players have joined, it spawns
everyone into the game automatically. You don't call anything for this to
happen — just set the number and it works.

## 7. Cheat sheet — every event you can listen to

```
LobbyActions.OnLobbyCreated       -> you made a lobby
LobbyActions.OnLobbyJoined        -> you joined someone else's lobby
LobbyActions.OnLobbyLeft          -> you left the lobby
LobbyActions.OnLobbyDestroyed     -> the lobby you were in got closed
LobbyActions.OnPlayerJoined       -> a new player walked in
LobbyActions.OnPlayerLeft         -> a player walked out
LobbyActions.OnPlayerKicked       -> the host kicked someone
LobbyActions.OnPlayerPromoted     -> someone became the new host
LobbyActions.OnPlayerListChanged  -> ANY of the above happened (easiest one to use)
```

Rule of thumb: **if you just want to refresh a UI list, use
`OnPlayerListChanged`.** Only use the more specific events if you need to
react to one exact thing (like showing a "Player X left" toast message).

## 8. Where everything lives

```
Assets/CoreTemplate/Scripts/
  EOSManager.cs      <- logs you in (Step 3-4 above)
  EOSTransport.cs    <- the raw lobby networking, used by LobbyActions
  LobbyActions.cs    <- the friendly lobby API you actually use (Steps 4-5)
  GameNetworkManager.cs <- waits for enough players, then starts the match
  LoginUI.cs         <- example login/lobby screen you can copy or replace
```

## 9. If something breaks

- **"Login never finishes"** → double check your 6 Epic credentials and
  the encryption key is exactly 64 hex characters.
- **`LobbyActions.Players` is empty right after creating a lobby** → wait
  for `OnLobbyCreated` or `OnPlayerListChanged` to fire first; the list
  fills in a moment after the lobby exists.
- **Game never starts** → check `playersToStart` on `GameNetworkManager` —
  if it's set to 4 but only 2 people are testing, it'll just keep waiting.
