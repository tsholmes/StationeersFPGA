include <FPGA_shared.scad>;
use <FPGAHousing_logicfull.scad>;
use <FPGAChip_pins.scad>;
use <Text_FPGA.scad>;

module FPGAChip_base() {
  bevelHeight = (chipBaseHeight - pinHorizHeight)/2;
  bevelAngle = atan2(chipBaseBevelSize, bevelHeight);
  translate([0, 0, chipTotalHeight/2 - chipBaseHeight/2]) difference() {
    cube(size=[chipBaseSize, chipBaseSize, chipBaseHeight], center=true);
    for (side=[0:3]) rotate([0,0,90*side])
      for (vside = [-1:2:1])
        translate([0,chipBaseSize/2,vside*pinHorizHeight/2])
          rotate([vside*bevelAngle,0,0])
          translate([0,chipBaseBevelSize,0])
            cube(size=[chipBaseSize*2,chipBaseBevelSize*2,bevelHeight*4], center=true);
    translate([alignHoleOffset,alignHoleOffset,chipBaseHeight/2])
      cylinder(h=alignHoleDepth*2, r=alignHoleRadius, center=true, $fn=alignHoleSides);
  }
}

FPGAChip_base();
%FPGAChip_pins();
%translate([0,0,0.05]) Text_FPGA();
*%translate([0,0,-slotBottom-chipTotalHeight/2]) FPGAHousing_logicfull();
