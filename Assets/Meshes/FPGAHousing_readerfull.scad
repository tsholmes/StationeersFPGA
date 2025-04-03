use <FPGAHousing_readerbase.scad>;
use <FPGAHousing_paintcap.scad>;
use <FPGAHousing_pins.scad>;
use <FPGAHousing_readerscrews.scad>;
use <PowerSymbol.scad>;
use <DataSymbol.scad>;
use <Text_FPGAREADER.scad>;

module FPGAHousing_readerfull() {
  color("#444") FPGAHousing_readerbase();
  color("#444") FPGAHousing_paintcap();
  color("#FFF") FPGAHousing_pins();
  color("#888") FPGAHousing_readerscrews();
  color("#F00") translate([0.067,0.25,-0.001]) rotate([-90, 0, 0]) PowerSymbol();
  color("#0F0") translate([-0.25,0.073,0.001]) rotate([90, 0, 90]) DataSymbol();
  color("#0F0") translate([0.25,0.073,0.001]) rotate([90, 0, -90]) DataSymbol();
  color("#FFF") translate([0,0.16,0.095]) Text_FPGAREADER();
}

FPGAHousing_readerfull();