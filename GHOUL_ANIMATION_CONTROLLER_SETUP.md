# Ghoul Zombie Animation Controller Setup Guide

This guide will help you set up the Animator Controller for the Ghoul Zombie to work with the GhoulZombieAI script.

## Step 1: Create/Open the Animator Controller

1. In Unity, go to **Window → Animation → Animator** (or press Ctrl+6)
2. If you don't have a controller yet:
   - Right-click in your Project window (in `Assets/_GhoulZombie/` folder)
   - Select **Create → Animator Controller**
   - Name it `GhoulController`
3. Select your Ghoul prefab in the scene or Project window
4. In the Animator component, assign your `GhoulController` (or create one)

## Step 2: Add Animation Clips

1. Import your ghoul animation clips (Idle, Walk, Run, Attack, Death)
2. Make sure they're imported correctly:
   - **Idle, Walk, Run**: Should be set to **Loop** in Import Settings
   - **Attack, Death**: Should **NOT** be looped

## Step 3: Create Animator Parameters

In the Animator window, click the **Parameters** tab (top left), then click the **+** button to add:

### Parameters to Add:

1. **Speed** (Float)
   - Type: Float
   - Default Value: 0

2. **IsWalking** (Bool)
   - Type: Bool
   - Default Value: false

3. **IsRunning** (Bool)
   - Type: Bool
   - Default Value: false

4. **Attack** (Trigger)
   - Type: Trigger

5. **Die** (Trigger)
   - Type: Trigger

## Step 4: Create Animation States

1. In the Animator window, right-click in the empty space
2. Select **Create State → Empty**
3. Create these states:

### States to Create:

- **Idle** (set as default - orange background)
- **Walk**
- **Run**
- **Attack**
- **Death**

### Setting Up Each State:

1. Click on each state
2. In the Inspector, assign the corresponding animation clip:
   - **Idle** state → Idle animation clip
   - **Walk** state → Walk animation clip
   - **Run** state → Run animation clip
   - **Attack** state → Attack animation clip
   - **Death** state → Death animation clip

3. For **Idle, Walk, Run**: Check **Loop** in the Motion settings
4. For **Attack, Death**: Uncheck **Loop**

5. Set **Idle** as the default state:
   - Right-click on **Idle** state
   - Select **Set as Layer Default State** (it will turn orange)

## Step 5: Create Transitions

### Locomotion Transitions (Idle ↔ Walk ↔ Run):

#### Idle → Walk:
1. Right-click **Idle** state → **Make Transition** → Click **Walk** state
2. Select the transition arrow
3. In Inspector:
   - **Conditions**: Add condition `IsWalking == true`
   - **Has Exit Time**: **UNCHECKED**
   - **Transition Duration**: 0.1 (smooth blend)

#### Walk → Idle:
1. Right-click **Walk** state → **Make Transition** → Click **Idle** state
2. Select the transition arrow
3. In Inspector:
   - **Conditions**: Add condition `IsWalking == false`
   - **Has Exit Time**: **UNCHECKED**
   - **Transition Duration**: 0.1

#### Walk → Run:
1. Right-click **Walk** state → **Make Transition** → Click **Run** state
2. Select the transition arrow
3. In Inspector:
   - **Conditions**: Add condition `IsRunning == true`
   - **Has Exit Time**: **UNCHECKED**
   - **Transition Duration**: 0.2

#### Run → Walk:
1. Right-click **Run** state → **Make Transition** → Click **Walk** state
2. Select the transition arrow
3. In Inspector:
   - **Conditions**: Add condition `IsRunning == false`
   - **Has Exit Time**: **UNCHECKED**
   - **Transition Duration**: 0.2

#### Idle → Run (optional, for direct transitions):
1. Right-click **Idle** state → **Make Transition** → Click **Run** state
2. Select the transition arrow
3. In Inspector:
   - **Conditions**: Add condition `IsRunning == true`
   - **Has Exit Time**: **UNCHECKED**
   - **Transition Duration**: 0.2

