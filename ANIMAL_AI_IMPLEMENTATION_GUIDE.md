# Animal AI System - Complete Implementation Guide

## Table of Contents
1. [Quick Start](#quick-start)
2. [First Time Setup](#first-time-setup)
3. [Creating Configuration Profiles](#creating-configuration-profiles)
4. [Applying AI to Animals](#applying-ai-to-animals)
5. [Tuning Parameters](#tuning-parameters)
6. [Debug & Testing](#debug--testing)
7. [Advanced Usage](#advanced-usage)
8. [Troubleshooting](#troubleshooting)

---

## Quick Start

### For Existing Animals (Already in Scene)

1. **Select your animal GameObject** in the Hierarchy
2. **Remove old AI** (if present):
   - Remove old `AnimalAIManager` component (if it exists)
   - Keep `NavMeshAgent`, `Animator`, `Rigidbody`, `CapsuleCollider`
3. **Add new AI**:
   - Click `Add Component` → Search "AnimalAIManager"
   - Add the component
4. **Create configs**:
   - Go to menu: `Tools > Create Default Animal Configs`
   - This creates 3 presets in `Assets/Animal Assets/Animals_FREE/Configs/`
5. **Assign config**:
   - In the `AnimalAIManager` component
   - Drag a config file (Deer/Rabbit/Bear) into the `Config` slot
6. **Test**:
   - Press Play
   - Animal should wander and flee from player

---

## First Time Setup

### Step 1: Verify Scene Setup

#### 1.1 NavMesh Baking
Your terrain/ground must have a NavMesh baked on it.

```
Window → AI → Navigation
```

**Bake Settings:**
- Agent Radius: `0.5`
- Agent Height: `1.0`
- Max Slope: `45`
- Step Height: `0.4`

Click **"Bake"** button at bottom.

**Verify:** You should see blue overlay on walkable surfaces.

#### 1.2 Layer Setup
Ensure your scene has proper layers:

```
Edit → Project Settings → Tags and Layers
```

**Recommended Layers:**
- `Default` (0) - General objects
- `Ground` (8) - Terrain, floors
- `Terrain` (9) - Terrain objects
- `Player` (10) - Player character

**Set Layer Masks:**
- Trees/Rocks → `Default` or create `Obstacles` layer
- Terrain → `Ground` or `Terrain`

#### 1.3 Player Setup
Your player must have:
- Tag: `Player`
- Component: `CharController_Motor` (already exists in your project)

---

## Creating Configuration Profiles

### Method 1: Use Preset Generator (Recommended)

1. **Open Unity Editor**
2. **Menu Bar** → `Tools` → `Create Default Animal Configs`
3. **Configs Created:**
   ```
   Assets/Animal Assets/Animals_FREE/Configs/
   ├── Deer_Config.asset
   ├── Rabbit_Config.asset
   └── Bear_Config.asset
   ```

### Method 2: Create Custom Config

1. **Right-click in Project window**
2. **Create** → `AI` → `Animal Configuration`
3. **Name it** (e.g., "Wolf_Config")
4. **Configure parameters** (see [Tuning Parameters](#tuning-parameters))

### Preset Behaviors

| Config | Speed | Detection | Behavior |
|--------|-------|-----------|----------|
| **Deer** | Fast (7 m/s) | Alert (12m) | Skittish, long flee distance |
| **Rabbit** | Very Fast (6 m/s) | Very Alert (8m) | Erratic, frequent direction changes |
| **Bear** | Slow (4 m/s) | Confident (6m) | Reluctant to flee, large territory |

---

## Applying AI to Animals

### For Prefabs

#### Step 1: Open Prefab
1. **Project window** → Navigate to your animal prefab
2. **Double-click** to open in Prefab Mode

#### Step 2: Verify Components
Ensure the prefab has:
- ✅ `NavMeshAgent`
- ✅ `Animator`
- ✅ `Rigidbody` (optional, but recommended)
- ✅ `CapsuleCollider` (or similar collider)

**If missing:**
```
Add Component → Search for missing component
```

#### Step 3: Add AI Manager
1. **Add Component** → Search "AnimalAIManager"
2. **Click** to add

#### Step 4: Configure
In the `AnimalAIManager` component:

**Configuration Section:**
```
Config: [Drag Deer_Config.asset here]
```

**References Section:**
```
Player Transform: [Leave empty - auto-finds Player tag]
```

**Debug Section:**
```
Show Debug Gizmos: ✓ (check for testing)
Log State Changes: ✓ (check for debugging)
```

#### Step 5: Save & Apply
1. **File** → `Save` (Ctrl/Cmd + S)
2. **Exit Prefab Mode**
3. Prefab is now ready to use

### For Scene Instances

#### Quick Setup:
1. **Select animal** in Hierarchy
2. **Inspector** → `Add Component` → "AnimalAIManager"
3. **Drag config** into `Config` slot
4. **Done!**

---

## Tuning Parameters

### Understanding Config Parameters

#### Detection Settings
```yaml
Detection Radius: 10          # Distance to detect walking/running player
Close Detection Radius: 2     # Distance to detect crouching player
```

**Use Cases:**
- **Deer/Skittish**: 12-15m detection
- **Predator/Confident**: 5-8m detection
- **Prey/Timid**: 8-12m detection

#### Movement Speeds
```yaml
Idle Speed: 1        # Speed when standing still (slight drift)
Walk Speed: 2        # Speed during wander
Run Speed: 5         # Speed when fleeing
Rotation Speed: 5    # How fast animal turns
```

**Realistic Speeds:**
- **Deer**: Idle 0.5, Walk 2, Run 7
- **Rabbit**: Idle 0.3, Walk 1.5, Run 6
- **Bear**: Idle 0.8, Walk 1.5, Run 4
- **Wolf**: Idle 0.6, Walk 2.5, Run 8

#### Flee Behavior
```yaml
Flee Distance: 15              # How far to run away
Min Flee Distance: 5           # Minimum distance between flee points
Max Flee Distance: 100         # Maximum search range for flee point
Flee Angle Variation: 30       # Randomness in flee direction (0-45°)
Flee Update Interval: 1        # Seconds between recalculating flee path
```

**Tuning Tips:**
- **Erratic animals** (rabbit): High angle variation (35-45°)
- **Direct fleers** (deer): Low angle variation (15-25°)
- **Long distance**: Increase max flee distance
- **Performance**: Increase update interval (1.5-2s)

#### Wander Behavior
```yaml
Wander Radius: 6               # How far from current position to wander
Wander Interval: 5             # Seconds between wander movements
Wander Stopping Distance: 0.5  # When to consider "arrived"
Wander Direction Change: 0.3   # Probability of changing direction (0-1)
```

**Behavior Patterns:**
- **Grazing (deer)**: Large radius (8-12), long interval (6-10s), low change (0.1-0.2)
- **Foraging (rabbit)**: Small radius (3-5), short interval (2-4s), high change (0.4-0.6)
- **Patrolling (bear)**: Large radius (10-15), long interval (8-12s), low change (0.1)

#### Obstacle Avoidance
```yaml
Obstacle Check Radius: 0.5              # Size of detection sphere
Obstacle Check Distance: 1.5            # How far ahead to check
Obstacle Check Distance Multiplier: 2   # Multiplied by velocity
Obstacle Layer Mask: Everything         # What counts as obstacle
Side Step Distance: 1.5                 # How far to step sideways
Avoidance Check Interval: 0.2           # Seconds between checks
```

**Layer Mask Setup:**
1. **Click** the `Obstacle Layer Mask` dropdown
2. **Check layers** you want to avoid:
   - ✓ Default
   - ✓ Terrain
   - ✓ Ground
   - ✗ Player (don't avoid player, flee instead)
   - ✗ Animal (if you want animals to pass through each other)

**Performance Tuning:**
- **High performance needed**: Increase `Avoidance Check Interval` to 0.3-0.5
- **Tight spaces**: Decrease `Obstacle Check Distance` to 1.0
- **Fast animals**: Increase `Obstacle Check Distance Multiplier` to 3-4

#### Bounce Settings
```yaml
Bounce Back Distance: 2.5      # How far to bounce away
Bounce Angle Jitter: 15        # Random variation in bounce (0-45°)
Bounce Cooldown: 0.5           # Seconds before can bounce again
Corner Detection Angle: 120    # Angle to detect corners
```

**Tuning for Feel:**
- **Realistic**: Distance 2-3, Jitter 10-20°, Cooldown 0.5-0.8
- **Arcade/Bouncy**: Distance 3-5, Jitter 25-40°, Cooldown 0.2-0.4
- **Heavy animals**: Distance 1.5-2, Jitter 5-10°, Cooldown 0.8-1.2

#### Stuck Recovery
```yaml
Stuck Speed Threshold: 0.05          # Speed below which = stuck
Stuck Time Threshold: 0.5            # Seconds before triggering recovery
Stuck Rotation Threshold: 5          # Rotation speed indicating stuck
```

**Adjust for:**
- **Tight spaces**: Lower time threshold (0.3-0.4)
- **Open areas**: Higher time threshold (0.8-1.0)
- **Performance**: Higher time threshold (reduces recovery attempts)

#### Animation
```yaml
Animation Blend Speed: 5           # How fast animations transition
Animation Speed Multiplier: 1      # Scale animation speed
```

**Match to Animator:**
- Check your Animator Controller
- If animations are too slow: Increase multiplier (1.2-1.5)
- If animations are too fast: Decrease multiplier (0.7-0.9)

#### Health
```yaml
Max HP: 100
```

**Recommended Values:**
- **Small prey** (rabbit): 20-30
- **Medium prey** (deer): 50-80
- **Predator** (wolf): 100-150
- **Large** (bear): 150-250

#### Physics
```yaml
Use Trigger Collider: true       # Prevents physics collisions
Use Kinematic Rigidbody: true    # Prevents physics forces
```

**When to disable:**
- If you want animals to push objects: `Use Kinematic Rigidbody: false`
- If you want physical collisions: `Use Trigger Collider: false`

---

## Debug & Testing

### Visual Debug Tools

#### Enable Debug Gizmos
In `AnimalAIManager` component:
```
Show Debug Gizmos: ✓
```

**What You'll See (in Scene view):**

1. **Yellow Wire Sphere**: Normal detection radius
2. **Red Wire Sphere**: Close detection radius (crouch)
3. **Green Line Path**: Current wander path
4. **Red Line Path**: Current flee path
5. **Magenta Sphere**: Detected obstacle point
6. **Cyan Ray**: Bounce direction
7. **Colored Cube Above Animal**: Current state
   - White = Idle
   - Green = Wandering
   - Red = Fleeing
   - Yellow = Stuck
   - Black = Dead

#### Enable State Logging
```
Log State Changes: ✓
```

**Console Output:**
```
[Deer] State changed: Idle -> Wandering
[Deer] State changed: Wandering -> Fleeing
[Deer] Animal is stuck - attempting recovery
[Deer] State changed: Stuck -> Fleeing
```

### Testing Checklist

#### Test 1: Idle & Wander
1. **Play** the scene
2. **Wait** and observe
3. **Expected**: Animal should wander every 5-10 seconds

**If not wandering:**
- Check NavMesh is baked
- Check `Wander Interval` isn't too high
- Enable `Log State Changes` to see state

#### Test 2: Detection & Flee
1. **Play** the scene
2. **Move player** toward animal
3. **Expected**: Animal flees when within detection radius

**If not fleeing:**
- Check player has `Player` tag
- Check `Detection Radius` isn't too small
- Check player has `CharController_Motor` component
- Enable debug gizmos to see detection radius

#### Test 3: Obstacle Avoidance
1. **Play** the scene
2. **Chase animal** toward trees/rocks
3. **Expected**: Animal bounces/sidesteps around obstacles

**If colliding:**
- Check `Obstacle Layer Mask` includes tree/rock layers
- Check trees/rocks have colliders
- Increase `Obstacle Check Distance`
- Lower `Avoidance Check Interval`

#### Test 4: Stuck Recovery
1. **Play** the scene
2. **Chase animal** into a corner
3. **Expected**: After 0.5s, animal attempts escape

**If stuck forever:**
- Check NavMesh covers escape routes
- Lower `Stuck Time Threshold`
- Check console for errors

### Performance Testing

#### Check Frame Rate
```
Window → Analysis → Profiler
```

**Look for:**
- `AnimalAIManager.Update` should be < 0.5ms per animal
- `ObstacleAvoidanceRoutine` should run every 0.2s

**If performance is bad:**
1. Increase `Avoidance Check Interval` to 0.5
2. Increase `Flee Update Interval` to 2.0
3. Reduce number of animals in scene
4. Disable `Show Debug Gizmos` in builds

---

## Advanced Usage

### Hooking into Events

#### Example: Play Sound on State Change
```csharp
using UnityEngine;

public class AnimalSoundController : MonoBehaviour
{
    [SerializeField] private AnimalAIManager aiManager;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip idleSound;
    [SerializeField] private AudioClip fleeSound;
    
    void Start()
    {
        aiManager.onStateChanged.AddListener(OnStateChanged);
    }
    
    void OnStateChanged(AnimalAIManager.AnimalState newState)
    {
        switch (newState)
        {
            case AnimalAIManager.AnimalState.Idle:
                audioSource.PlayOneShot(idleSound);
                break;
            case AnimalAIManager.AnimalState.Fleeing:
                audioSource.PlayOneShot(fleeSound);
                break;
        }
    }
}
```

#### Example: Spawn Item on Death
```csharp
using UnityEngine;

public class AnimalLoot : MonoBehaviour
{
    [SerializeField] private AnimalAIManager aiManager;
    [SerializeField] private GameObject lootPrefab;
    
    void Start()
    {
        aiManager.onStateChanged.AddListener(OnStateChanged);
    }
    
    void OnStateChanged(AnimalAIManager.AnimalState newState)
    {
        if (newState == AnimalAIManager.AnimalState.Dead)
        {
            Instantiate(lootPrefab, transform.position, Quaternion.identity);
        }
    }
}
```

#### Example: Track Obstacle Hits
```csharp
using UnityEngine;

public class AnimalObstacleTracker : MonoBehaviour
{
    [SerializeField] private AnimalAIManager aiManager;
    private int obstacleHitCount = 0;
    
    void Start()
    {
        aiManager.onObstacleHit.AddListener(OnObstacleHit);
    }
    
    void OnObstacleHit()
    {
        obstacleHitCount++;
        Debug.Log($"Animal hit obstacle {obstacleHitCount} times");
    }
}
```

### Dealing Damage to Animals

```csharp
// From your weapon/projectile script:
void OnCollisionEnter(Collision collision)
{
    AnimalAIManager animal = collision.gameObject.GetComponent<AnimalAIManager>();
    if (animal != null)
    {
        animal.TakeDamage(25); // Deal 25 damage
    }
}
```

### Runtime Config Changes

```csharp
// Get reference to AI manager
AnimalAIManager ai = GetComponent<AnimalAIManager>();

// Access config (read-only recommended)
float currentSpeed = ai.config.runSpeed;

// To change behavior at runtime, create new config:
AnimalAIConfig runtimeConfig = ScriptableObject.CreateInstance<AnimalAIConfig>();
runtimeConfig.runSpeed = 10f; // Faster!
// Note: Changing config at runtime requires reassigning
```

### Multiple Animals with Same Config

**Efficient Setup:**
1. Create one config (e.g., "Deer_Config")
2. Apply to all deer prefabs
3. All instances share the same config (memory efficient)

**Per-Instance Tuning:**
If you need one deer to be faster:
1. Duplicate the config: Right-click → Duplicate
2. Rename: "Deer_Config_Fast"
3. Adjust speed values
4. Assign to specific deer instance

---

## Troubleshooting

### Animal Not Moving

**Possible Causes:**

1. **No NavMesh**
   - Solution: Bake NavMesh (`Window → AI → Navigation → Bake`)

2. **Agent Not on NavMesh**
   - Check Console for: "Failed to place agent on NavMesh!"
   - Solution: Move animal to valid NavMesh area

3. **No Config Assigned**
   - Check: `Config` slot is not empty
   - Solution: Drag a config asset into slot

4. **NavMeshAgent Disabled**
   - Check: `NavMeshAgent` component is enabled
   - Solution: Enable component

### Animal Not Fleeing

**Possible Causes:**

1. **Player Not Found**
   - Check: Player has `Player` tag
   - Solution: `GameObject → Tag → Player`

2. **Detection Radius Too Small**
   - Check: `Detection Radius` value in config
   - Solution: Increase to 10-15

3. **Player Controller Missing**
   - Check: Player has `CharController_Motor` component
   - Solution: Add component or system falls back to distance-only

4. **Animal Already Fleeing**
   - Check: Debug cube above animal is red
   - Solution: Wait for animal to return to idle

### Animal Stuck in Place

**Possible Causes:**

1. **NavMesh Gap**
   - Check: Blue NavMesh overlay is continuous
   - Solution: Re-bake with smaller agent radius

2. **Stuck State Triggered**
   - Check: Debug cube is yellow
   - Solution: Lower `Stuck Time Threshold` or improve NavMesh

3. **No Valid Path**
   - Check Console for path errors
   - Solution: Ensure destination is on NavMesh

### Animal Spinning/Jittering

**Possible Causes:**

1. **Rotation Speed Too High**
   - Check: `Rotation Speed` in config
   - Solution: Lower to 3-5

2. **Conflicting Movement Systems**
   - Check: No `CreatureMover` component present
   - Solution: Remove `CreatureMover` (AI auto-disables if found)

3. **NavMesh Agent Settings**
   - Check: `Angular Speed` should be 0 (AI handles rotation)
   - Solution: Set in code or inspector

### Performance Issues

**Symptoms:**
- Low FPS
- Stuttering
- Lag spikes

**Solutions:**

1. **Reduce Check Frequency**
   ```
   Avoidance Check Interval: 0.5 (from 0.2)
   Flee Update Interval: 2.0 (from 1.0)
   ```

2. **Disable Debug**
   ```
   Show Debug Gizmos: ✗
   Log State Changes: ✗
   ```

3. **Limit Animal Count**
   - Recommended: < 20 animals active at once
   - Use object pooling for spawning

4. **Simplify NavMesh**
   - Increase agent radius
   - Reduce NavMesh accuracy

### Collider Issues

**Animal Falls Through Ground:**
- Solution: Enable `Use Kinematic Rigidbody: true`

**Animal Pushes Objects:**
- Solution: Enable `Use Trigger Collider: true`

**Need Physical Collisions:**
```
Use Trigger Collider: false
Use Kinematic Rigidbody: false
```
Then adjust Rigidbody mass/drag in inspector.

---

## Quick Reference Card

### Essential Settings

```yaml
# Timid Animal (Rabbit)
Detection Radius: 8
Run Speed: 6
Flee Angle Variation: 40
Wander Interval: 3

# Normal Animal (Deer)
Detection Radius: 12
Run Speed: 7
Flee Angle Variation: 25
Wander Interval: 6

# Confident Animal (Bear)
Detection Radius: 6
Run Speed: 4
Flee Angle Variation: 15
Wander Interval: 8
```

### Common Tasks

| Task | Steps |
|------|-------|
| **Add AI to animal** | 1. Select animal<br>2. Add Component → AnimalAIManager<br>3. Assign config |
| **Create new config** | Right-click → Create → AI → Animal Configuration |
| **Debug animal** | Enable "Show Debug Gizmos" and "Log State Changes" |
| **Make faster** | Increase `Run Speed` in config |
| **Make more alert** | Increase `Detection Radius` in config |
| **Reduce lag** | Increase `Avoidance Check Interval` |

### Keyboard Shortcuts (Unity)

- **F** - Frame selected animal in Scene view
- **Ctrl/Cmd + D** - Duplicate config
- **Ctrl/Cmd + S** - Save changes
- **Ctrl/Cmd + P** - Play/Stop

---

## Support & Resources

### File Locations

```
Scripts:
└── Haven/Assets/Animal Assets/Animals_FREE/Animal Scripts/
    ├── AnimalAIManager.cs       (Main AI logic)
    ├── AnimalAIConfig.cs         (Configuration data)
    └── Editor/
        └── CreateAnimalConfigs.cs (Config generator)

Configs (after generation):
└── Haven/Assets/Animal Assets/Animals_FREE/Configs/
    ├── Deer_Config.asset
    ├── Rabbit_Config.asset
    └── Bear_Config.asset
```

### Getting Help

1. **Enable Logging**: `Log State Changes: ✓`
2. **Check Console**: Look for errors/warnings
3. **Enable Gizmos**: Visualize what AI sees
4. **Test in Isolation**: One animal in empty scene

### Best Practices

✅ **DO:**
- Use configs for different animal types
- Test with debug gizmos enabled
- Bake NavMesh before testing
- Use trigger colliders for smooth movement
- Profile performance with many animals

❌ **DON'T:**
- Mix `CreatureMover` and `AnimalAIManager` on same object
- Forget to bake NavMesh
- Set detection radius too small
- Leave debug logging on in builds
- Place animals off NavMesh

---

**Last Updated:** 2025-10-26  
**Version:** 2.0 (Refactored)

