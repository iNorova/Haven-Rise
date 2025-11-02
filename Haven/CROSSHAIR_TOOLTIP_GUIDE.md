# Crosshair Tooltip Customization Guide

## Fixed Issues
✅ **Vertical Text Issue**: Fixed by:
- Disabling word wrapping (`enableWordWrapping = false`)
- Setting proper width (250px) for text container
- Setting overflow mode to prevent text from wrapping vertically

## How to Customize Tooltip Text for Different Objects

### Step 1: Access the Component
1. Select the GameObject with the `CrosshairTooltip` component in the Unity Inspector
2. Look for the **"Text Customization"** sections

### Step 2: Customize Text for Different Object Types

#### **Destroyable Objects Section** (Trees, Rocks, etc.)
In the Inspector, you'll find these fields under **"Text Customization - Destroyable Objects"**:

- **Tree Text**: Text shown when looking at trees
  - Default: `"Press [Left Click] to Chop Tree"`
  - Detects objects with names containing: "tree", "log", "wood"
  
- **Rock Text**: Text shown when looking at rocks
  - Default: `"Press [Left Click] to Mine Rock"`
  - Detects objects with names containing: "rock", "stone", "boulder"
  
- **Destroyable Text**: Fallback text for other destroyable objects
  - Default: `"Press [Left Click] to Break"`

#### **General Objects Section**
- **Pickupable Text**: For pickupable items
- **Bed Text**: For beds
- **NPC Text**: For NPCs
- **Meat Text**: For meat/food items

### Step 3: How Object Detection Works

The system detects objects in this priority order:

1. **By Tag** (Fastest)
   - Objects tagged "Destroyable" → Check if it's a tree or rock by name
   - Objects tagged "Pickupable" → Show pickupable text
   - Objects tagged "NPC" → Show NPC text

2. **By Component**
   - `BedInteraction` component → Show bed text
   - `ItemIconProvider` component → Show pickupable text

3. **By Name** (Fallback)
   - Objects with "tree"/"log"/"wood" in name → Show tree text
   - Objects with "rock"/"stone"/"boulder" in name → Show rock text
   - Objects with "meat"/"food" in name → Show meat text

### Step 4: Examples

#### Example 1: Customize Tree Text
```
In Inspector → CrosshairTooltip → Tree Text field:
Change from: "Press [Left Click] to Chop Tree"
Change to: "Chop with Axe [LMB]"
```

#### Example 2: Customize Rock Text
```
In Inspector → CrosshairTooltip → Rock Text field:
Change from: "Press [Left Click] to Mine Rock"
Change to: "Mine Rock [Left Click]"
```

#### Example 3: Add Custom Text for Specific Objects
If you want text for objects not covered, you can:

**Option A: Add to object name**
- Rename your object to include "tree", "rock", etc. in the name
- The system will automatically detect it

**Option B: Modify the script**
- Add new text fields in the Inspector for your custom objects
- Update `GetTooltipText()` method to check for your objects

### Step 5: Adjust Display Settings

**Screen Offset**: Move tooltip position
- `X = 0, Y = -50` = 50 pixels below center (default)
- `X = 0, Y = 50` = 50 pixels above center
- `X = -100, Y = 0` = 100 pixels left of center

**Detection Range**: How far to detect objects
- Default: 5 units
- Increase for longer detection range
- Decrease for shorter detection range

**Fade Speed**: How fast tooltip fades in/out
- Default: 5
- Higher = faster fade
- Lower = slower fade

## Troubleshooting

### Text Still Appears Vertical?
1. Check if the `Tooltip Panel` has proper width (should be at least 250px)
2. Ensure `enableWordWrapping = false` on TextMeshPro component
3. Make sure text has enough horizontal space

### Tooltip Not Showing?
1. Check `Detection Range` - might be too short
2. Verify object has correct tag or name
3. Check `Interactable Layers` - make sure object's layer is included

### Wrong Text Showing?
1. Check object name contains expected keywords (tree, rock, etc.)
2. Verify object has correct tag
3. Object detection priority: Tag → Component → Name

## Quick Reference

| Object Type | Detection Method | Text Field |
|------------|-----------------|-----------|
| Tree | Name contains "tree"/"log"/"wood" OR tag "Destroyable" + name check | `treeText` |
| Rock | Name contains "rock"/"stone"/"boulder" OR tag "Destroyable" + name check | `rockText` |
| Other Destroyable | Tag "Destroyable" but not tree/rock | `destroyableText` |
| Pickupable | Tag "Pickupable" OR `ItemIconProvider` component | `pickupableText` |
| Bed | `BedInteraction` component | `bedText` |
| NPC | Tag "NPC" | `npcText` |
| Meat/Food | Name contains "meat"/"food" OR tag "Pickupable" + name check | `meatText` |

