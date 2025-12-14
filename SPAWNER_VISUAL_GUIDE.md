# Universal Animal Spawner - Visual Guide

This guide explains how to read the visual gizmos in the Scene view to understand spawn areas.

## How to View Gizmos

1. **Open Scene View** (not Game view)
2. **Select your spawner GameObject** in Hierarchy
3. **Look at Scene view** - you should see colored wireframes

## Visual Indicators

### Radius Method (Circular Spawn Area)

When **Spawn Method = Radius**:

- **Green Circle**: The spawn radius boundary
  - All animals spawn within this circle
  - Radius = the value you set in "Spawn Radius" (e.g., 100 units)
  
- **Green Lines**: Four lines showing North, South, East, West directions
  - Helps visualize the radius distance
  
- **Green Center Sphere**: The spawner's position (center of spawn area)
  - Size: 2 units radius

- **Blue Circle** (if Avoid Water enabled): Water level indicator
  - Shows where water is - animals won't spawn below this

- **Yellow Spheres** (after spawning): Already spawned positions
  - Each sphere shows where an animal was spawned
  - Size = half of "Min Distance Between Spawns"

### Grid Method (Rectangular Spawn Area)

When **Spawn Method = Grid**:

- **Cyan-Green Rectangle**: The spawn area boundary
  - Size = "Spawn Area Size" (e.g., 200x200 units)
  - Animals spawn within this rectangle
  
- **Corner Spheres**: Four spheres at the corners
  - Mark the exact boundaries of the spawn area
  
- **Grid Lines** (if "Show Grid Lines" enabled): Subdivision lines
  - Shows how the area is divided for spawning
  - Helps visualize distribution

- **Blue Rectangle** (if Avoid Water enabled): Water level indicator
  - Shows where water is

- **Yellow Spheres** (after spawning): Already spawned positions

## Understanding the Sizes

### Radius Method:
- **Spawn Radius = 100**: Circle with 100 unit radius = 200 unit diameter
- **Visual**: Green circle with center at spawner position
- **Area**: π × 100² ≈ 31,416 square units

### Grid Method:
- **Spawn Area Size = (200, 200)**: Rectangle 200 units wide × 200 units long
- **Visual**: Cyan-green rectangle centered on spawner
- **Area**: 200 × 200 = 40,000 square units

## Size Comparison Guide

### Small Spawn Area:
- **Radius**: 50 units (diameter = 100)
- **Grid**: (100, 100) = 100×100 units
- **Good for**: Dense spawning, small areas

### Medium Spawn Area:
- **Radius**: 100 units (diameter = 200)
- **Grid**: (200, 200) = 200×200 units
- **Good for**: Normal gameplay, moderate distribution

### Large Spawn Area:
- **Radius**: 200 units (diameter = 400)
- **Grid**: (400, 400) = 400×400 units
- **Good for**: Wide distribution, avoiding water, sparse spawning

### Very Large Spawn Area:
- **Radius**: 500 units (diameter = 1000)
- **Grid**: (500, 500) = 500×500 units
- **Good for**: Very wide distribution, large maps

## Per-Prefab Spawn Counts

### How It Works:

1. **Spawn Configs List**: Each entry has:
   - **Prefab**: The GameObject to spawn
   - **Spawn Count**: How many of THIS prefab to spawn
   - **Label**: Optional name for organization

2. **Example Setup**:
   ```
   Config 1: Deer Prefab, Count = 10
   Config 2: Ghoul Prefab, Count = 5
   Config 3: Rabbit Prefab, Count = 15
   ```
   Result: 10 deer + 5 ghouls + 15 rabbits = 30 total

3. **Weighted Selection**: 
   - Prefabs with higher remaining spawn counts are more likely to be selected
   - Ensures all prefabs reach their target count

### Setting Up Per-Prefab Counts:

1. In Inspector, find **"Spawn Configs"** list
2. Click **+** to add a new config
3. Drag your prefab into **"Prefab"** field
4. Set **"Spawn Count"** to desired number
5. (Optional) Add a **"Label"** like "Deer" or "Ghoul"
6. Repeat for each prefab type

### Benefits:

- **Flexible**: Each prefab can have different spawn counts
- **Organized**: Easy to see what spawns and how many
- **Future-proof**: Just add new configs for new prefabs
- **No code changes needed**: Works for any prefab you add

## Tips for Using Visual Gizmos

1. **Check Spawn Area Placement**:
   - Make sure the green/cyan area overlaps with your terrain
   - Adjust spawner position if needed

2. **Verify Water Avoidance**:
   - Blue gizmo shows water level
   - Make sure spawn area is above water level
   - Or adjust Water Height if needed

3. **Check Spawn Distribution**:
   - Yellow spheres show where animals actually spawned
   - If they're clustered, increase Min Distance
   - If area looks empty, increase spawn count or area size

4. **Grid vs Radius**:
   - **Radius**: Better for circular/even distribution
   - **Grid**: Better for wide rectangular areas
   - **Grid**: Better for avoiding water (wider spread)

## Common Visual Issues

### No Gizmos Showing:
- Make sure spawner is **selected** in Hierarchy
- Check **"Show Gizmos"** is enabled in spawner component
- Make sure you're in **Scene view**, not Game view

### Gizmos Too Small/Large:
- Adjust spawn radius or area size
- Use Scene view zoom (mouse wheel) to see better

### Gizmos Overlapping:
- This is normal - multiple gizmos show different information
- Green/cyan = spawn area
- Blue = water level
- Yellow = spawned positions

## Example Configurations with Visuals

### Deer Spawner (Radius):
```
Spawn Method: Radius
Spawn Radius: 100
Visual: Green circle, 200 unit diameter
Result: Deer spawn in circular area around spawner
```

### Ghoul Spawner (Grid):
```
Spawn Method: Grid
Spawn Area Size: (200, 200)
Visual: Cyan-green rectangle, 200×200 units
Result: Ghouls spawn in wide rectangular area
```

### Mixed Animal Spawner:
```
Spawn Configs:
  - Deer: Count = 10
  - Rabbit: Count = 15
  - Bird: Count = 5
Spawn Method: Radius
Spawn Radius: 150
Visual: Green circle, 300 unit diameter
Result: 30 total animals (10 deer, 15 rabbits, 5 birds) in circular area
```

