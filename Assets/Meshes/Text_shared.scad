char_width = 0.02;
char_height = 0.02;
char_depth = 0.0001;
char_spacing = 0.025;

charpt_gridx = 5;
charpt_gridy = 5;

char_pixelsz = 0.02/5;

function charpt(cx, cy) = [
  (cx-charpt_gridx/2)*char_width/charpt_gridx,
  (cy-charpt_gridy/2)*char_height/charpt_gridy,
];

function sum(list) = list * [for (_=list) 1];
function partialsum(list, end) = list * [for (i=[0:len(list)-1]) i < end ? 1 : 0];

module _Text_Line(a, b) {
  polygon([
    [min(a.x, b.x)-0.5, min(a.y, b.y)-0.5],
    [max(a.x, b.x)+0.5, min(a.y, b.y)-0.5],
    [max(a.x, b.x)+0.5, max(a.y, b.y)+0.5],
    [min(a.x, b.x)-0.5, max(a.y, b.y)+0.5],
  ]*char_pixelsz);
}

module _Text_Segments(pts, off=[0,0]) {
  for (i=[0:len(pts)-2]) {
    _Text_Line(pts[i]+off, pts[i+1]+off);
  }
}

Letter_A = [
  [-2, -2],
  [-2, 2],
  [2, 2],
  [2, -2],
  [2, 0],
  [-2, 0],
];

Letter_C = [
  [2, -2],
  [-2, -2],
  [-2, 2],
  [2, 2],
];

Letter_F = [
  [2, 2],
  [-2, 2],
  [-2, -2],
  [-2, 0],
  [0, 0],
];

Letter_G = [
  [0, 0],
  [2, 0],
  [2, -2],
  [-2, -2],
  [-2, 2],
  [2, 2],
];

Letter_I = [
  [0, -2],
  [0, 2],
];

Letter_L = [
  [-2, 2],
  [-2, -2],
  [2, -2],
];

Letter_O = [
  [-2, -2],
  [-2, 2],
  [2, 2],
  [2, -2],
  [-2, -2],
];

Letter_P = [
  [-2, -2],
  [-2, 2],
  [2, 2],
  [2, 0],
  [-2, 0],
];

module Text(letters) {
  lxs = [for (letter=letters) concat([0], [for (pt=letter) pt.x])];
  lwidths = [for (xs=lxs) max(xs)-min(xs)+1];
  lmins = [for (xs=lxs) min(xs)];
  twid = sum(lwidths) + len(letters)-1;
  startoff = (1-twid)/2;
  offs = [for (i=[0:len(letters)-1]) startoff+i+partialsum(lwidths, i)];
  linear_extrude(height=char_depth) {
    for (i=[0:len(letters)-1]) if (len(letters[i]) > 0)
      _Text_Segments(letters[i], [offs[i]-lmins[i], 0]);
  }
}