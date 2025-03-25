symWidth = 0.038;
symHeight = 0.086;
symDepth = 0.01;
middleHeight = 0.0075;
topHorizOffset = 0.007;
sideVertOffset = 0.007;

module PowerSymbol() {
  linear_extrude(symDepth, center=true) {
    halfHei = symHeight/2;
    halfWid = symWidth/2;
    halfMid = middleHeight/2;
    polygon([
      [topHorizOffset, halfHei],
      [0, halfMid],
      [halfWid, sideVertOffset],
      [-topHorizOffset, -halfHei],
      [0, -halfMid],
      [-halfWid, -sideVertOffset]
    ]);
  }
}

PowerSymbol();