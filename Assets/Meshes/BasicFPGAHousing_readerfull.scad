use <BasicFPGAHousing_readerbase.scad>;
use <BasicFPGAHousing_paintcap.scad>;
use <BasicFPGAHousing_pins.scad>;
use <BasicFPGAHousing_readerscrews.scad>;
use <PowerSymbol.scad>;
use <DataSymbol.scad>;
use <Text_FPGAREADER.scad>;

module BasicFPGAHousing_readerfull() {
  color("#444") BasicFPGAHousing_readerbase();
  color("#444") BasicFPGAHousing_paintcap();
  color("#FFF") BasicFPGAHousing_pins();
  color("#888") BasicFPGAHousing_readerscrews();
  color("#F00") translate([0.067,0.25,-0.001]) rotate([-90, 0, 0]) PowerSymbol();
  color("#0F0") translate([-0.25,0.073,0.001]) rotate([90, 0, 90]) DataSymbol();
  color("#0F0") translate([0.25,0.073,0.001]) rotate([90, 0, -90]) DataSymbol();
  color("#FFF") translate([0,0.16,0.095]) Text_FPGAREADER();
}

BasicFPGAHousing_readerfull();