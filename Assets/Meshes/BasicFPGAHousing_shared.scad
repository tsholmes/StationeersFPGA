smallGridSize = 0.5;
baseSize = smallGridSize;
baseTop = 0.05;
baseBottom = -0.045;
baseDepth = baseTop - baseBottom;
baseCenter = (baseTop + baseBottom)/2;
baseBackRim = 0.02;
baseBackCutoutSize = baseSize - baseBackRim*2;
baseBackCutoutDepth = 0.01;
capSize = 0.41;
capDepth = 0.045;
capBevel = 0.01;
slotSize = smallGridSize * 0.5;
slotBottom = 0.03;
slotPinEdgeCount = 4;
slotPinEdgePad = 0.01;
slotPinEdgeSpacing = 0.045;
slotPinSize = 0.025;
slotPinDepth = 0.025;
eps = 0.00001;

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
  }
}

module cablecutout() {
  cableradius = 0.025;
  cableDepth = 0.0125;
  translate([0,smallGridSize/2,0]) rotate([90,0,0]) {
    cylinder(h=cableDepth*2, r=cableradius, center=true, $fn=8);
  }
}