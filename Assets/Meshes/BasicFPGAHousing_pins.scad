include <FPGA_shared.scad>;
use <BasicFPGAHousing_base.scad>;
use <BasicFPGAHousing_paintcap.scad>;

module BasicFPGAHousing_pins() {
  pinStartOffset = -(decoPinCount+1)/2 * decoPinSpacing;
  bevelSize = decoPinBevelSize * sqrt(2);

  for (edge=[0:3]) rotate([0,0,90*edge]) {
    for (pin=[1:decoPinCount]) translate([capSize/2, pinStartOffset+pin*decoPinSpacing, baseTop]) {
      difference() {
        cube(size=[decoPinLength*2, decoPinWidth, decoPinHeight*2], center=true);
        translate([decoPinLength,0,decoPinHeight]) rotate([0,45,0]) {
          cube(size=[bevelSize,decoPinWidth*2,bevelSize],center=true);
        }
      }
    }
  }
}

BasicFPGAHousing_pins();
%BasicFPGAHousing_base();
%BasicFPGAHousing_paintcap();