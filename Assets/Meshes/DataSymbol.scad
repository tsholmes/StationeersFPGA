boxSize = 0.029;
lineSize = 0.0045;
symDepth = 0.01;
vertSeparation = 0.019;
horizSeparation = 0.018;

module DataSymbol() {
  boxVertCenter = (boxSize+vertSeparation)/2;
  boxHorizCenter = (boxSize+horizSeparation)/2;
  linear_extrude(symDepth, center=true) {
    translate([0,boxVertCenter]) square(boxSize, center=true); // top box
    translate([-boxHorizCenter,-boxVertCenter]) square(boxSize, center=true); // bottom left box
    translate([boxHorizCenter,-boxVertCenter]) square(boxSize, center=true); // bottom right box
    square([2*boxHorizCenter + lineSize, lineSize], center=true); // middle line
    translate([0, horizSeparation/4]) square([lineSize, horizSeparation/2 + lineSize], center=true); // line to top
    translate([-boxHorizCenter, -horizSeparation/4]) square([lineSize, horizSeparation/2 + lineSize], center=true); // line to bottom left
    translate([boxHorizCenter, -horizSeparation/4]) square([lineSize, horizSeparation/2 + lineSize], center=true); // line to bottom right
  }
}

DataSymbol();