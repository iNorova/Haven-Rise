# Animal Assets and AI Systems

## Overview

The Haven-Rise game features two sophisticated animal AI systems that create realistic wildlife behavior. The animal systems are designed to respond intelligently to player actions while maintaining performance and providing engaging gameplay interactions.

## Asset Structure

### Directory Organization
```
Haven/Assets/Animal Assets/
├── Animals_FREE/
│   ├── Animal Scripts/           # Core AI implementations
│   │   ├── AnimalAIManager.cs   # NavMesh-based AI system
│   │   ├── CreatureMover.cs     # CharacterController-based AI system
│   │   ├── MovePlayerInput.cs   # Input handling for creatures
│   │   ├── PlayerCamera.cs      # Camera system for creatures
│   │   └── ThirdPersonCamera.cs # Alternative camera implementation
│   ├── Animations/              # Animal animation controllers and clips
│   ├── Materials/               # Animal materials and textures
│   ├── Meshes/                  # 3D animal models
│   ├── Prefabs/                 # Instantiable animal prefabs
│   └── Scenes/                  # Test scenes for animal behavior
└── Animals_FREE.meta
```

## AI System Architecture

### Dual AI System Design

The game implements **two complementary AI systems** for maximum flexibility:

1. **NavMesh-Based AI (AnimalAIManager.cs)** - Primary system for deer and wildlife
2. **CharacterController-Based AI (CreatureMover.cs)** - Advanced system with physics integration

## NavMesh-Based AI System (AnimalAIManager.cs)

### Core Features

#### State Management
```csharp
private enum AnimalState { Idle, Fleeing, Dead }
private AnimalState currentState = AnimalState.Idle;
```

**State Transitions:**
- **Idle**: Default wandering state with minimal movement
- **Fleeing**: Active escape behavior triggered by player proximity
- **Dead**: Terminal state with cleanup sequence

#### Detection System

**Dual-Radius Detection:**
- **Close Detection Radius (2m)**: Triggers when player is crouching nearby
- **Normal Detection Radius (10m)**: Triggers when player is walking/sprinting

**Player State Integration:**
```csharp
CharController_Motor playerController = playerTransform.GetComponent<CharController_Motor>();

if (playerController.IsCrouching())
{
    if (distanceToPlayer <= closeDetectionRadius)
    {
        StartFleeing();
    }
}
else if (playerController.IsWalking() || playerController.IsSprinting())
{
    if (distanceToPlayer <= detectionRadius)
    {
        StartFleeing();
    }
}
```

#### Advanced Pathfinding

**NavMesh Configuration:**
```csharp
navAgent.acceleration = 8f;
navAgent.angularSpeed = 0f;        // Custom rotation handling
navAgent.stoppingDistance = 0.5f;
navAgent.radius = 0.5f;
navAgent.height = 1f;
navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
navAgent.avoidancePriority = 50;
navAgent.updateRotation = false;   // Manual rotation for smoother movement
```

**Smart Flee Position Calculation:**
```csharp
// Validates flee positions through multiple criteria
if (newDistToPlayer > currentDistToPlayer &&     // Must be further from player
    Vector3.Distance(hit.position, lastFleePosition) > minFleeDistance)  // Not too close to previous position
{
    navAgent.SetDestination(hit.position);
    foundValidPosition = true;
}
```

#### Animation Integration

**Smooth Animation Blending:**
```csharp
// Animation parameters using hashed IDs for performance
private static readonly int State = Animator.StringToHash("State");
private static readonly int Vert = Animator.StringToHash("Vert");
private static readonly int Speed = Animator.StringToHash("Speed");

// Smooth transitions between movement states
float targetSpeed = currentState == AnimalState.Fleeing ? 1f : 0.5f;
currentAnimationSpeed = Mathf.Lerp(currentAnimationSpeed, targetSpeed, Time.deltaTime * animationBlendSpeed);
```

### Health and Combat System

**Damage Integration:**
```csharp
public void TakeDamage(int damage)
{
    currentHP -= damage;
    if (currentHP <= 0)
    {
        Die();
    }
}

private void Die()
{
    currentState = AnimalState.Dead;

    // Cleanup sequence
    navAgent.isStopped = true;
    navAgent.enabled = false;
    rb.isKinematic = true;
    capsuleCollider.enabled = false;

    StartCoroutine(DeathSequence());
}
```

## CharacterController-Based AI System (CreatureMover.cs)

### Advanced Movement System

#### Movement Handler Architecture
```csharp
private class MovementHandler
{
    private readonly CharacterController m_Controller;
    private readonly Transform m_Transform;
    private float m_WalkSpeed, m_RunSpeed, m_RotateSpeed;
    private Space m_Space;
    private Vector3 m_GravityAcelleration = Physics.gravity;
}
```

#### Sophisticated Obstacle Avoidance

