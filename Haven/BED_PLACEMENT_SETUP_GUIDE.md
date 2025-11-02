# Bed Placement System Setup Guide

## Overview
The bed placement system now features:
- **Left Click**: Place bed on ground (with preview)
- **Q Key**: Drop bed from hotbar (remove without placing)
- **G Key**: Sleep in placed bed (triggers day/night cycle)

## Features
1. **Visual Preview**: Shows where the bed will be placed
2. **Green/Red Feedback**: Green = valid placement, Red = invalid placement
3. **Real-time Updates**: Preview follows your crosshair
4. **Collision Detection**: Prevents placing on invalid surfaces

## Setup Instructions

### Step 1: Assign the BedPlacement Script
1. Select your **Bed prefab** or **Bed GameObject** in the scene
2. In the Inspector, find or add the **BedPlacement** component
3. Configure the following settings:

#### Placement Settings:
- **Placement Range**: How far you can place the bed (default: 5)
- **Placement Offset**: Distance from ground surface (default: 0.1)
- **Ground Layer**: Layer mask for ground/terrain detection
  - To set: Click the dropdown and select your terrain/ground layers
  - Common: Default, Terrain, Ground
- **Drop Key**: Q (to drop bed from hotbar)

#### Preview Settings:
- **Show Preview**: ✅ Enable this (checked by default)
- **Valid Placement Material**: (Optional) Leave empty - uses built-in green tint
- **Invalid Placement Material**: (Optional) Leave empty - uses built-in red tint

### Step 2: Verify Bed Components
Make sure your bed GameObject has:
- ✅ **Collider** (BoxCollider, MeshCollider, etc.)
- ✅ **Renderer** (MeshRenderer or any renderer component)
- ✅ **BedPlacement** script (added above)
- ✅ **BedInteraction** script (for sleeping with G key)
- ✅ **BedPickup** script (optional, for picking up placed beds)

### Step 3: Setup Ground Layer (Important!)
1. Go to **Edit → Project Settings → Tags and Layers**
2. Create or identify your ground/terrain layer:
   - Example: Create layer named "Ground" or use existing "Terrain"
3. Assign this layer to your terrain/ground objects:
   - Select your terrain/floor objects
   - In Inspector, set their **Layer** to your ground layer
4. In BedPlacement component:
   - Set **Ground Layer** mask to include your ground layer
   - Click the layer mask dropdown and select your ground layer(s)

### Step 4: Test the System
1. **Add bed to hotbar**:
   - Put bed item in your hotbar inventory
   - Select the bed slot in hotbar
   
2. **Preview should appear**:
   - Green outline = valid placement location
   - Red outline = invalid placement location
   - Preview follows where you're looking
   
3. **Place the bed**:
   - Look at valid ground location (green preview)
   - **Left Click** to place
   - Bed will be placed and locked in position
   
4. **Drop the bed**:
   - Press **Q** to drop from hotbar without placing
   
5. **Sleep in bed**:
   - Approach placed bed
   - Press **G** to sleep and transition to night

## Troubleshooting

### Preview Not Showing
- ✅ Check **Show Preview** is enabled in BedPlacement
- ✅ Make sure bed is selected in hotbar
- ✅ Verify camera is assigned (should find automatically)
- ✅ Check console for errors

### Preview Always Red
- ✅ Check **Ground Layer** mask includes your terrain layer
- ✅ Verify terrain/ground objects are on correct layer
- ✅ Surface might be too steep (max 45 degrees)
- ✅ Check if something is blocking the raycast (player, other objects)

### Bed Not Placing
- ✅ Preview must be **green** (valid placement)
- ✅ Must be within **Placement Range**
- ✅ Check console for error messages
- ✅ Verify ground layer is correctly set

### Bed Dropping Instead of Placing
- ✅ Make sure you're using **Left Click** (not Q)
- ✅ Q key is for dropping only
- ✅ Preview must be green before clicking

### Bed Placement Not Aligned to Ground
- ✅ Check **Placement Offset** value (too high = floating)
- ✅ For terrain, bed should snap directly to surface
- ✅ For other surfaces, offset prevents clipping

## Customization

### Change Placement Range
In BedPlacement component → **Placement Range**
- Higher = can place further away
- Lower = must be closer to place

### Change Preview Colors
The system automatically tints the preview:
- **Green** = Valid placement (isValidPlacement = true)
- **Red** = Invalid placement (surface too steep, out of range, etc.)

To customize colors, you can modify the `UpdatePreviewColor()` method in BedPlacement.cs

### Adjust Surface Angle Tolerance
By default, surfaces steeper than 45° are invalid. To change:
1. Open **BedPlacement.cs**
2. Find `IsValidPlacement()` method
3. Change `if (angle > 45f)` to your desired angle

## How It Works

### Preview System:
1. When bed is selected in hotbar, preview bed is created
2. Preview follows raycast hit point on ground
3. Preview color changes based on placement validity
4. Preview is hidden when bed is not selected

### Placement System:
1. Left click checks if placement is valid
2. If valid (green), bed is instantiated at preview position
3. Placed bed is locked in place (kinematic rigidbody)
4. Original bed is removed from hotbar

### Drop System:
1. Q key removes bed from hotbar
2. Preview is destroyed
3. Bed GameObject is destroyed

## Notes
- Preview uses a semi-transparent copy of the bed model
- Preview materials are automatically tinted green/red
- Preview has no physics/colliders (doesn't interact with world)
- Preview automatically cleans up when bed is deselected

