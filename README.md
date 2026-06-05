# Solid Edge Voronoi Generator

![Main UI](Images/main-ui.png)

A WinForms VB.NET application for generating, editing, previewing, and exporting Voronoi-based geometric patterns. The program supports both a standard rectangular working domain and closed sketch profiles imported from Solid Edge, including regions with inner holes.

## Overview

The application generates Voronoi cells from editable seed points and displays them on an interactive canvas with multiple rendering modes. It also includes a shared export pipeline that converts the visible result into geometric paths suitable for SVG, DXF, and Solid Edge sketch output.

## Main Features

- Generate Voronoi diagrams inside the default rectangular domain.
- Read closed sketch profiles from the active Solid Edge sketch and use them as generation boundaries.
- Support multiple outer loops and hole regions inside imported sketch domains.
- Edit seed points interactively on the canvas and rebuild the geometry after changes.
- Render cells as straight edges, curved inner paths, circles, regular polygons, and star-based symbols, including Star, Star3, and Star4.
- Export the generated geometry to SVG, DXF, or directly to the active Solid Edge sketch.

## User Interface

![Main UI](Images/main-ui.png)

The main window combines a compact control sidebar on the left with a large drawing canvas on the right. The sidebar exposes generation parameters, rendering options, sketch import controls, and export commands, while the canvas is responsible for visualization and direct seed manipulation.

## Random Voronoi Generation

![Random generation](Images/random-generation.png)

The default workflow creates Voronoi seed points inside a rectangular domain defined in the main form and optionally applies relaxation passes before building the final cells. This provides a fast way to explore procedural layouts before moving to constrained CAD-oriented domains.

## Sketch Profile Import

![Sketch profile import](Images/sketch-profile-import.png)

When Solid Edge is running with an active sketch, the application can read lines, arcs, and circles, reconstruct closed loops, and classify them into outer boundaries and holes. Those loops are then converted into sketch domains used to constrain Voronoi generation inside the imported profile.

## Seed Editing

![Seed editing](Images/seed-editing.png)

Seed points can be edited directly on the canvas, allowing local refinement of the generated pattern without restarting the whole process. After a seed edit, the form updates the current seed list and rebuilds the Voronoi cells from the edited positions.

## Rendering Styles

![Rendering styles](Images/render-styles.png)

The rendering system supports several `CellRenderStyle` modes, including Straight, Curved, Circle, Square, RoundedSquare, Triangle, Pentagon, Hexagon, Octagon, Star, Star3, and Star4. Inner curve and symbol corner behavior can be controlled independently with sharp, Bezier, or fillet-arc handling depending on the selected mode.

## Export Output

![Export output](Images/export-output.png)

The export pipeline converts the current visual result into line, arc, and cubic Bezier segments, making the output reusable across multiple destinations. The same exported geometry can be saved as SVG, written as DXF, or sent directly into the active Solid Edge sketch.

## Workflow

1. Open the application and choose a rendering style.
2. Generate a random Voronoi diagram or import a closed sketch profile from Solid Edge.
3. Adjust cell count, seed, relaxation, scaling, offset, trim, bulge, and symbol options from the sidebar.
4. Refine the layout by dragging seed points directly on the canvas.
5. Export the final geometry to SVG, DXF, or Solid Edge.

## Project Structure

Typical source files in the project include:

- `MainForm.vb` — main UI, command flow, sketch import, and export actions.
- `VoronoiCanvas.vb` — drawing surface, rendering logic, and interactive seed editing.
- `VoronoiEngine.vb` — seed creation, Voronoi construction, and relaxation logic.
- `Geometry.vb` — shared geometric helper functions.
- `SolidEdgeExporter.vb` — Solid Edge interoperability for sketch reading and export.
- `ExportGeometry.vb` — conversion of rendered cells into exportable paths.

## Requirements

- Windows with support for VB.NET WinForms execution.
- Solid Edge installed and running for sketch import and direct CAD export features.
- An active Solid Edge document with an active sketch when using sketch-boundary import.

## Notes

The application is designed as a practical geometry authoring tool rather than only a visual experiment. Because rendering, editing, and exporting all rely on shared geometric representations, the result seen on screen can be transferred to CAD-oriented outputs with minimal manual reconstruction.