**Multi-Raycast Path Planning:**
```csharp
private Vector3 GetSafeFleeDirection(Vector3 preferredDirection)
{
    // Creates fan of raycasts for obstacle detection
    float angleStep = m_RaycastSpread / (m_RaycastCount - 1);
    float startAngle = -m_RaycastSpread / 2f;

    for (int i = 0; i < m_RaycastCount; i++)
    {
        float currentAngle = startAngle + (angleStep * i);
        Vector3 direction = Quaternion.Euler(0, currentAngle, 0) * preferredDirection;
        float score = EvaluateDirection(direction);
        // Selects best direction based on multiple criteria
    }
}
```

**Direction Evaluation Criteria:**
- **Obstacle Clearance**: Penalizes directions with nearby obstacles
- **Slope Analysis**: Avoids steep terrain beyond climbable angles
- **Ground Availability**: Ensures walkable terrain ahead
- **Height Variation**: Checks for obstacles at multiple heights

#### Ground Detection System

**Multi-Point Ground Checking:**
```csharp
private bool IsGrounded()
{
    Vector3[] checkPoints = new Vector3[]
    {
        m_Transform.position,
        m_Transform.position + m_Transform.forward * 0.2f,
        m_Transform.position - m_Transform.forward * 0.2f,
        m_Transform.position + m_Transform.right * 0.2f,
        m_Transform.position - m_Transform.right * 0.2f
    };

    foreach (Vector3 point in checkPoints)
    {
        if (Physics.Raycast(point + Vector3.up * m_GroundCheckHeight,
                           Vector3.down, m_GroundCheckDistance, m_GroundLayerMask))
        {
            return true;
        }
    }
    return false;
}
```

### Wandering Behavior

**Random Target Generation:**
```csharp
private void SetNewWanderTarget()
{
    Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * 5f;
    m_WanderTarget = m_Transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
    m_NextWanderTime = Time.time + UnityEngine.Random.Range(3f, 7f);
}
```

## Spawning Systems

### Simple Terrain Spawning (DeerSpawner.cs)

**Grass-Based Placement:**
```csharp
if (Physics.Raycast(randomPos, Vector3.down, out RaycastHit hit, 200f))
{
    if (hit.collider.CompareTag("Grass"))
    {
        Instantiate(deerPrefab, hit.point, Quaternion.identity);
    }
}
```

**Features:**
- Raycast-based ground detection
- Tag-based surface validation
- Retry logic with maximum attempts
- Random positioning within spawn radius

### Advanced Grid-Based Spawning (Universal Object Spawner.cs)

**Sophisticated Placement Algorithm:**
```csharp
void SpawnObjects()
{
    // Grid-based distribution with randomization
    InitializeGrid();
    foreach (Vector2 gridPos in gridPositions)
    {
        // Validates spawn position against multiple criteria
        if (IsValidPosition(spawnPosition) && !IsNearStructure(spawnPosition) && !IsInWater(spawnPosition))
        {
            // Spawns with random rotation and parent assignment
        }
    }
}
```

**Validation Layers:**
- **Proximity Check**: Minimum distance between objects
- **Terrain Bounds**: Ensures position is within terrain limits
- **Structure Avoidance**: Layer-based collision detection
- **Water Detection**: Multiple water validation methods

## Integration Points

### Player System Integration

**Direct Player Controller Queries:**
Both AI systems directly integrate with the player's `CharController_Motor`:

```csharp
CharController_Motor playerController = playerTransform.GetComponent<CharController_Motor>();

// Responds to different player states
bool isPlayerCrouching = playerController.IsCrouching();
bool isPlayerMoving = playerController.IsWalking() || playerController.IsSprinting();
bool isPlayerSprinting = playerController.IsSprinting();
```

### Animation System Integration

**Smooth State Transitions:**
```csharp
// Uses Unity's Animator with hashed parameters for performance
m_Animator.SetFloat(State, currentAnimationSpeed);
m_Animator.SetFloat(Vert, currentAnimationSpeed);
m_Animator.SetFloat(Speed, currentSpeed * animationSpeedMultiplier);

// Smooth blending between states
currentAnimationSpeed = Mathf.Lerp(currentAnimationSpeed, targetSpeed, Time.deltaTime * animationBlendSpeed);
```

### Navigation System Integration

**NavMesh Agent Configuration:**
```csharp
// Optimized for animal movement patterns
navAgent.updateRotation = false;  // Custom rotation for natural movement
navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
navAgent.avoidancePriority = 50;  // Lower priority than player
```

## Configuration Options

### AnimalAIManager Settings

