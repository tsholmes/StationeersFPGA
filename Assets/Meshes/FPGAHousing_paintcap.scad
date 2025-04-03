include <FPGA_shared.scad>;
use <FPGAHousing_base.scad>;

module FPGAHousing_paintcap() {
  bevelDepth = capBevel * sqrt(2);
  difference() {
    translate([0,0,baseTop]) {
      cube(size=[capSize, capSize, capDepth*2], center=true);
    }
    cube(size=[slotSize+eps*10, slotSize+eps*10, smallGridSize], center=true);
    for (edge=[1:4]) rotate([0,0,90*edge]) {
      translate([0,capSize/2,baseTop+capDepth]) rotate([45,0,0])
        cube(size=[capSize+0.1, bevelDepth, bevelDepth*2], center=true);
    }
  }
}

%FPGAHousing_base();
FPGAHousing_paintcap();