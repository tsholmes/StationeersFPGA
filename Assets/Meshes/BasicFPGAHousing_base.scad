include <BasicFPGAHousing_shared.scad>;
use <BasicFPGAHousing_paintcap.scad>;
use <PowerSymbol.scad>;

module BasicFPGAHousing_base() {
  middleSize = (slotSize + capSize) / 2;
  difference() {
    union() {
      translate([0,0,baseCenter])
        cube(size=[baseSize, baseSize, baseDepth], center=true);
      translate([0,0,baseTop-eps])
        cube(size=[middleSize, middleSize, capDepth*2], center=true);
    }
    translate([0,0,baseBottom])
      cube(size=[baseBackCutoutSize, baseBackCutoutSize, baseBackCutoutDepth*2], center=true);
    BasiFPGAHousing_chipcutout();
    cablecutout();
    rotate([0,0,180]) cablecutout();
  }
}

BasicFPGAHousing_base();
%BasicFPGAHousing_paintcap();
%translate([0.05,smallGridSize/2,0]) rotate([-90, 0, 0]) PowerSymbol();