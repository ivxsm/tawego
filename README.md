# Tawego Flappy Bird (Games class phase 2&3)

Flappy Bird-style game with the tawago character.

Demo video:


https://github.com/user-attachments/assets/de3c2d81-1aa6-4fed-a369-ccd848617540



## Scenes

- **main menu** – difficulty, shop, skin preview, play.
- **SampleScene** – gameplay.

## What I added (features and systems)

### Gameplay and level flow

- Pipe spawning and movement (`pipspowner`, `pipe`), including difficulty stored in `PlayerPrefs` from the menu.
- **Score zone** vs **pipe damage** using colliders/triggers and tags so passing a gap can score without the same collider killing the bird.
- **Coins** collectible in the pipe gap (`Coin`, `PipeCoinSpawner` uses pipe colliders to place coins in the gap with a margin).
- **Dual scoring** (pipes and coins) with HUD updates in `LogicScript`.
- **Power / lives** – a limited number of pipe hits before game over, with a UI bar.
- **Shield** – periodic shield from collecting enough coins (logic in `LogicScript`).
- **Countdown timer** – timed run with a win state when time reaches zero (pause and win UI).
- Bird tuning: flap strength caps, gravity feel, optional death when falling too far behind the camera, frozen rotation for a stable sprite read.

### Character skins

- Skins loaded from `Assets/Resources/Skins/` (`default`, `green`, `blue` sprites).
- Selected skin index stored in `PlayerPrefs` under `MainMenuScript.SkinPrefKey` and applied on the bird at runtime.

### Scripts (quick map)

| Script | Role |
|--------|------|
| `bird_script` | Movement, collisions, skin sprite, death |
| `LogicScript` | Score, HUD, timer, win/lose, power, shield, UI |
| `MainMenuScript` | Menu UI, shop, skins, scene load |
| `pipspowner` | Pipe spawn timing / difficulty |
| `pipe` | Individual pipe behaviour (gap, movement) |
| `PipeScoreTrigger` | Trigger for scoring when passing a pipe |
| `PipeCoinSpawner` | Spawns coins in the gap |
| `Coin` | Pickup and notify game logic |
