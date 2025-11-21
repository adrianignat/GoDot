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
├── Arrow.cs                # Homing projectile
├── Bow.cs                  # Weapon spawner
├── Characters/
│   ├── Character.cs        # Base class for all characters
│   ├── Player.cs           # Player controller, input, animations
│   ├── PlayerHealthBar.cs  # Health bar UI
│   └── Enemies/
│       ├── Enemy.cs        # Goblin AI, pursuit, attack
│       └── EnemySpawner.cs # Wave spawning with difficulty scaling
├── Helpers/
│   ├── Direction.cs        # 8-way direction enum & utilities
│   └── AnimationHelper.cs  # Animation utilities (placeholder)
├── Menus/
│   └── MainMenu.cs         # Start/pause/restart menu
├── Objects/
│   └── Gold.cs             # Collectible with magnet behavior
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
- Enemies pursue and attack the player when in range
- Directional animations for shooting and attacking

### Progression
- Kill enemies to drop gold
- Gold is magnetically attracted to player when nearby
- Reaching gold thresholds triggers upgrade selection
- Upgrades: Health, Speed, Weapon Speed
- Rarities: Common (+10), Rare (+20), Epic (+30), Legendary (+40)

### Difficulty Scaling
- Enemy spawn rate increases 10% every 30 seconds
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

## Running the Project

Open `project.godot` in Godot 4 and run `main.tscn`.
