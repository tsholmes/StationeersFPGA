use <BasicFPGAHousing_base.scad>;
use <BasicFPGAHousing_paintcap.scad>;
use <BasicFPGAHousing_pins.scad>;
use <PowerSymbol.scad>;
use <DataSymbol.scad>;
use <Text_FPGALOGIC.scad>;

module BasicFPGAHousing_full() {
  color("#444") BasicFPGAHousing_base();
  color("#444") BasicFPGAHousing_paintcap();
  color("#FFF") BasicFPGAHousing_pins();
  color("#F00") translate([0.067,0.25,-0.001]) rotate([-90, 0, 0]) PowerSymbol();
  color("#0F0") translate([0.073,-0.25,0.001]) rotate([90, 0, 0]) DataSymbol();
  color("#FFF") translate([0,0.16,0.095]) Text_FPGALOGIC();
}

BasicFPGAHousing_full();