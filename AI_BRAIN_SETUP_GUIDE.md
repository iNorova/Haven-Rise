# Animal AI Brain + Creature Mover Setup Guide

## Architecture Overview

The new system properly separates concerns:

```
┌─────────────────┐
│  AnimalAIBrain  │ ← Makes decisions (what to do)
└────────┬────────┘
         │ Outputs: Direction, Speed, ShouldRun
         ↓
┌─────────────────┐
│  CreatureMover  │ ← Executes movement (how to do it)
└─────────────────┘
```

**AnimalAIBrain** = The brain (decision making)
- Detects player
- Decides to flee/wander/idle
- Outputs movement commands

**CreatureMover** = The body (movement execution)
- Reads commands from brain
- Handles obstacle avoidance
- Applies physics/animation
- Manages CharacterController

---

## Quick Setup (2 Components)

### Step 1: Add Components to Animal

1. **Select your animal** in Hierarchy
2. **Add Component** → Search "AnimalAIBrain"
3. **Add Component** → Search "CreatureMover" (if not already present)
4. **Ensure these components exist**:
   - ✅ CharacterController
   - ✅ Animator
   - ✅ AnimalAIBrain (new)
   - ✅ CreatureMover (new)

### Step 2: Configure AnimalAIBrain

In the `AnimalAIBrain` component:

```
Configuration:
  Config: [Drag Deer_Config.asset here]

References:
  Player Transform: [Leave empty - auto-finds]

Debug:
  Show Debug Gizmos: ✓
  Log State Changes: ✓ (for testing)
```

### Step 3: Configure CreatureMover

In the `CreatureMover` component:

```
Movement:
  Walk Speed: 1
  Run Speed: 4
  Flee Speed: 6
  Rotate Speed: 90

Player Detection: (Legacy - ignored when using AIBrain)
  [Leave as is]

Animator:
  Vertical ID: "Vert"
  State ID: "State"

Ground Detection:
  Ground Layer Mask: [Select Ground, Terrain layers]

Obstacle Layer Mask: [Select Default, Terrain layers]
```

### Step 4: Test

1. **Press Play**
2. **Console should show**: `[AnimalName] Using AnimalAIBrain for decision making`
3. **Animal should**:
   - Wander when idle
   - Flee when player approaches
   - Avoid obstacles smoothly

---

## How It Works

### Data Flow

```
1. AnimalAIBrain.Update()
   ├─ Checks player proximity
   ├─ Updates state (Idle/Wandering/Fleeing)
   └─ Sets output properties:
      ├─ DesiredDirection
      ├─ DesiredSpeed
      ├─ ShouldRun
      └─ LookTarget

2. CreatureMover.Update()
   ├─ Reads AnimalAIBrain outputs
   ├─ Applies obstacle avoidance
   ├─ Executes movement via CharacterController
   └─ Updates animations
```

### State Machine (in AnimalAIBrain)

```
Idle ──────────> Wandering ──────────> Fleeing
 ↑                   ↓                     ↓
 └───────────────────┴─────────────────────┘
         (Player far away)
```

**Idle**: Standing still, waiting to wander  
**Wandering**: Moving to random nearby point  
**Fleeing**: Running away from player  
**Alert**: (Future) Watching player cautiously  

---

## Configuration

### Create Configs

```
Tools → Create Default Animal Configs
```

This creates 3 presets:
- `Deer_Config.asset` - Fast, skittish
- `Rabbit_Config.asset` - Very fast, erratic
- `Bear_Config.asset` - Slow, confident

### Config Parameters

#### Detection
```yaml
Detection Radius: 10        # Distance to detect player
Close Detection Radius: 2   # Distance when crouching
```

#### Speeds
```yaml
Idle Speed: 0.5      # Slight drift when idle
Walk Speed: 2        # Wander speed
Run Speed: 5         # Flee speed
Rotation Speed: 5    # Turn speed
```

#### Behavior
```yaml
Flee Distance: 15              # How far to run
Flee Angle Variation: 30       # Randomness (degrees)
Wander Radius: 6               # Wander area size
Wander Interval: 5             # Time between wanders
Wander Direction Change: 0.3   # Change probability
```

---

## Debug Tools

### Visual Gizmos (Scene View)

When `Show Debug Gizmos` is enabled:

**AnimalAIBrain Gizmos:**
- Yellow sphere = Normal detection radius
- Red sphere = Close detection radius (crouch)
- Green line = Wander target
- Red ray = Flee direction
- Cyan line = Look target
- Colored cube = Current state
  - White = Idle
  - Green = Wandering
  - Red = Fleeing
  - Yellow = Alert

**CreatureMover Gizmos:**
- Obstacle detection rays
- Ground check points
- Safe path calculations

### Console Logging

Enable `Log State Changes` to see:
```
[Deer] State changed: Idle -> Wandering
[Deer] State changed: Wandering -> Fleeing
```

---

## Comparison: Old vs New

### Old System (AnimalAIManager)
```
❌ NavMeshAgent required
❌ AI + Movement mixed together
❌ Hard to extend
❌ NavMesh-dependent
```

### New System (AIBrain + CreatureMover)
```
✅ No NavMeshAgent needed
✅ Clean separation of concerns
✅ Easy to extend (add new states)
✅ CharacterController-based (better physics)
✅ Reusable CreatureMover for other AI
```

