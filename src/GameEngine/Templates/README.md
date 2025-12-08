# UI Templates

This directory contains reusable UI templates that can be instantiated in your game.

## PerformanceMonitorUITemplate

A drop-in UI overlay that displays real-time performance metrics including FPS, frame time, and subsystem timings.

### Usage

```csharp
// Create default overlay (Top-Left)
var overlay = new PerformanceMonitorUITemplate();

// Customize position
var customOverlay = new PerformanceMonitorUITemplate
{
    Position = new Vector2D<float>(20, 20),
    Alignment = new Vector2D<float>(1.0f, -1.0f) // Top-Right
};
```

### Features

- Real-time FPS and Frame Time display
- Subsystem timing breakdown (Update, Render)
- Visual warning when frame time exceeds threshold (default 16.67ms)
- Customizable position and alignment
- Toggle visibility support (via external behavior)
