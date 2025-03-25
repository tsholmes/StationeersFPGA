include <FPGA_shared.scad>;
use <BasicFPGAHousing_paintcap.scad>;
use <PowerSymbol.scad>;
use <DataSymbol.scad>;

module BasiFPGAHousing_chipcutout() {
  slotPinBaseOffset = (-slotPinEdgeCount-1)/2 * slotPinEdgeSpacing;
  slotPinEdgeOffset = slotSize/2 - slotPinSize/2 - slotPinEdgePad;
  union() {
    translate([0, 0, baseDepth/2 + slotBottom + slotPinDepth]) {
      cube(size=[slotSize, slotSize, baseDepth], center=true);
    }
    for(edge=[1:4]) rotate([0,0,90*edge]) {
      for(i=[1:slotPinEdgeCount]) {
        translate([
          slotPinBaseOffset + slotPinEdgeSpacing*i,
          slotPinEdgeOffset,
          slotBottom + slotPinDepth
        ]) {
          cube(size=[slotPinSize, slotPinSize, slotPinDepth*2], center=true);
        }
      }
    }
    translate([alignHoleOffset,alignHoleOffset,slotBottom + slotPinDepth])
      cylinder(h=alignHoleDepth*2, r=alignHoleRadius, center=true, $fn=alignHoleSides);
  }
}

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
%translate([0.067,0.25,-0.001]) rotate([-90, 0, 0]) PowerSymbol();
%translate([0.073,-0.25,0.001]) rotate([90, 0, 0]) DataSymbol();