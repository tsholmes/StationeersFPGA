use <FPGAHousing_logicbase.scad>;
use <FPGAHousing_paintcap.scad>;
use <FPGAHousing_pins.scad>;
use <PowerSymbol.scad>;
use <DataSymbol.scad>;
use <Text_FPGALOGIC.scad>;

module FPGAHousing_logicfull() {
  color("#444") FPGAHousing_logicbase();
  color("#444") FPGAHousing_paintcap();
  color("#FFF") FPGAHousing_pins();
  color("#F00") translate([0.067,0.25,-0.001]) rotate([-90, 0, 0]) PowerSymbol();
  color("#0F0") translate([0.073,-0.25,0.001]) rotate([90, 0, 0]) DataSymbol();
  color("#FFF") translate([0,0.16,0.095]) Text_FPGALOGIC();
}

FPGAHousing_logicfull();