#### Run → Idle:
1. Right-click **Run** state → **Make Transition** → Click **Idle** state
2. Select the transition arrow
3. In Inspector:
   - **Conditions**: Add condition `IsWalking == false` AND `IsRunning == false`
   - **Has Exit Time**: **UNCHECKED**
   - **Transition Duration**: 0.2

### Attack Transitions:

#### Any State → Attack:
1. Right-click **Any State** (the entry point) → **Make Transition** → Click **Attack** state
2. Select the transition arrow
3. In Inspector:
   - **Conditions**: Add condition `Attack` (trigger)
   - **Has Exit Time**: **UNCHECKED**
   - **Transition Duration**: 0.15 (smooth blend from any state)

#### Attack → Idle:
1. Right-click **Attack** state → **Make Transition** → Click **Idle** state
2. Select the transition arrow
3. In Inspector:
   - **Conditions**: (leave empty - no conditions)
   - **Has Exit Time**: **CHECKED** (wait for attack animation to finish)
   - **Exit Time**: 0.9 (exit near end of animation)
   - **Transition Duration**: 0.2

**Alternative Attack → Locomotion:**
You can also create transitions from Attack back to Walk/Run:
- **Attack → Walk**: Has Exit Time checked, condition `IsWalking == true`
- **Attack → Run**: Has Exit Time checked, condition `IsRunning == true`

### Death Transitions:

#### Any State → Death:
1. Right-click **Any State** → **Make Transition** → Click **Death** state
2. Select the transition arrow
3. In Inspector:
   - **Conditions**: Add condition `Die` (trigger)
   - **Has Exit Time**: **UNCHECKED**
   - **Transition Duration**: 0.1 (instant transition)

**Important**: Do NOT create any transitions OUT of the Death state. The ghoul should stay in death animation.

## Step 6: Configure Animation Events (Optional but Recommended)

For precise damage timing:

1. Select your **Attack** animation clip in the Project window
2. Open the **Animation** window (Window → Animation → Animation)
3. Scrub to the frame where the attack should hit (usually when hand/claw makes contact)
4. Click the **Add Event** button (small + icon at the top)
5. In the event, set the function name to: `AnimationEvent_DealDamage`
6. This ensures damage is dealt exactly when the attack connects

## Step 7: Connect to GhoulZombieAI Script

1. Select your Ghoul GameObject/prefab
2. In the Inspector, find the **GhoulZombieAI** component
3. Verify these parameter names match your Animator Controller:
   - **Speed Param**: `Speed`
   - **Walk Bool**: `IsWalking`
   - **Run Bool**: `IsRunning`
   - **Attack Trigger**: `Attack`
   - **Death Trigger**: `Die`

If you named your parameters differently, update these fields to match.

## Step 8: Test the Setup

1. Enter Play Mode
2. Watch the Animator window while the ghoul is active
3. You should see:
   - **Idle** state when ghoul is stationary
   - **Walk** state when patrolling during day
   - **Run** state when patrolling/chasing during night
   - **Attack** trigger fires when ghoul attacks
   - **Die** trigger fires when ghoul dies

## Troubleshooting

### Ghoul not animating:
- Check that Animator component is enabled
- Verify animation clips are assigned to states
- Check that parameter names match in GhoulZombieAI component

### Animations not transitioning smoothly:
- Increase Transition Duration values
- Make sure "Has Exit Time" is unchecked for instant transitions
- Check that conditions are set correctly

### Attack not triggering:
- Verify Attack trigger parameter exists
- Check that transition from Any State → Attack has Attack trigger condition
- Ensure GhoulZombieAI has correct attackTrigger name

### Death animation not playing:
- Verify Die trigger parameter exists
- Check that transition from Any State → Death has Die trigger condition
- Ensure GhoulZombieAI has correct deathTrigger name

## Visual Layout Suggestion

```
[Entry] → [Idle] ←→ [Walk] ←→ [Run]
            ↓         ↓         ↓
         [Attack] ← [Attack] ← [Attack]
            ↓
         [Death] (no exit)
```

This layout makes it easy to see the state flow and transitions.

