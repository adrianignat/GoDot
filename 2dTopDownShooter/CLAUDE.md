# 2D Top-Down Shooter

A Vampire Survivors-style roguelike survival game built with Godot 4 and C#.

## Overview

This is a 2D top-down survival game where players fight endless waves of enemies, collect gold, and choose upgrades to become progressively stronger. The game features auto-targeting weapons, magnetic loot collection, and scaling difficulty.

## Tech Stack

- **Engine**: Godot 4
- **Language**: C#
- **Architecture**: Signal-based event system with singleton game manager

## Project Structure

```
Scripts/
├── Game.cs                 # Singleton game manager, state & signals
├── Arrow.cs                # Homing projectile with bouncing/piercing
├── Bow.cs                  # Arrow spawner (extends Spawner<Arrow>)
├── DynamiteThrower.cs      # Dynamite spawner (extends Spawner<Dynamite>)
├── Characters/
│   ├── Character.cs        # Base class with health, damage, hit flash
│   ├── Player.cs           # Player controller, upgrades, input
│   ├── PlayerHealthBar.cs  # Health bar UI
│   └── Enemies/
│       ├── Enemy.cs        # Goblin AI with tier system
│       └── EnemySpawner.cs # Wave spawning with tier progression
├── Helpers/
│   ├── Direction.cs        # 8-way direction enum & utilities
│   └── AnimationHelper.cs  # Animation utilities (placeholder)
├── Menus/
│   └── MainMenu.cs         # Start/pause/restart menu
├── Objects/
│   ├── Gold.cs             # Collectible with magnet behavior
│   └── Dynamite.cs         # Explosive projectile with fuse animation
├── Spawners/
│   ├── Spawner.cs          # Generic spawner base class
│   └── LootSpawner.cs      # Drops loot on enemy death
└── Upgrades/
    ├── Upgrade.cs          # Upgrade types, rarities, amounts
    └── UpgradeSelection.cs # Upgrade UI and selection logic

Entities/                   # Godot scene files (.tscn)
Sprites/                    # Character and UI sprites
```

## Core Mechanics

### Combat
- Player auto-shoots arrows that home in on the nearest enemy
- Arrows can gain Bouncing (ricochet) or Piercing (pass-through) abilities
- Dynamite can be thrown at enemies, exploding after a fuse animation
- Enemies pursue and attack the player when in range
- Hit flash animation on damage (white flash via tween)

### Enemy Tiers
Enemies have tiers that determine health (BaseHealth = 100):
- **Blue** (Tier 1): 100 HP - spawns from start
- **Red** (Tier 2): 200 HP - introduced at 60s
- **Purple** (Tier 3): 300 HP - introduced at 120s
- **Yellow** (Tier 4): 400 HP - introduced at 180s

Sprites are cached per tier to avoid recreating SpriteFrames for each enemy.

### Upgrades
| Type | Description | Notes |
|------|-------------|-------|
| Health | Increase max HP | |
| Speed | Movement speed | |
| WeaponSpeed | Arrow fire rate | |
| Luck | Better rarity chances | +2% legendary, +5% epic per 10 luck |
| Magnet | Gold pickup radius | Starts at 0, max 300 |
| Bouncing | Arrows ricochet | Legendary only, mutually exclusive with Piercing |
| Piercing | Arrows pass through | Legendary only, mutually exclusive with Bouncing |
| Dynamite | Throw explosives | Starts disabled, max blast radius 150 |

Rarities: Common (+10), Rare (+20), Epic (+30), Legendary (+40)

### Progression
- Kill enemies to drop gold
- Gold is magnetically attracted to player (requires Magnet upgrade)
- Reaching gold thresholds triggers upgrade selection
- No duplicate upgrade types in selection screen

### Difficulty Scaling
- Enemy spawn rate increases 10% every 30 seconds
- New enemy tiers introduced every 60 seconds
- Enemies spawn off-screen at map edges

## Input

- **WASD / Arrow Keys**: Movement (8-directional)
- **ESC**: Pause menu
- Shooting is automatic

## Collision Layers

1. Map
2. Player
3. Enemies
4. Environment
5. Projectiles
6. Loot

## Key Signals (Game.cs)

- `EnemyKilled` - Triggers loot drops
- `PlayerTakeDamage` / `PlayerHealthChanged` - Health system
- `GoldAcquired` - Currency collection
- `UpgradeReady` / `UpgradeSelected` - Progression system

## Architecture Patterns

### Spawner Pattern
`Spawner<T>` is a generic base class for spawning objects at intervals:
- Override `GetLocation()` to set spawn position
- Override `CanSpawn()` to control spawn conditions
- Override `InitializeSpawnedObject(T obj)` to set properties before `_Ready()`

Used by: `Bow`, `DynamiteThrower`, `EnemySpawner`, `LootSpawner`

**Important**: `InitializeSpawnedObject` is called BEFORE `AddChild`, so:
- Use it to set simple properties (Arrow.BouncingLevel, Enemy.Tier)
- Don't access child nodes here - they aren't ready yet
- For initialization needing child nodes, use properties set here and read them in `_Ready()`

### Upgrade Caps
Some upgrades have maximum values and are removed from selection when maxed:
- Check caps in `UpgradeSelection.GetAvailableUpgradeType()`
- Constants defined in `Player.cs` (MaxMagnetRadius, MaxDynamiteBlastRadius)

## Running the Project

Open `project.godot` in Godot 4 and run `main.tscn`.
