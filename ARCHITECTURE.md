# Haven-Rise Game Architecture

## Overview
Haven-Rise is a Unity-based first-person survival game built with C#. The game features environmental themes centered around deforestation, reforestation, and ecosystem management. Players must balance resource gathering with environmental consequences in a procedurally generated world.

## Core Architecture

### Game Engine & Framework
- **Unity Version**: 2023.x (based on package versions)
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Input System**: Unity Input System (1.12.0)
- **UI Framework**: Unity UI (UGUI)
- **AI Navigation**: Unity AI Navigation (2.0.7)

### Architecture Patterns
- **Singleton Pattern**: Central managers (UIManager, InventorySystem)
- **Component-Based Architecture**: Modular MonoBehaviour components
- **Event-Driven Systems**: Unity Events for decoupled communication
- **Layer-Based Organization**: Separate scripts by functionality (UI, Function, Animation, etc.)

## System Architecture

### 1. Player Systems

#### Character Controller (`CharController_Motor.cs`)
- **Purpose**: First-person character movement and interaction
- **Features**:
  - FPS-style movement with mouse look
  - Sprint mechanics with stamina integration
  - Crouching system with height adjustment
  - Head bobbing animation
  - Water buoyancy mechanics
  - Input management with pause integration

#### Input Management
- **System**: Unity Input System integration
- **Actions**: Movement, interaction, UI navigation
- **Contextual**: Different input handling for gameplay vs menus

### 2. World Systems

#### Procedural Generation (`WorldSeedManager.cs`)
- **Purpose**: Seeded world generation for consistent gameplay
- **Features**:
  - Deterministic random number generation
  - Position calculation for object placement
  - Seed-based randomization for replayability

#### Day/Night Cycle (`DayNightCycle.cs`)
- **Purpose**: Dynamic time progression and environmental lighting
- **Features**:
  - Configurable day duration (default: 10 minutes real-time)
  - Dynamic sun rotation and lighting
  - Gradient-based color transitions for ambient lighting
  - Fog color management

### 3. Environmental Systems

#### Temperature System (Integrated in `UIManager.cs`)
- **Purpose**: Environmental consequence management
- **Mechanics**:
  - Temperature increases with deforestation
  - Temperature decreases with reforestation
  - Health damage in critical temperature zones
  - Visual effects based on temperature thresholds
  - Stamina regeneration affected by temperature

#### Tree Management System (`TreePlantingSystem.cs`)
- **Purpose**: Resource and ecosystem management
- **Features**:
  - Tree cutting spawns soil patches
  - Seed planting on soil patches
  - Ground detection for proper placement
  - Integration with temperature system

### 4. Inventory & Items

#### Inventory System (`InventorySystem.cs`, `InventoryManager.cs`)
- **Purpose**: Item management and UI interaction
- **Architecture**:
  - Singleton pattern for global access
  - Drag-and-drop functionality between slots
  - Hotbar and inventory grid separation
  - Item activation/deactivation management

#### Item Interaction (`ItemPickup.cs`, `ObjectInteractionController.cs`)
- **Purpose**: Player interaction with world objects
- **Features**:
  - Raycast-based interaction system
  - Damage application to destroyable objects
  - Item pickup mechanics
  - Visual feedback systems

### 5. AI Systems

#### Animal AI (`AnimalAIManager.cs`)
- **Purpose**: Wildlife behavior simulation
- **Features**:
  - NavMesh-based pathfinding
  - Flee behavior based on player actions
  - State machine (Idle, Fleeing, Dead)
  - Animation blending based on movement speed
  - Player detection with different radii for different movement states

### 6. UI Management

#### Central UI Manager (`UIManager.cs`)
- **Purpose**: Game state and UI coordination
- **Systems Managed**:
  - Health system with visual indicators
  - Stamina system with regeneration mechanics
  - Temperature system with environmental effects
  - Pause menu functionality
  - Player control enabling/disabling

#### Inventory UI (`InventoryUIManager.cs`, `InventoryManager.cs`)
- **Purpose**: Inventory interface management
- **Features**:
  - Grid-based inventory display
  - Hotbar management
  - Drag-and-drop interactions
  - Item tooltips and descriptions

### 7. Game State Management

#### Save/Load System (`ApplySavedPlayerState.cs`, `PauseMenuManager.cs`)
- **Purpose**: Game persistence
- **Features**:
  - PlayerPrefs-based save system
  - Scene transition management
  - Player position and rotation persistence
  - Game state restoration

#### Scene Management (`CutsceneController.cs`, `MainMenu.cs`)
- **Purpose**: Game flow control
- **Features**:
  - Cutscene to gameplay transitions
  - Main menu navigation
  - Scene loading coordination
  - Save state validation

## Asset Organization

### Directory Structure
```
Haven/Assets/
├── Animal Assets/          # Wildlife assets and behaviors
├── Flooded_Grounds/        # Main game content
│   ├── Scripts/
│   │   ├── Animation Scripts/     # Animation controllers
│   │   ├── cutscene script/       # Cinematic sequences
│   │   ├── FPSController/         # Player movement
│   │   ├── Function Scripts/      # Core gameplay systems
│   │   ├── UI Scripts/           # UI management
│   ├── Prefabs/                  # Reusable game objects
│   ├── Scenes/                   # Game levels
│   └── Content/                  # 3D models and materials
├── Sound Effects/         # Audio assets
├── UI Elements/          # Interface graphics
└── PostProcessing/       # Visual effects
```

## Technical Specifications

### Performance Considerations
- **Frame Rate**: FPS display system for monitoring
- **Memory Management**: Object pooling for frequently spawned items
- **Asset Loading**: Asynchronous scene loading

### Platform Target
- **Primary Platform**: Windows/Editor (based on project settings)
- **Resolution**: 1920x1080 default
- **Input**: Keyboard + Mouse

### Dependencies
- **Unity Timeline**: For cutscene management
- **Unity PostProcessing**: Visual effects stack
- **Unity Animation Rigging**: Advanced character animations
- **Input System**: Enhanced input handling

## Game Flow Architecture

### Application Lifecycle
1. **Main Menu** → Scene selection and save management
2. **Cutscene** → Introduction sequence (10s duration)
3. **Gameplay** → Main game loop with all systems active
4. **Pause** → Game state preservation and menu access
5. **Save/Load** → Persistent state management

### Core Game Loop
- **Input Processing** → Player actions and interactions
- **Physics Update** → Movement and collision detection
- **AI Updates** → Animal behaviors and pathfinding
- **Environmental Updates** → Temperature, day/night cycle
- **UI Updates** → Health, stamina, inventory states
- **Rendering** → Visual feedback and effects

## Integration Points

### System Interactions
- **Temperature ↔ Health**: Critical temperature causes damage
- **Temperature ↔ Stamina**: High temperature reduces regeneration
- **Inventory ↔ World**: Item pickup and placement
- **Player ↔ AI**: Movement state affects animal behavior
- **Environment ↔ Visuals**: Temperature drives post-processing effects

This architecture provides a solid foundation for the environmental survival gameplay while maintaining modularity and extensibility for future features.
