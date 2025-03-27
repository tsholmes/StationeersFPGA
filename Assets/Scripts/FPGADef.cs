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

    public static string[] GateNames = new string[64];
    public string[] GateLabels = new string[64];
    public FPGAOp[] GateOps = new FPGAOp[64];
    public byte[] GateInput1s = new byte[64];
    public byte[] GateInput2s = new byte[64];

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

    public string GetGateInputLabel(byte input)
    {
      var name = "invalid";
      var label = "invalid";
      if (input < 64)
      {
        name = InputNames[input];
        label = InputLabels[input];
      }
      else if (input < 128)
      {
        name = GateNames[input - 64];
        label = GateLabels[input - 64];
      }
      else if (input < 192)
      {
        name = LutNames[input - 128];
        label = LutLabels[input - 128];
      }
      return label.Length > 0 ? label : name;
    }

    public static string GetGateInputName(byte input)
    {
      if (input < 64)
      {
        return InputNames[input];
      }
      else if (input < 128)
      {
        return GateNames[input - 64];
      }
      else if (input < 192)
      {
        return LutNames[input - 128];
      }
      return "invalid";
    }
  }
}