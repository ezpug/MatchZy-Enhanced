# The ezpug patch series

This is [EZPug](https://github.com/ezpug/ezpug-iron)'s fork of
[MatchZy-Enhanced](https://github.com/Auto-Tournament/cs2-plugin) (MIT, see `LICENSE`).
The `ezpug` branch is one upstream release tag with a short series of commits on top,
each titled `ezpug: …`, and nothing else. Releases are tagged `v<upstream>-ezpug.<n>`
and built by `.github/workflows/ezpug-release.yml`.

## What the series changes

**A simulated match can leave seats to people.** Upstream's simulation mode spawns one
bot for every configured player, readies both teams on everybody's behalf and, after two
watchdog passes, starts the match whoever is there. The series lets a roster entry in a
team's `players` map say that a person, not a bot, takes the seat:

```json
"players": {
  "76561198000000001": "puppet-1",
  "76561198000000002": { "name": "tk", "simulated": false }
}
```

- `ezpug: a per-player simulated flag in the players map` — a seat with
  `"simulated": false` (a JSON boolean) gets no bot and no simulated identity. Any other
  entry, including every plain name, is a bot's seat as before. The other readers of a
  player's name accept both forms.
- `ezpug: a person's seat goes through the ordinary ready gate` — with such a seat,
  nothing marks a team ready on anybody's behalf. The bots are readied by the simulation,
  the person readies (or is auto-readied when the match enables it), and `IsTeamReady`
  decides, which needs `players_per_team` bodies on the side first.
- `ezpug: the watchdog never force-starts a match a person has a seat in` — it keeps
  reconciling the bots, and the match waits. `css_start` still starts it.

A match file with no `"simulated": false` entry takes none of the new branches.

**The plugin is built against the CounterStrikeSharp it runs on.** Upstream builds against
CounterStrikeSharp.API 1.0.342 on `net8.0`. EZPug's servers run 1.0.381, which marks the
untyped `ConVar` obsolete (upstream PR #935, since 1.0.379), and the day it is deleted a
binary built against 1.0.342 fails at load while every build still passes.

- `ezpug: cvars through ConVar<T>, built against CounterStrikeSharp 1.0.381` — the plugin
  and its tests target CounterStrikeSharp.API 1.0.381 and `net10.0` (every API package from
  1.0.369 on is `net10.0` only, and `ConVar<T>` first shipped in 1.0.379). Every cvar read
  or write goes through `ConVar<T>` or `ConVarBase`, with the same type per call site, and
  `GetConvarStringValue` / `SetConvarValue` go through `ConvarValues`, a pure table of
  engine type to CLR type with its own tests. Using an obsolete CounterStrikeSharp API is a
  build error (`CS0612`, `CS0618`, `CS0619`). `PlayerConnectedState` lost its `Player`
  prefix in the API (`PlayerConnected` is `Connected`, same values). Two reads change on
  purpose. A `Float64` cvar is read and written as a `double`, where the untyped API used a
  `float`. The safe auto-updater reads the `matchzy_tournament_*` strings, which the
  untyped `GetPrimitiveValue<string>` always refused (it fell back to `idle`).

## Rebasing onto a new upstream release

EZPug Iron's `docs/operations.md` ("MatchZy-Enhanced, our fork") has the procedure.
In short: fetch upstream's tags, rebase `ezpug` onto the new tag, set `ModuleVersion` to
`<upstream>-ezpug.1`, run `dotnet test tests/MatchZy.Tests`, push the branch with
`--force-with-lease` and push the new tag. A release between rebases (`ezpug.2`, …) is a
fast-forward: the change, then a `ezpug: release …` commit that moves `ModuleVersion`. A
rebase folds the release commits back into one, the last in the series.

The .NET SDK is 10 (`dotnet test tests/MatchZy.Tests -c Release`, `dotnet build -c Release`).
