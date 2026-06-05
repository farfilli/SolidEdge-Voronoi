\# Voronoi Editor for Solid Edge







A WinForms VB.NET application for generating, editing, previewing, and exporting Voronoi-based geometric patterns. The program supports both a standard rectangular working domain and closed sketch profiles imported from Solid Edge, including regions with inner holes.\[1]\[2]



\## Overview



The application generates Voronoi cells from editable seed points and displays them on an interactive canvas with multiple rendering modes.\[1]\[3] It also includes a shared export pipeline that converts the visible result into geometric paths suitable for SVG, DXF, and Solid Edge sketch output.\[1]\[4]



\## Main Features



\- Generate Voronoi diagrams inside the default rectangular domain.\[1]

\- Read closed sketch profiles from the active Solid Edge sketch and use them as generation boundaries.\[2]

\- Support multiple outer loops and hole regions inside imported sketch domains.\[1]\[2]

\- Edit seed points interactively on the canvas and rebuild the geometry after changes.\[1]\[3]

\- Render cells as straight edges, curved inner paths, circles, regular polygons, and star-based symbols including Star, Star3, and Star4.\[3]\[4]

\- Export the generated geometry to SVG, DXF, or directly to the active Solid Edge sketch.\[1]\[4]



\## User Interface







The main window combines a compact control sidebar on the left with a large drawing canvas on the right.\[1] The sidebar exposes generation parameters, rendering options, sketch import controls, and export commands, while the canvas is responsible for visualization and direct seed manipulation.\[1]\[3]



\## Random Voronoi Generation







The default workflow creates Voronoi seed points inside a rectangular domain defined in the main form and optionally applies relaxation passes before building the final cells.\[1] This provides a fast way to explore procedural layouts before moving to constrained CAD-oriented domains.\[1]



\## Sketch Profile Import







When Solid Edge is running with an active sketch, the application can read lines, arcs, and circles, reconstruct closed loops, and classify them into outer boundaries and holes.\[2] Those loops are then converted into sketch domains used to constrain Voronoi generation inside the imported profile.\[1]\[2]



\## Seed Editing







Seed points can be edited directly on the canvas, allowing local refinement of the generated pattern without restarting the whole process.\[3] After a seed edit, the form updates the current seed list and rebuilds the Voronoi cells from the edited positions.\[1]\[3]



\## Rendering Styles







The rendering system supports several `CellRenderStyle` modes, including Straight, Curved, Circle, Square, RoundedSquare, Triangle, Pentagon, Hexagon, Octagon, Star, Star3, and Star4.\[3] Inner curve and symbol corner behavior can be controlled independently with sharp, Bezier, or fillet-arc handling depending on the selected mode.\[3]\[4]



\## Export Output







The export pipeline converts the current visual result into line, arc, and cubic Bezier segments, making the output reusable across multiple destinations.\[4] The same exported geometry can be saved as SVG, written as DXF, or sent directly into the active Solid Edge sketch.\[1]\[4]



\## Workflow



1\. Open the application and choose a rendering style.\[1]

2\. Generate a random Voronoi diagram or import a closed sketch profile from Solid Edge.\[1]\[2]

3\. Adjust cell count, seed, relaxation, scaling, offset, trim, bulge, and symbol options from the sidebar.\[1]

4\. Refine the layout by dragging seed points directly on the canvas.\[1]\[3]

5\. Export the final geometry to SVG, DXF, or Solid Edge.\[1]\[4]



\## Project Structure



Typical source files in the project include:



\- `MainForm.vb` — main UI, command flow, sketch import, and export actions.\[1]

\- `VoronoiCanvas.vb` — drawing surface, rendering logic, and interactive seed editing.\[3]

\- `VoronoiEngine.vb` — seed creation, Voronoi construction, and relaxation logic.\[5]

\- `Geometry.vb` — shared geometric helper functions.\[6]

\- `SolidEdgeExporter.vb` — Solid Edge interoperability for sketch reading and export.\[2]

\- `ExportGeometry.vb` — conversion of rendered cells into exportable paths.\[4]



\## Requirements



\- Windows with support for VB.NET WinForms execution.\[1]

\- Solid Edge installed and running for sketch import and direct CAD export features.\[2]

\- An active Solid Edge document with an active sketch when using sketch-boundary import.\[2]



\## Notes



The application is designed as a practical geometry authoring tool rather than only a visual experiment.\[3]\[4] Because rendering, editing, and export all rely on shared geometric representations, the result seen on screen can be transferred to CAD-oriented outputs with minimal manual reconstruction.\[3]\[2]\[4]

