# Universal Animal Spawner - Detailed Setup Guide

This guide will walk you through setting up the UniversalAnimalSpawner for both animals (deer) and ghouls.

## Step 1: Create the Spawner GameObject

1. In Unity, right-click in your **Hierarchy** window
2. Select **Create Empty**
3. Name it **"AnimalSpawner"** (or "GhoulSpawner" for ghouls)
4. Position it where you want the center of the spawn area to be

## Step 2: Add the Component

1. Select the GameObject you just created
2. In the **Inspector**, click **Add Component**
3. Search for **"UniversalAnimalSpawner"**
4. Add the component

## Step 3: Configure Basic Settings

### For Animals (Deer):

1. **Spawn Prefabs**:
   - Click the **+** button to add a slot
   - Drag your **Deer prefab** from Project window into the slot
   - You can add multiple prefabs if you have different animal types

2. **Spawn Count**: Set to `10` (or however many you want)

3. **Spawn Method**: Select **"Radius"** (simpler for animals)

4. **Spawn Radius**: Set to `100` or higher

5. **Min Distance Between Spawns**: Set to `5` (prevents clustering)

## Step 4: Configure Terrain Settings

1. **Target Terrain**: 
   - Find your terrain GameObject in the scene
   - Drag it into the **Target Terrain** field

2. **Use Terrain Height**: 
   - ✓ **Check this** for accurate terrain positioning
   - This uses terrain.SampleHeight instead of raycasting

## Step 5: Configure Layer Masks

### Setting Up Layers (if not already done):

1. Go to **Edit → Project Settings → Tags and Layers**
2. Make sure you have these layers set up:
   - **Ground** (or **Terrain**)
   - **Water**
   - **Structure** (or **Building**)

### Assign Layers to Objects:

1. Select your **Terrain** GameObject
2. In Inspector, set **Layer** to **"Ground"** (or your terrain layer name)
3. Select your **Water** GameObject
4. Set **Layer** to **"Water"**
5. Select your **House/Building** GameObjects
6. Set **Layer** to **"Structure"** (or your structure layer name)

### Configure Layer Masks in Spawner:

1. Select your **AnimalSpawner** GameObject
2. In **UniversalAnimalSpawner** component:
   - **Ground Layer**: Click dropdown, select **"Ground"** (or your terrain layer)
   - **Structure Layer**: Click dropdown, select **"Structure"** (or your building layer)
   - **Water Layer**: Click dropdown, select **"Water"**

## Step 6: Configure Spawn Conditions

### For Animals (Deer):

1. **Required Tags**: 
   - Click **+** to add a tag
   - Type **"Grass"** (or whatever tag your grass/ground has)
   - Make sure your grass objects have this tag assigned

2. **Require Tag Match**: ✓ **Check this**

3. **Allow Terrain Spawn**: ✓ **Check this**

