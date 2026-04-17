# AR Room Placement Project

## Detailed Implementation Documentation

## 1. Project Objective

The objective of this project is to develop an Augmented Reality (AR) application that:

- scans the real-world environment using a mobile camera
- detects horizontal surfaces (ground)
- allows the user to place 3D objects by tapping on detected surfaces

## 2. Project Setup

### 2.1 Unity Project Creation

- Created a 3D Unity project
- Opened the project in VS Code
- Organized the project using structured folders

### 2.2 Folder Structure

```text
Assets/
└── _Project/
    ├── Materials/
    ├── Models/
    ├── Prefabs/
    ├── Scenes/
    ├── Scripts/
    │   ├── AR/
    │   ├── Interaction/
    │   ├── Managers/
    │   └── UI/
    ├── Textures/
    └── XR/
```

### 2.3 Folder Structure Explanation

The project uses a clean, feature-based Unity folder structure. The goal of this structure is not only to store files neatly, but also to make the project easier to understand, maintain, debug, and expand in the future. Since AR projects often grow from a small prototype into a larger interactive application, separating files by their purpose helps avoid confusion and reduces the chance of mixing unrelated systems together.

This organization also supports teamwork. If another developer opens the project, they can quickly identify where the scenes are stored, where the AR logic is written, where reusable prefabs are kept, and where visual assets such as textures and materials belong.

#### `Assets/`

- This is the main Unity content folder and the most important directory in the project.
- Unity automatically imports and tracks files placed inside `Assets/`, which means scenes, scripts, prefabs, textures, materials, and models all need to exist here to be used inside the Editor.
- In simple terms, this folder acts as the root of all game and AR content created for the application.

#### `Assets/_Project/`

- This folder is the custom workspace for all original project files.
- It separates our own work from third-party assets, Unity-generated content, package files, and example resources.
- Keeping project-specific assets inside `_Project` makes the structure cleaner and more professional because all important work can be found in one place.
- This also makes it easier to back up, migrate, or reuse the project structure in future AR applications.

#### `Assets/_Project/Materials/`

- This folder stores material assets used by objects in the AR scene.
- Materials control how surfaces look, including color, transparency, shininess, and texture appearance.
- In this project, materials are useful for detected planes, placed cubes, and future room objects.
- Keeping materials in a separate folder is important because the same material can be reused by many prefabs and scene objects without duplication.

#### `Assets/_Project/Models/`

- This folder stores raw imported 3D assets.
- Examples include cubes, room meshes, furniture models, decorative objects, or any external `.fbx` or `.obj` files added to the project.
- It is useful to keep original models separate from prefabs because a model is the base visual asset, while a prefab is usually a configured and reusable version of that asset.
- This separation becomes especially helpful when the project evolves from placing a basic cube to placing full room elements or detailed furniture.

#### `Assets/_Project/Prefabs/`

- This folder stores prefabs, which are reusable GameObject templates in Unity.
- A prefab may contain a model, materials, colliders, scripts, transforms, and custom settings already configured and ready for reuse.
- In this project, examples include the AR plane visual prefab and any object that will be placed into the real-world environment.
- Prefabs are important because they improve consistency. If the same object is used many times, it can be updated once in the prefab and the changes will apply everywhere it is used.

#### `Assets/_Project/Scenes/`

- This folder stores all Unity scenes used in the application.
- A scene contains the full arrangement of objects, lights, cameras, managers, and AR components needed for a specific part of the project.
- For this project, the main AR placement experience belongs here, and future demo scenes, test scenes, or experimental scenes can also be added in the same location.
- Storing scenes together makes build setup, navigation, and testing more organized.

#### `Assets/_Project/Scripts/`

- This folder contains all custom C# scripts written for the project.
- Scripts control behavior such as plane detection, object placement, interaction, UI updates, and scene management.
- Instead of placing every script in one location, the project divides scripts by responsibility.
- This improves readability and makes the codebase easier to scale as new features are added.

#### `Assets/_Project/Scripts/AR/`

- This subfolder contains AR-specific logic.
- Typical responsibilities include plane detection, AR raycasting, surface filtering, anchor placement, and the overall AR interaction flow.
- Scripts in this folder usually depend directly on AR Foundation components such as `ARPlaneManager`, `ARRaycastManager`, or `ARSessionOrigin` / `XROrigin`.
- The object placement script belongs here because it works directly with real-world plane detection and touch-based placement.

#### `Assets/_Project/Scripts/Interaction/`

- This subfolder is intended for logic that happens after an object has been placed.
- Examples include selecting an object, moving it, rotating it, scaling it, opening detail panels, or triggering animation.
- Separating interaction logic from AR scanning logic keeps responsibilities clear.
- In other words, AR scripts handle understanding the real world, while interaction scripts handle what the user can do with virtual objects afterward.

#### `Assets/_Project/Scripts/Managers/`

