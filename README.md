# UI Toolkit Framework

A high-performance, data-driven UI architecture for Unity's **UI Toolkit**. This framework separates logic from visuals, allowing for CSS-like styling, hot-reloading, and complex state-driven animations using JSON.

## 1. Core Architecture

### **UIBase**
The pure C# base class for all UI Toolkit controllers.
- **Asynchronous Lifecycle**: Handles `VisualTreeAsset` loading, initialization, and disposal via UniTask.
- **Auto-Styling**: Elements are automatically styled via IDs (`#Id`) and Classes (`.Class`).
- **Hooks**: Provides `OnInitializeAsync`, `OnShowAsync`, and `OnHideAsync`.

### **StyleManager**
The central engine that manages theme data.
- **Hot-Reloading**: Watches `StreamingAssets/style.json` and instantly applies visual changes in the Editor and at Runtime.
- **Theming**: Consolidates variables and shared styles across the entire UI.

---

## 2. JSON Styling System

Visuals are defined in `StreamingAssets/style.json`. The syntax is inspired by CSS but optimized for the UI Toolkit property model.

### Variables
Define shared values to maintain consistency.
```json
{
  "variables": {
    "accentColor": "#FF5733",
    "mainPadding": "12px"
  }
}
```
Reference them in styles using `var(--name)` or `$name`:
`"backgroundColor": "var(--accentColor)"`

### Style Properties
| Category | Properties |
| :--- | :--- |
| **Shorthands** | `margin`, `padding`, `borderRadius`, `borderWidth`, `borderColor` |
| **Layout** | `width`, `height`, `flexGrow`, `flexShrink`, `position`, `top`, `bottom`, `left`, `right` |
| **Colors** | `backgroundColor`, `color`, `borderTopColor`, etc. |
| **Text** | `fontSize`, `letterSpacing`, `unityTextAlign` |
| **Transform** | `opacity`, `scale` (Vector2), `rotation` (Vector3) |

---

## 3. Advanced Features

### **Shadows & Text Shadows**
The framework supports CSS-like string parsing for shadows.
- **Format**: `[offsetX] [offsetY] [blur] [color]`
- **Examples**:
  - ` "textShadow": "2px 2px 5px rgba(0,0,0,0.5)" `
  - ` "shadow": "0 10 20 #00000088" `

> [!NOTE]
> `textShadow` maps natively to UI Toolkit. `shadow` is stored in the style state for use by custom procedural renderers or shaders.

### **Asynchronous Background Images**
Background textures are loaded asynchronously from `StreamingAssets` to keep the UI thread responsive.
- **Property**: `backgroundImage`
- **Path**: Relative to `StreamingAssets` (e.g., `"textures/panel_bg.png"`).
- **Optimization**: The bridge automatically detects path changes to prevent redundant loads during animations.

---

## 4. Animation System

Powered by **PrimeTween**, the framework allows you to define complex transitions between states purely in JSON.

### Animation States
- **`initial`**: The starting style on instantiation.
- **`enter`**: The target style when the element is shown (e.g., fade in from opacity 0).
- **`exit`**: The target style when the element is hidden.
- **`hover`**: Triggered on pointer over.
- **`press`**: Triggered on pointer down.

### Example Configuration
```json
"#SubmitBtn": {
  "backgroundColor": "#333",
  "animation": {
    "transition": { "duration": 0.25, "ease": "OutQuad" },
    "enter": {
      "style": { "opacity": 0, "scale": { "x": 0.8, "y": 0.8 } },
      "transition": { "duration": 0.5, "ease": "OutBack" }
    },
    "hover": {
      "style": { "backgroundColor": "$accentColor" }
    }
  }
}
```

---

## 5. Summary of Built-in Components

### **UIView**
Used for top-level screens and windows. Manages the injection into the main UI hierarchy.

### **UIElement**
Used for discrete interactive parts of a view (Buttons, Cards, Icons). Handles events and local styling.
