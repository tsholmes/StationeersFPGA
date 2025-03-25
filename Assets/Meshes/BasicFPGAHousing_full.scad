use <BasicFPGAHousing_base.scad>;
use <BasicFPGAHousing_paintcap.scad>;
use <PowerSymbol.scad>;
use <DataSymbol.scad>;

module BasicFPGAHousing_full() {
  BasicFPGAHousing_base();
  BasicFPGAHousing_paintcap();
  translate([0.067,0.25,-0.001]) rotate([-90, 0, 0]) PowerSymbol();
  translate([0.073,-0.25,0.001]) rotate([90, 0, 0]) DataSymbol();
}

BasicFPGAHousing_full();