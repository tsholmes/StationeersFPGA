using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace fpgamod
{
  public class FPGADef
  {
    public static string[] InputNames = new string[64];
    public string[] InputLabels = new string[64];

    public static String[] GateNames = new string[64];
    public string[] GateLabels = new string[64];

    public static string[] LutNames = new string[64];
    public string[] LutLabels = new string[64];
    public double[] LutValues = new double[64];

    static FPGADef()
    {
      for (var i = 0; i < 64; i++)
      {
        InputNames[i] = $"in{i:D2}";
        GateNames[i] = $"gate{i:D2}";
        LutNames[i] = $"lut{i:D2}";
      }
    }

    public FPGADef()
    {
      Array.Fill(this.InputLabels, "");
      Array.Fill(this.GateLabels, "");
      Array.Fill(this.LutLabels, "");
    }
  }
}