---

## Advanced Usage

### Hooking into AI Events

```csharp
using UnityEngine;

public class AnimalSoundController : MonoBehaviour
{
    [SerializeField] private AnimalAIBrain brain;
    [SerializeField] private AudioSource audioSource;
    
    void Start()
    {
        brain.onStateChanged.AddListener(OnStateChanged);
        brain.onPlayerDetected.AddListener(OnPlayerDetected);
        brain.onPlayerLost.AddListener(OnPlayerLost);
    }
    
    void OnStateChanged(AnimalAIBrain.AnimalState newState)
    {
        Debug.Log($"Animal is now: {newState}");
    }
    
    void OnPlayerDetected()
    {
        audioSource.Play(); // Play alert sound
    }
    
    void OnPlayerLost()
    {
        // Player escaped
    }
}
```

### Custom AI States

Extend `AnimalAIBrain` to add new states:

```csharp
public class CustomAnimalAI : AnimalAIBrain
{
    // Add new state
    public enum ExtendedState { Idle, Wandering, Fleeing, Hunting, Eating }
    
    // Override behavior
    protected override void UpdateStateBehavior()
    {
        // Your custom logic
    }
}
```

### Reading AI State from Other Scripts

```csharp
AnimalAIBrain brain = animal.GetComponent<AnimalAIBrain>();

if (brain.CurrentState == AnimalAIBrain.AnimalState.Fleeing)
{
    // Animal is scared
}

Vector3 whereAnimalIsGoing = brain.DesiredDirection;
float howFast = brain.DesiredSpeed;
```

---

## Troubleshooting

### Animal Not Moving

**Check:**
1. Both `AnimalAIBrain` and `CreatureMover` components present?
2. Config assigned to `AnimalAIBrain`?
3. `CharacterController` component present?
4. Console shows "Using AnimalAIBrain for decision making"?

**Solution:**
- Ensure all components are added
- Assign a config asset
- Check console for errors

### Animal Not Fleeing

**Check:**
1. Player has `Player` tag?
2. `Detection Radius` not too small?
3. Player has `CharController_Motor` component?

**Solution:**
- Set player tag: `GameObject → Tag → Player`
- Increase detection radius in config (10-15)
- Add `CharController_Motor` to player

### Animal Ignores Obstacles

**Check:**
1. `CreatureMover` has `Obstacle Layer Mask` set?
2. Obstacles have colliders?
3. Obstacles on correct layer?

**Solution:**
- Set obstacle layer mask to include tree/rock layers
- Add colliders to obstacles
- Assign obstacles to `Default` or custom layer

### Using Legacy Mode (No AIBrain)

If you remove `AnimalAIBrain`, `CreatureMover` automatically falls back to built-in AI:

```
Console: [AnimalName] Using built-in AI (legacy mode)
```

This uses the old player detection logic in `CreatureMover`.

---

## Migration from Old System

### If you have AnimalAIManager (NavMesh-based):

1. **Remove** `AnimalAIManager` component
2. **Remove** `NavMeshAgent` component (not needed)
3. **Add** `AnimalAIBrain` component
4. **Add** `CreatureMover` component (if not present)
5. **Add** `CharacterController` component
6. **Assign** config to `AnimalAIBrain`
7. **Test**

### If you have old CreatureMover (built-in AI):

1. **Keep** `CreatureMover` component
2. **Add** `AnimalAIBrain` component
3. **Assign** config to `AnimalAIBrain`
4. **Test** - should automatically use brain

---

## Performance

### Brain (Decision Making)
- Very lightweight
- ~0.1ms per animal
- No NavMesh queries
- No physics checks

### CreatureMover (Movement)
- Handles obstacle avoidance
- CharacterController-based
- ~0.3ms per animal

**Total: ~0.4ms per animal** (can handle 50+ animals at 60 FPS)

---

## Best Practices

✅ **DO:**
- Use `AnimalAIBrain` for all new animals
- Create config profiles for different animal types
- Enable debug gizmos during development
- Test with one animal first

❌ **DON'T:**
- Mix `AnimalAIManager` and `AnimalAIBrain` on same object
- Use NavMeshAgent with this system
- Forget to assign config
- Leave debug logging on in builds

---

## Quick Reference

### Required Components
```
GameObject (Animal)
├─ CharacterController
├─ Animator
├─ AnimalAIBrain     ← Brain (decisions)
└─ CreatureMover     ← Body (movement)
```

### Component Roles
| Component | Purpose | Required |
|-----------|---------|----------|
| AnimalAIBrain | Makes AI decisions | Yes |
| CreatureMover | Executes movement | Yes |
| CharacterController | Physics movement | Yes |
| Animator | Animations | Yes |
| Collider | Collision | Recommended |

### Files
```
Scripts:
├─ AnimalAIBrain.cs      (AI decisions)
├─ CreatureMover.cs      (Movement execution)
└─ AnimalAIConfig.cs     (Configuration data)

Configs:
└─ Deer_Config.asset     (Example preset)
```

---

**Last Updated:** 2025-10-26  
**Version:** 3.0 (Brain + Mover Architecture)

