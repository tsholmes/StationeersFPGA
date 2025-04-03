include <FPGA_shared.scad>;
use <FPGAChip_base.scad>;
use <FPGAChip_pins.scad>;
use <Text_FPGA.scad>;

color("#111") FPGAChip_base();
color("#DDD") FPGAChip_pins();
color("#FFF") translate([0,0,0.05]) Text_FPGA();