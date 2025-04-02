include <FPGA_shared.scad>;
use <BasicFPGAChip_base.scad>;
use <BasicFPGAChip_pins.scad>;
use <Text_FPGA.scad>;

color("#111") BasicFPGAChip_base();
color("#DDD") BasicFPGAChip_pins();
color("#FFF") translate([0,0,0.05]) Text_FPGA();