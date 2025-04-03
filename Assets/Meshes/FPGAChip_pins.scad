include <FPGA_shared.scad>;
use <FPGAChip_base.scad>;

module FPGAPin() {
  pinTop = chipTotalHeight/2 - chipBaseHeight/2 + pinHorizHeight/2;
  pinVCenter = pinTop - pinTotalHeight/2;
  pinBottomInnerEdge = pinHorizUnderLength + pinVertInnerOffset;
  pinInnerHeight = pinTotalHeight - pinHorizHeight;
  pinInnerAngle = atan2(-pinVertInnerOffset, pinInnerHeight);
  pinInnerCutoutHeight = pinInnerHeight / cos(pinInnerAngle);
  pinBottomOuterEdge = pinHorizLength - pinVertOuterOffset;
  pinOuterAngle = atan2(pinVertOuterOffset, pinTotalHeight);
  pinBottomNarrowEdge = pinHorizWidth/2 - pinVertNarrowOffset;
  pinNarrowAngle = atan2(pinVertNarrowOffset, pinTotalHeight);
  translate([0,0,pinVCenter]) difference() {
    translate([0, 0, 0])
      cube(size=[pinHorizLength*2, pinHorizWidth, pinTotalHeight], center=true);
    translate([pinHorizUnderLength-pinHorizLength, 0, -pinHorizHeight])
      cube(size=[pinHorizLength*2, pinHorizWidth*2, pinTotalHeight], center=true);
    translate([pinBottomInnerEdge,0,-pinHorizHeight/2-pinInnerHeight/2])
      rotate([0,pinInnerAngle,0])
      translate([-pinVertInnerOffset,0,0])
        cube(size=[pinVertInnerOffset*2, pinHorizWidth*2, pinInnerCutoutHeight*2], center=true);
    translate([pinBottomOuterEdge,0,-pinTotalHeight/2])
      rotate([0,pinOuterAngle,0])
      translate([pinVertOuterOffset,0,0])
        cube(size=[pinVertOuterOffset*2, pinHorizWidth*2, pinTotalHeight*4], center=true);
    translate([0,pinBottomNarrowEdge,-pinTotalHeight/2])
      rotate([-pinNarrowAngle,0,0])
      translate([0,pinVertNarrowOffset,0])
        cube(size=[pinHorizLength*4,pinVertNarrowOffset*2, pinTotalHeight*4], center=true);
    translate([0,-pinBottomNarrowEdge,-pinTotalHeight/2])
      rotate([pinNarrowAngle,0,0])
      translate([0,-pinVertNarrowOffset,0])
        cube(size=[pinHorizLength*4,pinVertNarrowOffset*2, pinTotalHeight*4], center=true);
  }
}

module FPGAChip_pins() {
  for (edge=[0:3])
    for (off=[1:slotPinEdgeCount])
      rotate([0,0,90*edge])
        translate([chipBaseSize/2, slotPinEdgeSpacing * (off - (slotPinEdgeCount+1)/2), 0])
        FPGAPin();
}

FPGAChip_pins();
%FPGAChip_base();