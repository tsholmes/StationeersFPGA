include <FPGA_shared.scad>;
use <ConfigScrew.scad>;
use <BasicFPGAHousing_readerbase.scad>;
use <BasicFPGAHousing_paintcap.scad>;
use <Text_FPGAREADER.scad>;

screw_offset = 0.16;
screw_spacing = 0.075;

module BasicFPGAHousing_readerscrews() {
  for (sy=[-2:1]) for (sx=[-1:2:1]) translate([sx*screw_offset,sy*screw_spacing,capDepth+baseTop]) {
    ConfigScrew();
  }
}

BasicFPGAHousing_readerscrews();
%BasicFPGAHousing_readerbase();
%BasicFPGAHousing_paintcap();
%translate([0,0.16,0.095]) Text_FPGAREADER();