- This subfolder is intended for higher-level control scripts.
- Manager scripts usually coordinate multiple systems instead of handling one isolated action.
- Examples include app state management, scene flow, placement mode switching, reset systems, or communication between UI and AR features.
- Using managers helps reduce tight coupling between unrelated scripts and supports a more maintainable architecture.

#### `Assets/_Project/Scripts/UI/`

- This subfolder stores scripts related to the user interface.
- These scripts may control buttons, instruction text, status labels, menus, warnings, onboarding prompts, and tool panels.
- For example, if the project later adds a reset button, a placement guidance label, or a split-view mode toggle, the related script would belong here.
- Keeping UI code separate from AR placement code makes the project easier to maintain and prevents unrelated systems from becoming mixed together.

#### `Assets/_Project/Textures/`

- This folder stores image-based assets used throughout the project.
- Textures may be applied to models, materials, UI elements, icons, or scanned-surface visual effects.
- Examples include plane visualization textures, interface icons, and image maps used by materials.
- Keeping textures in their own folder helps track visual assets more clearly and avoids mixing them with prefabs or scripts.

#### `Assets/_Project/XR/`

- This folder stores XR-related assets, settings, and supporting configuration files.
- It may include XR setup assets, loader settings, simulation resources, and files connected to AR or XR initialization.
- Since XR projects often depend on multiple configuration assets, keeping them grouped together makes troubleshooting and maintenance easier.
- This is especially useful when checking whether AR features are correctly configured for the target device and platform.

### 2.4 Additional Organization Notes

- Use descriptive names for prefabs, scripts, and materials so their purpose is clear.
- Keep reusable assets in `Prefabs/` and raw imported assets in `Models/`.
- Avoid mixing test assets and final assets in the same folder when the project grows.
- Keep AR logic, interaction logic, and UI logic separated to reduce script complexity.
- As the cube evolves into a full room, the same structure can still be used without major reorganization.

## 3. AR Foundation Setup

### 3.1 Installed Packages

- AR Foundation
- ARCore XR Plugin

### 3.2 XR Configuration

Path:

`Project Settings -> XR Plug-in Management`

Enabled:

- ARCore
- Initialize XR on Startup

## 4. Scene Setup

### 4.1 Scene Created

`Assets/_Project/Scenes/ARPlacementScene`

### 4.2 Scene Hierarchy

```text
ARPlacementScene
├── Directional Light
├── Global Volume
├── AR Session
└── XR Origin
    └── Camera Offset
        └── Main Camera
```

## 5. XR Origin Configuration

Selected object:

- XR Origin

Added components:

- AR Plane Manager
- AR Raycast Manager
- Placement Script

### 5.1 AR Plane Manager Settings

- Detection Mode -> Horizontal
- Plane Prefab -> `ARPlaneVisual`

## 6. Plane Detection Setup

### 6.1 AR Plane Prefab Created

Location:

`Assets/_Project/Prefabs/ARPlaneVisual`

### 6.2 Components Used

- AR Plane
- AR Plane Mesh Visualizer
- Mesh Filter (empty)
- Mesh Renderer (with material)

### 6.3 Important Fix

Removed the default Unity plane mesh.

Reason:

- `ARPlaneMeshVisualizer` generates a dynamic real-world mesh
- a static mesh causes incorrect detection

## 7. Material Setup

Created:

- `PlaneMaterial` - added to cube
- `PlaneScanningTextureMaterial` - added to plane

Properties:

- transparent surface
- low alpha value, around `0.1-0.2`

Purpose:

- visualize detected surfaces

## 8. Object Placement Setup

### 8.1 Cube Created

- Created a cube
- Scaled to `0.2, 0.2, 0.2`

Converted to prefab:

`Assets/_Project/Prefabs/TestObject`

## 9. Script Implementation

### 9.1 Script Location

`Assets/_Project/Scripts/AR/`

### 9.2 Script Used

`PlaceCubeOnPlaneTest.cs`

### 9.3 Functionality

The script performs the following:

- detects touch input
- performs AR raycast
- checks whether the ray hits a plane
- gets the hit position
- spawns the object at that location

### 9.4 Important Issue

Problem:

- touch input was not working

Cause:

- the project was using `Input System (New)`
- but the script used `Input.touchCount` from the old API

### 9.5 Fix

Path:

`Project Settings -> Player -> Other Settings`

Set:

- Active Input Handling -> `Both`

## 10. Android Build Setup

### 10.1 Platform Configuration

- Switched to Android Build Profile
- Selected the connected device

### 10.2 Build Issues Faced

Issue 1: App running on laptop

- Cause: platform was set to macOS
- Fix: set Android as the active build profile

Issue 2: ADB not working

- Cause: `adb` was not available globally
- Fix: used Unity internal `adb`

Issue 3: APK not installing

- Fix: verified the device using `adb devices`

## 11. Rendering Issues

### 11.1 Black Screen Issue

Cause:

- Vulkan Graphics API

Fix:

- remove Vulkan
- use OpenGLES3 only
