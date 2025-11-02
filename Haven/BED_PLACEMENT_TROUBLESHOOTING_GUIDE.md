# Bed Placement Troubleshooting Guide

## Issue: Can't Place Bed After Picking It Up

This guide will help you fix the issue where you can't place a bed after picking it up.

## Step-by-Step Debugging

### Step 1: Check Console Logs
1. Open Unity
2. Go to **Window → General → Console** (or press `Ctrl+Shift+C`)
3. Play the game
4. Place a bed, then pick it up
5. Try to place it again
6. Look for messages starting with `[BedPlacement]`

**What to look for:**
- `"[BedPlacement] Bed 'X' is currently selected"` - Good! Bed is detected
- `"[BedPlacement] Bed 'X' is NOT selected"` - Problem! Bed isn't being detected
- `"[BedPlacement] CheckPlacement: playerCamera is null!"` - Camera reference missing
- `"[BedPlacement] Placement check: Valid=false"` - Placement location invalid

### Step 2: Verify Bed Component Setup

1. **In Unity Hierarchy, select your bed GameObject** (or bed prefab)
2. **In Inspector, verify it has these components:**
   - ✅ **BedPlacement** component (when in inventory/hotbar)
   - ✅ **BedInteraction** component (when placed)
   - ✅ **BedPickup** component (when placed)

3. **Check BedPlacement settings:**
   - `Show Preview` = ✅ Enabled
   - `Preview Update Interval` = 0.3 (or higher for less lag)
   - `Placement Range` = 5 (or your desired range)
   - `Ground Layer` = Should include your terrain/ground layer

### Step 3: Test Bed Detection

**Test if bed is being detected as selected:**

1. Place a bed on the ground
2. Press **F** to pick it up
3. Select the bed in your hotbar (number key 1-9)
4. **Check Console** - you should see: `"[BedPlacement] Bed 'X' is currently selected"`
5. If you see `"is NOT selected"`, the bed detection is failing

**Fix if bed is not detected:**
- Make sure bed name contains "Bed" (case insensitive)
- Check that BedPlacement component is enabled on the bed
- Verify the bed is actually in the selected hotbar slot

### Step 4: Verify References Are Initialized

**Check if references are null:**

1. When bed is selected, look for errors in console:
   - `"playerCamera is null"` → Camera reference missing
   - `"hotbarManager is null"` → HotbarManager missing in scene
   - `"inventoryManager is null"` → InventoryManager missing (less critical)

**Fix missing references:**
- Ensure your Main Camera has the **"MainCamera"** tag
- Make sure **HotbarManager** exists in the scene
- Make sure **InventoryManager** exists in the scene

### Step 5: Test Placement Logic

**Manual placement test:**

1. Select bed in hotbar
2. Look at the ground where you want to place
3. **Press Left Click**
4. Check console messages:
   - `"Left click detected. isValidPlacement=false"` → Placement location issue
   - `"Cannot place bed - Invalid placement"` → Ground detection failing

**If placement fails:**
- Make sure you're looking at terrain/ground (not air or other objects)
- Verify `Placement Range` is high enough (try increasing to 10)
- Check that `Ground Layer` includes your terrain layer

### Step 6: Verify Bed Pickup Process

**Check pickup initialization:**

1. Place a bed
2. Pick it up with **F**
3. **In Console**, look for:
   - `"BedPickup: Added BedPlacement component to 'X' with initialized references"` ✅ Good
   - `"BedPickup: Added BedPlacement component to 'X'"` ✅ Good
   - Any errors about missing components ❌ Problem

**If pickup fails:**
- Check that BedPickup component exists on placed beds
- Verify HotbarManager and InventoryManager exist in scene

## Common Issues and Fixes

### Issue 1: "Bed is NOT selected" Error
**Cause:** Bed detection failing
**Fix:**
- Ensure bed name contains "Bed"
- Check BedPlacement component is enabled
- Verify bed is in the selected hotbar slot (use number keys 1-9)

### Issue 2: "playerCamera is null" Error
**Cause:** Camera reference not found
**Fix:**
- Tag your main camera as **"MainCamera"**
- Ensure camera is active in scene
- Restart the game scene

### Issue 3: "Invalid placement" When Clicking
**Cause:** Ground detection failing
**Fix:**
- Increase `Placement Range` to 10 or higher
- Check `Ground Layer` mask includes your terrain
- Make sure you're looking at actual ground (not air)

### Issue 4: Bed Placements Works But Preview Doesn't Show
**Cause:** Preview disabled or lag prevention
**Fix:**
- Enable `Show Preview` in BedPlacement component
- Lower `Preview Update Interval` to 0.1 (if no lag)
- Check preview materials are set up correctly

### Issue 5: Bed Works Once But Not After Pickup
**Cause:** References not re-initializing
**Fix:**
- This should be fixed with the latest code
- Check console for initialization errors
- Make sure `InitializeReferences()` is being called

## Quick Verification Checklist

Before testing, verify:
- [ ] Bed has BedPlacement component (when in hotbar)
- [ ] Bed has BedInteraction component (when placed)
- [ ] Bed has BedPickup component (when placed)
- [ ] Main Camera tagged as "MainCamera"
- [ ] HotbarManager exists in scene
- [ ] InventoryManager exists in scene
- [ ] Bed name contains "Bed"
- [ ] Console shows no errors
- [ ] Preview is enabled if you want outline

## Testing Workflow

1. **Fresh Start Test:**
   - Start game
   - Put bed in hotbar (if not already)
   - Select bed slot
   - Try to place on ground
   - ✅ Should work

2. **Pickup/Place Cycle Test:**
   - Place bed → ✅ Should work
   - Pick up bed (F) → ✅ Should work
   - Select bed slot → ✅ Should work
   - Try to place again → ✅ Should work
   - Repeat 3-5 times → ✅ Should always work

3. **If any step fails:**
   - Check console logs
   - Follow troubleshooting steps above
   - Verify component setup

## Still Not Working?

If you've followed all steps and it still doesn't work:

1. **Copy the console output** (especially [BedPlacement] messages)
2. **Check the bed GameObject:**
   - Select it in Hierarchy
   - Take screenshot of Inspector showing all components
3. **Verify scene setup:**
   - Main Camera exists and is tagged
   - HotbarManager exists
   - InventoryManager exists

The debug logs will tell us exactly what's failing!

