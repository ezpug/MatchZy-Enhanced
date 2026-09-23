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

## Rebasing onto a new upstream release

EZPug Iron's `docs/operations.md` ("MatchZy-Enhanced, our fork") has the procedure.
In short: fetch upstream's tags, rebase `ezpug` onto the new tag, set `ModuleVersion` to
`<upstream>-ezpug.1`, run `dotnet test tests/MatchZy.Tests`, push the branch with
`--force-with-lease` and push the new tag.
