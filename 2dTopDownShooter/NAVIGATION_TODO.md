# Navigation System - Parked Task

## Problem Summary
Enemies get stuck on obstacles (hills, trees, buildings) because the navigation mesh doesn't properly exclude these areas.

## Current State

### What We Did
1. **Replaced custom A* with NavigationAgent2D** - Simplified enemy movement to always use Godot's built-in navigation
2. **Simplified Enemy.cs** - Removed complex stuck detection, now just uses NavigationAgent2D directly
3. **Checkpoint commit:** `6f82016` - Can revert to hybrid stuck-detection approach if needed
   ```
   git reset --hard 6f82016
   ```

### The Root Cause
The `Layer_3_Grass` TileMapLayer fills the entire map with grass tile `1:1`, which has a **full navigation polygon**. Hills/obstacles are placed on `Layer_5_Hills` on TOP of the grass, adding collision but NOT removing the underlying grass navigation.

**Result:** Navigation mesh says "entire area is walkable" while physics says "hills block movement" = enemies get stuck.

### Tileset Analysis (`WorldTileset_Day.tres`)

**Tiles WITH navigation polygons (walkable):**
- Water/grass edge tiles (0:0 through 2:2) - partial navigation polygons
- Center grass tile (1:1) - FULL navigation polygon covering entire tile

**Tiles with collision but NO navigation:**
- Elevated terrain (5:0 through 8:5) - collision only, no nav polygon
- Trees (Tree1.png) - collision only
- Cliffs (0:4, 3:4) - collision only

## Attempted Solutions

### Option 1: Remove grass tiles under hills
**Problem:** Creates visual artifacts - blue line/gap visible at hill edges where water background shows through.

### Option 2: Disable navigation on grass layer
Set `navigation_enabled = false` on `Layer_3_Grass` in all map pieces.
**Problem:** Then nothing has navigation - need to create a separate navigation-only layer.

### Option 3: Add navigation polygons to hill edge tiles (RECOMMENDED)
Add small navigation polygons to hill edge tiles (5:0, 6:0, 7:0, 8:0, etc.) that only cover the visible grass strip at the bottom of each tile.

**How it would work:**
- Grass layer provides navigation for open areas
- Hill edge tiles provide navigation only for their grass portions
- Rocky parts have no navigation = not walkable

### Option 4: Use NavigationObstacle2D
Add `NavigationObstacle2D` nodes as children of obstacle objects to dynamically carve them out of the navigation mesh.

### Option 5: Use NavigationRegion2D with baked navigation
Create a single `NavigationRegion2D` and use Godot's navigation mesh baking with obstacle carving.

## Files Involved

- `Scripts/Characters/Enemies/Enemy.cs` - Base enemy with NavigationAgent2D
- `Scripts/Characters/Enemies/TorchGoblin.cs` - Uses base navigation
- `Scripts/Characters/Enemies/TntGoblin.cs` - Uses base navigation, stops when in throw range
- `Scripts/Characters/Enemies/Shaman.cs` - Custom movement (maintains distance), doesn't use NavigationAgent2D
- `Resources/WorldTileset_Day.tres` - Tileset with navigation/collision definitions
- `Entities/MapPieces/map_piece_*.tscn` - Map pieces with TileMapLayers

## Next Steps

1. **Choose a solution** - Option 3 (nav polygons on hill edges) is cleanest for tile-based maps
2. **Edit WorldTileset_Day.tres** - Add navigation polygons to hill edge tiles covering only the grass portion
3. **Test** - Verify enemies navigate around hills properly
4. **Consider trees/buildings** - May need similar treatment or NavigationObstacle2D nodes

## Debug Info

Click on an enemy to see debug output:
```
═══════════════════════════════════════════════════════════
  [timestamp] ENEMY DEBUG - ID: X (EnemyType)
═══════════════════════════════════════════════════════════
  CanMove:           True/False
  Nav Finished:      True/False
  Next Nav Position: (x, y)
  Velocity:          (x, y)
═══════════════════════════════════════════════════════════
```

If `Nav Finished: True` but enemy is far from player, navigation mesh is broken at that location.