```csharp
[Header("Detection Settings")]
[SerializeField] private float detectionRadius = 10f;      // Normal detection range
[SerializeField] private float closeDetectionRadius = 2f;  // Crouching detection range
[SerializeField] private float runSpeed = 5f;              // Fleeing speed
[SerializeField] private float idleSpeed = 1f;             // Wandering speed
[SerializeField] private float fleeDistance = 15f;         // How far to flee
[SerializeField] private float maxFleeDistance = 100f;     // Maximum flee search distance

[Header("Animation Settings")]
[SerializeField] private float animationBlendSpeed = 5f;   // Animation transition speed
[SerializeField] private float animationSpeedMultiplier = 1f; // Animation speed scaling
```

### CreatureMover Settings

```csharp
[Header("Player Detection")]
[SerializeField] private float m_DetectionRadius = 10f;        // Detection range
[SerializeField] private float m_CloseDetectionRadius = 2f;    // Close detection range
[SerializeField] private float m_FleeDistance = 20f;           // Flee distance
[SerializeField] private float m_FleeTurnSpeed = 180f;         // Turn speed when fleeing

[Header("Ground Detection")]
[SerializeField] private LayerMask m_GroundLayerMask;         // Ground layer
[SerializeField] private float m_GroundCheckDistance = 0.5f;  // Ground check distance
[SerializeField] private float m_MaxSlopeAngle = 45f;         // Maximum climbable slope
```

## Performance Considerations

### Optimization Techniques

**Efficient Detection:**
```csharp
// Only processes when necessary
if (currentState == AnimalState.Dead) return;

// Calculates actual movement speed for animation accuracy
currentSpeed = Vector3.Distance(transform.position, lastPosition) / Time.deltaTime;
```

**Memory Management:**
- Object pooling not implemented (potential improvement area)
- Components disabled during death sequence
- Proper cleanup in death coroutine

**Physics Optimization:**
- CharacterController-based system for physics interactions
- Multi-point ground detection for stability
- Raycast pooling for obstacle avoidance

## Visual Debugging

### Editor Gizmos

**AnimalAIManager Visualization:**
```csharp
private void OnDrawGizmosSelected()
{
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, detectionRadius);

    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, closeDetectionRadius);
}
```

**Universal Spawner Visualization:**
```csharp
void OnDrawGizmosSelected()
{
    // Spawn area visualization
    Gizmos.color = Color.green;
    Gizmos.DrawWireCube(transform.position, new Vector3(spawnAreaSize.x, 0.1f, spawnAreaSize.y));

    // Water height and structure check visualization
    Gizmos.color = Color.blue;
    Gizmos.DrawWireCube(transform.position + Vector3.up * waterHeight,
        new Vector3(spawnAreaSize.x, 0.1f, spawnAreaSize.y));
}
```

## Usage Examples

### Basic Animal Setup

1. **Add NavMesh-based AI:**
   ```csharp
   // Attach AnimalAIManager component to animal prefab
   // Set detection radii and speeds in inspector
   // Assign player transform reference
   // Configure NavMesh agent settings
   ```

2. **Add CharacterController-based AI:**
   ```csharp
   // Attach CreatureMover component
   // Configure movement speeds and detection settings
   // Set up ground and obstacle layers
   // Add MovePlayerInput for external control
   ```

3. **Spawning Configuration:**
   ```csharp
   // Use DeerSpawner for simple grass-based spawning
   // Use UniversalObjectSpawner for complex environment-based placement
   // Configure layer masks and terrain references
   ```

## Future Enhancement Opportunities

### Potential Improvements

1. **Unified AI System**: Merge the two AI systems into a single, configurable framework
2. **Behavior Trees**: Implement behavior tree system for more complex AI patterns
3. **Group Behavior**: Add flocking, herding, or pack behaviors
4. **Day/Night Cycles**: Different behavior patterns based on time of day
5. **Seasonal Changes**: Behavioral variations based on environmental conditions
6. **Memory System**: Animals remember player interactions and adjust behavior accordingly

### Performance Optimizations

1. **Object Pooling**: Implement pooling for frequent spawning/destruction
2. **LOD System**: Level-of-detail system for distant animals
3. **Culling**: Frustum and occlusion culling for off-screen animals
4. **Async Pathfinding**: Background pathfinding calculations
5. **Spatial Partitioning**: Optimize detection queries with spatial data structures

## Integration with Game Systems

### Environmental Integration

**Temperature System:**
- Animals could have preferred temperature ranges
- Heat stress affects movement speed and behavior
- Migration patterns based on environmental conditions

**Tree System:**
- Herbivores graze near trees and plants
- Deforestation affects animal population distribution
- Reforestation creates new habitats

**Water System:**
- Animals require water sources
- Aquatic animals respond to water levels
- Flooding affects terrestrial animal behavior

### Player Interaction

**Hunting System:**
- Damage system already implemented
- Could add resource drops (meat, hides, etc.)
- Trophy/scoring system

**Taming System:**
- Pet/follow behavior
- Mount system for certain animals
- Animal companions with unique abilities

This comprehensive animal AI system provides a solid foundation for realistic wildlife simulation while maintaining performance and extensibility for future enhancements.