4. **Allow House Spawn**: ✗ **Uncheck this** (animals don't go in houses)

### For Ghouls:

1. **Required Tags**: Leave **empty** (or add "Ground", "Terrain" if you want)

2. **Require Tag Match**: ✗ **Uncheck this** (unless you want tag matching)

3. **Allow Terrain Spawn**: ✓ **Check this**

4. **Allow House Spawn**: ✓ **Check this** (if you want ghouls in houses)

## Step 7: Configure Water Avoidance

1. **Avoid Water**: ✓ **Check this**

2. **Water Height**: 
   - If your water has the **"Water"** tag, this will auto-detect
   - Otherwise, manually set the Y position of your water plane

3. **Water Check Radius**: `1` (default is fine)

4. **Water Check Height**: `1` (default is fine)

5. **Make sure your Water GameObject**:
   - Has the **"Water"** tag assigned
   - OR manually set the **Water Height** value in the spawner

## Step 8: Advanced Settings (For Ghouls - Wider Spawn Area)

### If spawning Ghouls with Grid method:

1. **Spawn Method**: Select **"Grid"**

2. **Spawn Area Size**: 
   - X: `200` (or larger)
   - Y: `200` (or larger)
   - This creates a 200x200 unit spawn area

3. **Random Offset**: `2` (adds natural variation)

4. **Min Distance Between Spawns**: `10` or higher (prevents clustering)

## Step 9: Rotation and Parenting

1. **Random Rotation Range**: `360` (full random rotation)

2. **Parent To Spawner**: 
   - ✓ Check if you want spawned animals as children of spawner
   - ✗ Uncheck if you want them as root objects

## Step 10: Verify Setup Checklist

Before testing, verify:

- [ ] Spawn Prefabs list has at least one prefab assigned
- [ ] Target Terrain is assigned (if using terrain height)
- [ ] Ground Layer mask is set
- [ ] Water Layer mask is set (if avoiding water)
- [ ] Structure Layer mask is set (if allowing house spawn)
- [ ] Water GameObject has "Water" tag OR Water Height is manually set
- [ ] Terrain GameObject is on the Ground layer
- [ ] Water GameObject is on the Water layer
- [ ] If using tags, objects have the correct tags assigned

## Step 11: Test the Spawner

1. **Enter Play Mode**
2. **Check Console** for spawn messages:
   - Should see: "UniversalAnimalSpawner: Starting to spawn..."
   - Should see: "UniversalAnimalSpawner: Successfully spawned X animals..."
3. **Look in Scene view** to see if animals spawned
4. **Check Hierarchy** - spawned animals should appear (or be children of spawner if parenting enabled)

## Troubleshooting

### No animals spawning:

1. **Check Console for errors**:
   - "No spawn prefabs assigned" → Add prefabs to list
   - "No terrain assigned" → Assign Target Terrain
   - "Could not spawn all requested" → Increase spawn radius/area or reduce min distance

2. **Check Layer Masks**:
   - Make sure Ground Layer includes your terrain
   - Make sure layers are actually set on GameObjects

3. **Check Tags** (if using tag matching):
   - Verify objects have the tags you specified
   - Check spelling (case-sensitive)

4. **Check Water Height**:
   - If animals spawn in water, adjust Water Height value
   - Make sure water has "Water" tag or set height manually

5. **Check Spawn Position**:
   - Select spawner in Scene view
   - Enable Gizmos (top right of Scene view)
   - You should see green wireframe showing spawn area
   - Make sure spawn area overlaps with terrain

6. **Check Raycast Settings**:
   - Raycast Start Height: Should be high enough (100+)
   - Max Raycast Distance: Should be enough (200+)
   - If terrain is very high/low, adjust these values

### Animals spawning in wrong places:

1. **Too close together**: Increase **Min Distance Between Spawns**
2. **Spawned in water**: 
   - Enable **Avoid Water**
   - Set correct **Water Height**
   - Set **Water Layer** mask
3. **Not spawning on terrain**: 
   - Check **Ground Layer** mask
   - Enable **Use Terrain Height**
   - Assign **Target Terrain**

### Performance Issues:

1. **Too many spawn attempts**: Reduce spawn count or increase spawn area
2. **Spawner runs every frame**: It only runs once in Start(), so this shouldn't happen

## Example Configurations

### Deer Spawner Configuration:
```
Spawn Prefabs: [Deer_001]
Spawn Count: 10
Spawn Method: Radius
Spawn Radius: 100
Min Distance: 5
Target Terrain: [Your Terrain]
Use Terrain Height: ✓
Ground Layer: Ground
Structure Layer: (empty or Structure)
Water Layer: Water
Required Tags: [Grass]
Require Tag Match: ✓
Allow Terrain Spawn: ✓
Allow House Spawn: ✗
Avoid Water: ✓
```

### Ghoul Spawner Configuration:
```
Spawn Prefabs: [Ghoul]
Spawn Count: 5
Spawn Method: Grid
Spawn Area Size: (200, 200)
Min Distance: 10
Target Terrain: [Your Terrain]
Use Terrain Height: ✓
Ground Layer: Ground
Structure Layer: Structure
Water Layer: Water
Required Tags: (empty)
Require Tag Match: ✗
Allow Terrain Spawn: ✓
Allow House Spawn: ✓
Avoid Water: ✓
```

## Visual Debugging

1. **Select the spawner** in Hierarchy
2. **Look at Scene view** (not Game view)
3. You should see:
   - **Green wireframe**: Spawn area
   - **Blue wireframe**: Water height level
   - **Yellow spheres**: Already spawned positions (after spawning)

This helps you see if the spawn area is in the right place!

