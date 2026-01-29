# Procedural UI Framework

A high-performance, data-driven UI architecture for Unity. This framework separates logic from visuals, allowing for CSS-like styling, hot-reloading, and complex state-machine animations using JSON. Built on top of **UniTask** and **PrimeTween**.

## 1. Architecture & Usage

The framework is built around a centralized Manager that handles layer sorting and z-order, while individual elements inherit from a robust base class to handle lifecycle events.

### The UI Manager

The `UIManager` automatically generates the canvas hierarchy at runtime, organized into four sorting layers:

1. **Background:** Static backdrops or 3D render textures.
2. **Screen:** The main interactive layer (HUD, Main Menu).
3. **Modal:** Stacked windows (Inventory, Settings) with automatic backdrop darkening.
4. **Widget:** Floating elements (Tooltips, Toasts) that sit above everything.

**Usage:**

```csharp
// Show a view (Async)
await UIManager.Instance.Show("InventoryView");

// Show a specific component instance
var view = await UIManager.Instance.Show<InventoryView>("InventoryView");

// Close the top-most modal
await UIManager.Instance.Back();

// Hide specific view
await UIManager.Instance.Hide(view);

```

### Inheritance Model

#### **UIBase**

The abstract core of all UI elements. It manages:

* **Dependencies:** Auto-fetches `CanvasGroup`, `RectTransform`, etc.
* **Lifecycle:** Provides `Show()` and `Hide()` methods powered by UniTask.
* **State:** Tracks Hover, Press, Select, and Check states.

#### **UIView** (For Windows/Screens)

Inherit from this for top-level screens. You must define the `ViewType` to tell the Manager where to place it.

```csharp
public class InventoryView : UIView
{
    // Define the layer (Screen, Modal, etc.)
    public override ViewType Type => ViewType.Modal;

    // Async Lifecycle Hooks
    protected override async UniTask OnShow()
    {
        // Bind data, play sounds, or wait for web requests here
        await InventorySystem.LoadData();
    }

    protected override async UniTask OnHide()
    {
        // Cleanup logic
        await UniTask.CompletedTask;
    }
}

```

#### **UIComponent** (For Widgets)

Inherit from this for interactable elements like Buttons, Cards, or Toggles. It automatically handles Pointer events (`Enter`, `Exit`, `Down`, `Up`) and Toggle value changes.

```csharp
public class MyButton : UIComponent
{
    protected override void ConfigureCanvas()
    {
        // Components usually inherit sorting from their parent View
        if (Canvas) Canvas.overrideSorting = false;
    }
}

```

---

## 2. Data-Driven Styling

Visuals are decoupled from Prefabs. You define the look and feel in `StreamingAssets/style.json`. The `StyleManager` watches this file and **hot-reloads changes instantly** in the editor and at runtime.

### UI Identity

To link a GameObject to a JSON style, add the `UIIdentity` component.

* **ID:** Unique identifier (e.g., `#SubmitButton`).
* **Classes:** Shared styles (e.g., `.btn`, `.primary`).

### JSON Structure

The style system supports variables, inheritance, and procedural rendering (Shadows, Borders, Rounded Corners) without needing textures.

```json
{
  "variables": {
    "primaryColor": "#FF5733",
    "radius": 12
  },
  "styles": {
    // Shared Class
    ".btn": {
      "preferredHeight": 60,
      "radius": "$radius",
      "backgroundColor": "#333333",
      "textColor": "#FFFFFF"
    },
    // Specific ID override
    "#SubmitButton": {
      "inherit": ".btn", // Inherits properties from .btn
      "backgroundColor": "$primaryColor",
      "shadow": { 
          "color": "#00000088", 
          "y": -5, 
          "softness": 10 
      }
    }
  }
}

```

### Procedural Rendering (`UIStyle`)

The framework generates two procedural images at runtime for every styled element:

1. **_ProceduralBG:** Handles Background Color, Border, and Radius.
2. **_ProceduralShadow:** Handles Drop Shadows (Distance, Softness, Color).

You do not need to assign Sprites. Just tweak the JSON.

---

## 3. The Animation System

Animations are defined purely in JSON. The system uses a state machine to blend between "Timelines" (Show/Hide/Loop) and "Interactions" (Hover/Press).

### Timeline Flows

The animation engine automatically determines the flow based on the properties defined in your JSON configuration.

1. **Standard Flow (Enter -> Normal):**
* Used when `repeat` is disabled.
* Element snaps to `Enter` state, then tweens to `Normal`.


2. **Looping Flow (Enter -> Initial -> Animate):**
* Used when `repeat` (cycles) is defined.
* Element snaps to `Enter`.
* Tweens to `Initial`.
* Loops between `Initial` and `Animate`.
* On Hide, transitions from current state to `Exit`.



### Interactions

Interactions (`hover`, `press`, `check`) interrupt the main timeline.

* **Priority:** Interaction tweens override the current loop.
* **Recovery:** When the interaction ends (e.g., Pointer Exit), the system smoothly tweens back to the `Normal` state (or resumes the loop).

### Animation JSON Configuration

Every state (`enter`, `exit`, `hover`, etc.) can have its own `style` target and specific `transition` timing.

```json
"#SubmitButton": {
  "animation": {
    // Global default transition
    "transition": { "duration": 0.2, "ease": "OutQuad" },

    // 1. Entrance Animation
    "enter": {
      "style": { "opacity": 0, "scale": { "x": 0, "y": 0 } },
      "transition": { "duration": 0.5, "ease": "OutBack" }
    },

    // 2. Idle Loop (Pulse)
    "repeat": { "cycles": -1, "cycleMode": "Yoyo" },
    "initial": { "style": { "scale": { "x": 1, "y": 1 } } },
    "animate": { 
      "style": { "scale": { "x": 1.05, "y": 1.05 } },
      "transition": { "duration": 1.0, "ease": "InOutSine" }
    },

    // 3. Interaction Overrides
    "hover": {
      "style": { "backgroundColor": "#FF8855" },
      "transition": { "duration": 0.1 } // Snappy hover
    },
    "press": {
      "style": { "scale": { "x": 0.95, "y": 0.95 } }
    },
    "exit": {
      "style": { "opacity": 0, "anchoredPosition": { "y": -50 } }
    }
  }
}

```
