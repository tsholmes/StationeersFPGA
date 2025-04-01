screw_radius = 0.031;
screw_height = 0.030;

notch_width = 0.015;
notch_depth = 0.010;

module ConfigScrew() {
  difference() {
    translate([0,0,screw_height*0.45])
      cylinder(h=screw_height*1.1, r=screw_radius, center=true, $fn=6);
    translate([0,0,screw_height])
      cube([notch_width, screw_radius*3, notch_depth*2], center=true);
  }
}

ConfigScrew();