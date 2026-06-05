\# Voronoi Editor for Solid Edge



A WinForms VB.NET application for generating, editing, previewing, and exporting Voronoi-based geometric patterns. The program supports standard rectangular domains as well as closed sketch profiles read from Solid Edge, including regions with internal holes.\[1]\[2]



\## Overview



The application builds Voronoi cells from editable seed points and renders them in several visual styles, including straight cells, curved cells, regular polygons, circles, and star-based symbols.\[1]\[3] It can export the visible geometry to SVG, DXF, or directly into the active Solid Edge sketch, using a shared export pipeline based on lines, arcs, and cubic Bezier segments.\[1]\[4]



\## Main Features



\- Generate Voronoi diagrams from a standard rectangular working area.\[1]

\- Read closed boundary loops from the active Solid Edge sketch and use them as generation domains.\[2]

\- Support multiple sketch regions and hole loops inside outer boundaries.\[1]\[2]

\- Edit seed points interactively on the canvas and rebuild cells after changes.\[1]\[3]

\- Render cells as straight edges, curved inner paths, circles, polygons, and star symbols such as Star, Star3, and Star4.\[3]\[4]

\- Export generated geometry to SVG, DXF, or directly back to Solid Edge.\[1]\[4]



\## Workflow



1\. Open the application.

2\. Generate a random Voronoi layout or read the active Solid Edge sketch profile.\[1]\[2]

3\. Adjust cell count, random seed, relaxation count, scaling, corner trimming, offsets, and symbol options from the sidebar controls.\[1]

4\. Drag seed points on the canvas to refine the layout visually.\[1]\[3]

5\. Export the resulting geometry to SVG, DXF, or the active Solid Edge part sketch.\[1]\[4]



\## Rendering Modes



The canvas supports different visualization modes through the `CellRenderStyle` enumeration, including Straight, Curved, Circle, Square, RoundedSquare, Triangle, Pentagon, Hexagon, Octagon, Star, Star3, and Star4.\[3] Corner behavior can also be controlled independently for inner curves and symbol outlines, with sharp, Bezier, or fillet-arc style handling depending on the selected mode.\[3]\[4]



\## Solid Edge Integration



When a sketch is active in Solid Edge, the application can read lines, arcs, and circles from that sketch, reconstruct closed loops, classify outer boundaries and holes, and convert them into generation regions.\[2] The imported geometry is transformed into the application coordinate system and used both for visualization and constrained Voronoi generation inside the detected sketch domains.\[1]\[2]



\## Export System



The export layer converts the rendered result into reusable geometric paths composed of line, arc, and cubic Bezier segments.\[4] This allows the same generated pattern to be written consistently to SVG, DXF, and Solid Edge while preserving the selected visual style as closely as possible.\[1]\[4]



\## Project Structure



Typical source files in the project include:



\- `MainForm.vb` — main UI, command flow, sketch import, and export actions.\[1]

\- `VoronoiCanvas.vb` — drawing surface, rendering logic, and seed editing behavior.\[3]

\- `VoronoiEngine.vb` — seed creation, cell construction, and relaxation logic.\[5]

\- `Geometry.vb` — geometric utility functions used across the project.\[6]

\- `SolidEdgeExporter.vb` — Solid Edge interoperability for sketch reading and sketch export.\[2]

\- `ExportGeometry.vb` — conversion of rendered cells into exportable geometric paths.\[4]



\## Requirements



\- Windows with .NET / VB.NET WinForms support.\[1]

\- Solid Edge installed and running for sketch import or direct sketch export features.\[2]

\- An active Solid Edge document with an active sketch when using profile reading.\[2]



\## Notes



The program is designed as a practical geometry authoring tool rather than only a visual demo. Because the canvas, export module, and Solid Edge integration all operate on shared geometric representations, changes made in the editor can be carried through to CAD-oriented output with minimal manual reconstruction.\[3]\[2]\[4]

