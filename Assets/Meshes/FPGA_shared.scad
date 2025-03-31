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
chipTotalSize = slotSize - slotPinEdgePad*2 - slotPinSize/2;
chipTotalHeight = 0.1;
chipBaseHeight = 0.05;
pinHorizLength = 0.022;
pinHorizUnderLength = 0.008;
pinHorizWidth = 0.02;
pinHorizHeight = 0.015;
pinVertInnerOffset = 0.003;
pinVertOuterOffset = 0.003;
pinVertNarrowOffset = 0.003;
pinTotalHeight = chipTotalHeight - chipBaseHeight/2 + pinHorizHeight/2;
chipBaseSize = chipTotalSize - pinHorizLength*2;
chipBaseBevelSize = 0.005;
alignHoleRadius = 0.005;
alignHoleOffset = chipBaseSize/2 - 0.02;
alignHoleDepth = 0.005;
alignHoleSides = 8;
decoPinCount = 8;
decoPinWidth = 0.036;
decoPinLength = 0.034;
decoPinHeight = 0.028;
decoPinSpacing = 0.049;
decoPinBevelSize = 0.010;

module cablecutout() {
  cableradius = 0.025;
  cableDepth = 0.0125;
  translate([0,smallGridSize/2,0]) rotate([90,0,0]) {
    cylinder(h=cableDepth*2, r=cableradius, center=true, $fn=8);
  }
}