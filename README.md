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

#### **UIComponent** 

Inherit from this for interactable elements like Buttons, Cards, or Toggles. It automatically handles Pointer events (`Enter`, `Exit`, `Down`, `Up`) and Toggle value changes.

```csharp
public class MyButton : UIComponent
{
    
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

### 1. Style Properties

These properties define the visual look of the UI element.

| Category | Property | Type | Description |
| --- | --- | --- | --- |
| **Positioning** | `rect` | `Object` | Controls the RectTransform. |
|  |   `anchoredPosition` | `Vector2` | The X/Y position relative to anchors (e.g., `{ "x": 10, "y": -10 }`). |
|  |   `sizeDelta` | `Vector2` | The Width/Height offset (e.g., `{ "x": 200, "y": 50 }`). |
|  |   `anchorMin` | `Vector2` | The normalized min anchor (0-1). |
|  |   `anchorMax` | `Vector2` | The normalized max anchor (0-1). |
|  |   `pivot` | `Vector2` | The normalized pivot point (0-1). |
| **Layout** | `layoutItem` | `Object` | Controls the LayoutElement component. |
|  |   `preferredWidth` | `float` | The preferred width for Layout Groups. |
|  |   `preferredHeight` | `float` | The preferred height for Layout Groups. |
|  |   `flexibleWidth` | `float` | Weight for expanding width (0 = fixed, 1+ = expand). |
|  |   `flexibleHeight` | `float` | Weight for expanding height. |
| **Visuals** | `backgroundColor` | `Hex` | The fill color (e.g., `"#FF0000"` or `"#FF000088"`). |
|  | `opacity` | `float` | The CanvasGroup alpha (0.0 to 1.0). |
|  | `radius` | `float` | Corner radius for the procedural background. |
| **Border** | `border` | `Object` | Controls the procedural border. |
|  |   `width` | `float` | Thickness of the border stroke. |
|  |   `color` | `Hex` | Color of the border stroke. |
| **Shadow** | `shadow` | `Object` | Controls the procedural drop shadow. |
|  |   `color` | `Hex` | Shadow color (include alpha, e.g., `"#00000055"`). |
|  |   `x` | `float` | Horizontal offset. |
|  |   `y` | `float` | Vertical offset. |
|  |   `softness` | `float` | Blur/spread radius of the shadow. |
| **Text** | `textColor` | `Hex` | Color of the TextMeshPro text. |
|  | `fontSize` | `float` | Font size of the text. |
|  | `characterSpacing` | `float` | Spacing between characters. |
| **Transform** | `scale` | `Vector2` | Local scale (e.g., `{ "x": 1.1, "y": 1.1 }`). |
|  | `rotation` | `Vector3` | Local rotation Euler angles (e.g., `{ "z": 90 }`). |

---

### 2. Animation Configuration

The `animation` block controls movement and interaction. The `repeat` logic is now a standalone section, separate from `transition` timing.

#### **Root Animation Properties**

| Section | Key | Type | Description |
| --- | --- | --- | --- |
| **Timing** | `transition` | `Object` | **Global Default Timing.** Defines duration and easing for all state changes unless overridden. |
| **Looping** | `repeat` | `Object` | **Loop Configuration.** Controls the cycling behavior for the `initial` <-> `animate` loop. |

#### **A. Transition Object**

Defines *how fast* and *how smooth* the change is.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `duration` | `float` | `0.2` | Time in seconds for the tween to complete. |
| `delay` | `float` | `0.0` | Time to wait before starting the tween. |
| `ease` | `string` | `"OutQuad"` | Easing function (e.g., `Linear`, `InSine`, `OutBack`, `InOutElastic`). |

#### **B. Repeat Object**

Defines *how* the idle animation loops.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `cycles` | `int` | `1` | Number of times to play the loop.<br>

<br>• **-1**: Infinite loop.<br>

<br>• **1+**: Specific number of repeats. |
| `cycleMode` | `string` | `"Restart"` | Behavior when a cycle ends:<br>

<br>• `"Restart"`: Resets to start value.<br>

<br>• `"Yoyo"`: Smoothly reverses back to start.<br>

<br>• `"Incremental"`: Adds value to the end. |

#### **C. Animation States**

Each state can be a simple Style Object, or a Wrapper (Style + Transition).

| State Key | Description | Behavior |
| --- | --- | --- |
| `enter` | **Entrance** | Snaps to this style on Show, then transitions to Normal/Loop. |
| `exit` | **Exit** | Target style when Hide is called. |
| `initial` | **Loop Start** | Point A of the idle loop. |
| `animate` | **Loop Target** | Point B of the idle loop. |
| `hover` | **Hover** | Active when pointer is over the element. |
| `press` | **Press** | Active when pointer is down. |
| `check` | **Toggle** | Active when Toggle `isOn` is true. |

---

### 3. Updated JSON Example

```json
"#LoaderIcon": {
  "backgroundColor": "#333",
  "animation": {
    
    // 1. GLOBAL TIMING (Duration/Ease only)
    "transition": { 
      "duration": 0.5, 
      "ease": "InOutSine" 
    },

    // 2. LOOP SETTINGS (Separate from transition)
    "repeat": { 
      "cycles": -1, 
      "cycleMode": "Yoyo" 
    },

    // 3. STATES
    "initial": { 
      "style": { "scale": { "x": 1.0, "y": 1.0 } } 
    },
    
    "animate": { 
      "style": { "scale": { "x": 1.2, "y": 1.2 } }
      // Uses global transition (0.5s), but loop behavior comes from 'repeat'
    },
    
    "hover": {
      "style": { "backgroundColor": "#555" },
      // Local Override: Faster transition for hover
      "transition": { "duration": 0.1 } 
    }
  }
}

```
