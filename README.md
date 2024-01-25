# SolidEdge-Voronoi
Voronoi diagram generator to Solid Edge sketch

Part of the code is adapted from https://github.com/RafaelKuebler/DelaunayVoronoi

# New release with relaxation and usage of the csDelaunay library
https://github.com/jfg8/csDelaunay

# VB.net Delaunay triangulation + Voronoi Diagram

A VB.net implementation of the [Bowyer–Watson algorithm](https://en.wikipedia.org/wiki/Bowyer%E2%80%93Watson_algorithm).
The result is a [Delaunay triangulation](https://en.wikipedia.org/wiki/Delaunay_triangulation) for a set of randomly generated points.
Following the Delaunay triangulation, the dual [Voronoi diagram](https://en.wikipedia.org/wiki/Voronoi_diagram) is constructed.

# Installation

The application is standalone, just unzip the archive in a folder of your choice.
You can personalize Solid Edge UI to add a button that starts the Voronoi generator application; [How to add a button in Solid Edge UI](https://community.sw.siemens.com/s/question/0D54O000061xsT3SAI/how-can-i-create-a-new-button-in-solidedge-toolbar-and-run-my-vbnet-code-from-it-)

# UI description

- The first textbox beside the Point label is the number of points; if you change this value you need to re-generate the Voronoi diagram
- The second textbox is the number of relaxations to perform; if you change this value the Voronoi diagram is automatically regenerated
- Draw button is used to random choose points and generate a new Voronoi diagram
- Draw in SE button is used to transfer the current Voronoi diagram in Solid Edge active sketch
- Points, Triangulation, Circles, and Voronoi options are to choose what to draw in the application, changing them will refresh the view

# Usage

- Start the application and generate a Voronoi diagram by the button "Draw"
- Open Solid Edge and open or create an Ordered Part or an Ordered SheetMetal
- Create or edit a Sketch
- While in the sketch use the "Draw in SE" button to transfer the current Voronoi diagram to Solid Edge

<img alt="User interface" src="Images/UI.png" width="700">

<img alt="Relax 1" src="Images/relax1.png" width="700">
<img alt="Relax 2" src="Images/relax2.png" width="700">
<img alt="Relax 3" src="Images/relax20.png" width="700">

<img alt="Solid Edge sketch" src="Images/Sketch.png" width="700">
<img alt="Rendering" src="Images/Render.png" width="700">

Releases here: https://github.com/farfilli/SolidEdge-Voronoi/releases

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details

## Acknowledgments

* [Procedural Dungeon Generation Algorithm](https://www.gamasutra.com/blogs/AAdonaac/20150903/252889/Procedural_Dungeon_Generation_Algorithm.php)
* [Polygonal Map Generation for Games](http://www-cs-students.stanford.edu/~amitp/game-programming/polygon-map-generation/)
* [Check if point is in circumcircle of a triangle (TitohuanT's answer)](https://stackoverflow.com/questions/39984709/how-can-i-check-wether-a-point-is-inside-the-circumcircle-of-3-points)
