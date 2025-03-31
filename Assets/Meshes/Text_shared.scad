char_depth = 0.0001;

char_pixelsz = 0.02/5;
char_pheight = 5;
char_max_pwidth = 5;

function sum(list) = list * [for (_=list) 1];
function partialsum(list, end) = list * [for (i=[0:len(list)-1]) i < end ? 1 : 0];

module _Text_Line(a, b) {
  l = norm(b-a);
  f = l > 0 ? (b-a)/l : [1, 0];
  r = [f.y, -f.x];
  ea = -0.5*f;
  eb = (l+0.5)*f;
  sr = 0.5*r;
  sl = -sr;
  polygon([
    a+ea+sr,
    a+eb+sr,
    a+eb+sl,
    a+ea+sl,
  ]*char_pixelsz);
}

module _Text_Segments(pts, off=[0,0]) {
  for (i=[0:len(pts)-2]) {
    pa = pts[i];
    pb = pts[i+1];
    if (len(pa) == 2 && len(pb) == 2)
      _Text_Line(pts[i]+off, pts[i+1]+off);
  }
}

_Letters = [
  [[-2, -2], [-2, 2], [2, 2], [2, -2], [2, 0], [-2, 0]], // A
  [], // B
  [[2, -2], [-2, -2], [-2, 2], [2, 2]], // C
  [[2, 0.5], [2, -2], [-2, -2], [-2, 2], [0.5, 2], [], [3, 0], [0, 3]], // D
  [[2, -2], [-2, -2], [-2, 2], [2, 2], [], [-2, 0], [0, 0]], // E
  [[2, 2], [-2, 2], [-2, -2], [-2, 0], [0, 0]], // F
  [[0, 0], [2, 0], [2, -2], [-2, -2], [-2, 2], [2, 2]], // G
  [], // H
  [[0, -2], [0, 2]], // I
  [], // J
  [], // K
  [[-2, 2], [-2, -2], [2, -2]], // L
  [], // M
  [], // N
  [[-2, -2], [-2, 2], [2, 2], [2, -2], [-2, -2]], // O
  [[-2, -2], [-2, 2], [2, 2], [2, 0], [-2, 0]], // P
  [], // Q
  [[-2, -2], [-2, 2], [2, 2], [2, 0], [-2, 0], [1, -3]], // R
  [], // S
  [], // T
  [], // U
  [], // V
  [], // W
  [], // X
  [], // Y
  [], // Z
  [[-2, 2], [2, 2], [2, 0], [0, 0], [], [0, -2], [0, -2]], // ? for missing
];

function _Letter_Lookup(c) =
  let(
    ci=ord(c),
    li=ci-ord("A"),
    issp=ci==32,
    lps=li>=0&&li<26 ? _Letters[li] : [],
    lpf=len(lps)==0 ? _Letters[26] : lps
  ) issp ? [] : lpf;

function _Letter_Bounds(letter) =
  let(
    fletter = concat([[0, 0]], [for (l=letter) len(l)==2 ? l : [0,0]]),
    xs = [for (l=fletter) l.x],
    ys = [for (l=fletter) l.y]
  ) [min(xs), min(ys), max(xs), max(ys)];

module Text(letters) {
  lbounds = [for (l=letters) _Letter_Bounds(l)];
  lwidths = [for (b=lbounds) min(b.z-b.x+1, char_max_pwidth)];
  lmins = [for (b=lbounds) b.x];
  twid = sum(lwidths) + len(letters)-1;
  startoff = (1-twid)/2;
  offs = [for (i=[0:len(letters)-1]) startoff+i+partialsum(lwidths, i)];
  linear_extrude(height=char_depth) {
    for (i=[0:len(letters)-1]) if (len(letters[i]) > 0) {
      letter = letters[i];
      off = offs[i];
      left = off-lmins[i];
      wid = lwidths[i];
      intersection() {
        _Text_Segments(letter, [left, 0]);
        scale([char_pixelsz,char_pixelsz]) translate([off-0.5, -char_pheight/2])
          square(size=[wid, char_pheight]);
      }
    }
  }
}

module Text(text) {
  letters = [for (c=text) _Letter_Lookup(c)];
  lbounds = [for (l=letters) _Letter_Bounds(l)];
  lwidths = [for (b=lbounds) min(b.z-b.x+1, char_max_pwidth)];
  lmins = [for (b=lbounds) b.x];
  twid = sum(lwidths) + len(letters)-1;
  startoff = (1-twid)/2;
  offs = [for (i=[0:len(letters)-1]) startoff+i+partialsum(lwidths, i)];
  linear_extrude(height=char_depth) {
    for (i=[0:len(letters)-1]) if (len(letters[i]) > 0) {
      letter = letters[i];
      off = offs[i];
      left = off-lmins[i];
      wid = lwidths[i];
      intersection() {
        _Text_Segments(letter, [left, 0]);
        scale([char_pixelsz,char_pixelsz]) translate([off-0.5, -char_pheight/2])
          square(size=[wid, char_pheight]);
      }
    }
  }
}
