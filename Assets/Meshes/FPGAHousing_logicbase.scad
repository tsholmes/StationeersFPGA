include <FPGA_shared.scad>;
use <FPGAHousing_base.scad>;
use <FPGAHousing_paintcap.scad>;
use <PowerSymbol.scad>;
use <DataSymbol.scad>;

module FPGAHousing_logicbase() {
  difference() {
    FPGAHousing_base();
    cablecutout();
    rotate([0,0,180]) cablecutout();
  }
}

FPGAHousing_logicbase();
%FPGAHousing_paintcap();
%translate([0.067,0.25,-0.001]) rotate([-90, 0, 0]) PowerSymbol();
%translate([0.073,-0.25,0.001]) rotate([90, 0, 0]) DataSymbol();