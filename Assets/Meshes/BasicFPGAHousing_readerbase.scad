include <FPGA_shared.scad>;
use <BasicFPGAHousing_base.scad>;
use <BasicFPGAHousing_paintcap.scad>;
use <PowerSymbol.scad>;
use <DataSymbol.scad>;

module BasicFPGAHousing_readerbase() {
  difference() {
    BasicFPGAHousing_base();
    cablecutout();
    rotate([0,0,90]) cablecutout();
    rotate([0,0,-90]) cablecutout();
  }
}

BasicFPGAHousing_readerbase();
%BasicFPGAHousing_paintcap();
%translate([0.067,0.25,-0.001]) rotate([-90, 0, 0]) PowerSymbol();
%translate([-0.25,0.073,0.001]) rotate([90, 0, 90]) DataSymbol();
%translate([0.25,0.073,0.001]) rotate([90, 0, -90]) DataSymbol();