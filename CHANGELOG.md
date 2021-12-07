## v1.3.10

- Fixed offline face assignment
- Made `PopUpMenu` class public

## v1.3.9

- Fixed online face assignment

## v1.3.8

- Fixed Deathmatch game end hooks.

## v1.3.7

- Fixed invites after disconnecting from game.

## v1.3.6

- Fixed error when changing game mode.

## v1.3.5

- Fixed uncommon issue where blocking would not work after rematching.

## v1.3.4

- Fixed continue/rematch option not being offered.

## v1.3.3

- Fixed issue where changes to game mode settings from mods such as [SetRounds](https://rounds.thunderstore.io/package/Ascyst/SetRounds/) were not respected.

## v1.3.2

- Fixed inviting players after returning to lobby from a game.

## v1.3.0

- Added possibility to continue or rematch after game ends.
- Fixed **Chilling Presence** effect color (Fixes [#6](https://github.com/olavim/RoundsWithFriends/issues/6)).

## v1.2.5

- Fixed round counter for the blue player.

## v1.2.4

- Fixed issue where players would get stuck in card pick phase when playing Deathmatch/FFA.

## v1.2.3

- Updated UnboundLib dependency to version >=2.1.0. The version adds support for coroutine game mode lifecycle hooks, allowing for better control when modding game modes.
- Fixed round/point display showing wrong fill amount during round transition.

## v1.2.2

- Restore game mode and settings after returning to lobby from an online game.
- Fixed the targeting logic of various effects when playing with more than two players. The full extent of this fix is difficult to determine, but at least the cards **Chase** and **Radar shot** were affected.

## v1.2.1

- Bumped UnboundLib dependency version to >=1.1.2
- Fixed issue where opening local multiplayer lobby with controller would cause visual artifacts and prevent other players from joining.

## v1.2.0

- UnboundLib dependency bumped to version >=1.1.0.
- Adds game mode lifecycle hooks and basic API game settings (not modifiable from UI).

## v1.1.2

- Fixed respawning in sandbox mode.

## v1.1.1

- UnboundLib dependency bumped to >=1.0.0.7.
- Added Deathmatch/FFA game mode. Supports online and local multiplayer.
- Fixed respawning in sandbox mode.
- Fixed rematch dialog getting stuck if you let your player die during the dialog.
- Fixed background art not being correctly set after returning from an online game.
- Fixed character creators overlapping each other in local multiplayer lobby.

## v1.0.0

- Changed local multiplayer lobby UI to support up to four players.
- Added a private multiplayer lobby UI to support up to four online players.
