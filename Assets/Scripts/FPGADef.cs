using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Assets.Scripts.Util;
using UnityEngine;

namespace fpgamod
{
  public class FPGADef
  {
    public static string[] InputNames = new string[64];
    public static string[] GateNames = new string[64];
    public static string[] LutNames = new string[64];

    static FPGADef()
    {
      for (var i = 0; i < 64; i++)
      {
        InputNames[i] = $"in{i:D2}";
        GateNames[i] = $"gate{i:D2}";
        LutNames[i] = $"lut{i:D2}";
      }
    }

    public static string GetName(byte address)
    {
      if (address < 64)
      {
        return InputNames[address];
      }
      else if (address < 128)
      {
        return GateNames[address - 64];
      }
      else if (address < 192)
      {
        return LutNames[address - 128];
      }
      return "invalid";
    }

    private List<ConfigLine> _configLines = new List<ConfigLine>();
    private int[] _configIndex = new int[192];

    public static FPGADef NewEmpty()
    {
      return new FPGADef();
    }

    public static FPGADef Parse(string raw)
    {
      var lines = raw.Split('\n');
      var cfgLines = new List<ConfigLine>();
      foreach (var line in lines)
      {
        cfgLines.Add(ConfigLine.Parse(line));
      }
      return new FPGADef(cfgLines);
    }

    private FPGADef()
    {
      Array.Fill(this._configIndex, -1);
    }

    private FPGADef(List<ConfigLine> configLines)
    {
      Array.Fill(this._configIndex, -1);
      this._configLines.AddRange(configLines);

      var labelMap = new Dictionary<string, byte>();
      // fill index mapping and make mapping of label to address
      for (var idx = 0; idx < this._configLines.Count; idx++)
      {
        var cfg = this._configLines[idx];
        if (!cfg.HasValidAddress)
        {
          continue;
        }
        if (this._configIndex[cfg.Address] != -1)
        {
          cfg.IsDuplicate = true;
          this._configLines[idx] = cfg;
          continue;
        }
        this._configIndex[cfg.Address] = idx;
        if (!string.IsNullOrEmpty(cfg.Label))
        {
          labelMap[cfg.Label] = cfg.Address;
        }
      }

      for (var idx = 0; idx < this._configLines.Count; idx++)
      {
        var cfg = this._configLines[idx];
        cfg.ResolveInputs(labelMap);
        this._configLines[idx] = cfg;
      }
    }

    public string GetLabel(byte address, bool nameFallback = true)
    {
      var name = nameFallback ? GetName(address) : "";
      var idx = address < 192 ? this._configIndex[address] : -1;

      if (idx == -1)
      {
        return name;
      }
      var cfg = this._configLines[idx];
      if (!string.IsNullOrEmpty(cfg.Label))
      {
        return cfg.Label;
      }
      return name;
    }

    public void SetLabel(byte address, string label)
    {
      this.EnsureAddress(address);

      var idx = this._configIndex[address];
      var cfg = this._configLines[idx];
      cfg.Label = label;
      cfg.RawDirty = true;
      this._configLines[idx] = cfg;
    }

    public FPGAOp GetGateOp(byte address)
    {
      this.AssertGateAddress(address);
      var idx = this._configIndex[address];
      if (idx == -1)
      {
        return FPGAOp.None;
      }
      return this._configLines[idx].GateOp;
    }

    public void SetGateOp(byte address, FPGAOp op)
    {
      this.EnsureAddress(address);
      this.AssertGateAddress(address);
      var idx = this._configIndex[address];

      var cfg = this._configLines[idx];
      cfg.RawDirty = true;
      cfg.GateOp = op;
      cfg.GateInput1Mode = ValueMode.Label;
      cfg.GateInput2Mode = ValueMode.Label;
      this._configLines[idx] = cfg;
    }

    public byte GetGateInput1(byte address)
    {
      this.AssertGateAddress(address);
      var idx = this._configIndex[address];
      if (idx == -1)
      {
        return 0;
      }
      return this._configLines[idx].GateInput1;
    }

    public byte GetGateInput2(byte address)
    {
      this.AssertGateAddress(address);
      var idx = this._configIndex[address];
      if (idx == -1)
      {
        return 0;
      }
      return this._configLines[idx].GateInput2;
    }

    public void SetGateInput1(byte address, byte inputAddress)
    {
      this.EnsureAddress(address);
      this.AssertGateAddress(address);
      var idx = this._configIndex[address];

      var cfg = this._configLines[idx];
      cfg.RawDirty = true;
      cfg.GateInput1 = inputAddress;
      cfg.GateInput1Mode = ValueMode.Label; // label will fallback to name if not available
      this._configLines[idx] = cfg;
    }

    public void SetGateInput2(byte address, byte inputAddress)
    {
      this.EnsureAddress(address);
      this.AssertGateAddress(address);
      var idx = this._configIndex[address];

      var cfg = this._configLines[idx];
      cfg.RawDirty = true;
      cfg.GateInput2 = inputAddress;
      cfg.GateInput2Mode = ValueMode.Label; // label will fallback to name if not available
      this._configLines[idx] = cfg;
    }

    public double GetLutValue(byte address)
    {
      this.AssertLutAddress(address);
      var idx = this._configIndex[address];
      if (idx == -1)
      {
        return 0;
      }
      return this._configLines[idx].LutValue;
    }

    public void SetLutValue(byte address, double value)
    {
      this.EnsureAddress(address);
      this.AssertLutAddress(address);
      var idx = this._configIndex[address];

      var cfg = this._configLines[idx];
      cfg.RawDirty = true;
      cfg.LutValue = value;
      cfg.LutRawValue = ""; // force restringify of value
      this._configLines[idx] = cfg;
    }

    private void EnsureAddress(byte address)
    {
      if (address >= 192)
      {
        throw new ArgumentOutOfRangeException();
      }
      var idx = this._configIndex[address];
      if (idx != -1)
      {
        // if it exists, force it to be valid.
        var existing = this._configLines[idx];
        existing.ForceValid();
        this._configLines[idx] = existing;
        return;
      }
      // make a new mostly empty config line for this address
      var cfg = new ConfigLine
      {
        RawDirty = true,
        IsValid = true,
        HasValidAddress = true,

        AddressMode = ValueMode.Name,
        Address = address,
      };
      idx = this._configLines.Count;
      this._configLines.Add(cfg);
      this._configIndex[address] = idx;
    }

    private void AssertGateAddress(byte address)
    {
      if (address < 64 || address > 127)
      {
        throw new ArgumentOutOfRangeException();
      }
    }

    private void AssertLutAddress(byte address)
    {
      if (address < 128 || address > 191)
      {
        throw new ArgumentOutOfRangeException();
      }
    }

    public string GetRaw()
    {
      var labels = new string[192];
      for (byte address = 0; address < 192; address++)
      {
        labels[address] = this.GetLabel(address);
      }
      var sb = new StringBuilder();
      for (var i = 0; i < this._configLines.Count; i++)
      {
        var cfg = this._configLines[i];
        cfg.RegenRaw(labels);
        if (i != 0)
        {
          sb.Append('\n');
        }
        sb.Append(cfg.RawLine);
      }
      return sb.ToString();
    }

    private enum ValueMode
    {
      Name,
      Decimal,
      Hex,
      Label
    }
    private struct ConfigLine
    {
      public string RawLine;

      // if this is blank or commented
      public bool IsComment;

      // if we need to regenerate raw line
      public bool RawDirty;

      // error states
      public bool IsValid;
      public bool HasValidAddress; // if we have a valid address and are not a duplicate, we can at least use the label
      public bool IsDuplicate;

      public ValueMode AddressMode;
      public byte Address;
      public string Label;

      public bool RawGate; // if op and inputs are contained in one hex value
      public string RawGateOp;
      public string RawGateInput1;
      public string RawGateInput2;
      public FPGAOp GateOp;
      public ValueMode GateInput1Mode;
      public byte GateInput1;
      public ValueMode GateInput2Mode;
      public byte GateInput2;

      public string LutRawValue;
      public double LutValue;

      public void ForceValid()
      {
        if (!this.HasValidAddress || this.IsDuplicate)
        {
          throw new InvalidOperationException();
        }
        if (this.IsValid && !this.RawGate)
        {
          // nothing to do
          return;
        }
        this.IsValid = true;
        this.RawDirty = true;

        if (this.Address < 64)
        {
          // input. nothing to do but regen raw
        }
        else if (this.Address < 128)
        {
          // gate. disable raw mode. force valid op. force valid inputs.
          this.RawGate = false;
          if (this.GateOp >= FPGAOps.Count)
          {
            this.GateOp = FPGAOp.None;
          }
          this.GateInput1Mode = ValueMode.Label;
          this.GateInput2Mode = ValueMode.Label;
          this.RawGateInput1 = "";
          this.RawGateInput2 = "";
          if (this.GateInput1 >= 192)
          {
            this.GateInput1 = 0;
          }
          if (this.GateInput2 >= 192)
          {
            this.GateInput2 = 0;
          }
        }
        else if (this.Address < 192)
        {
          // lut. regenerate raw value
          this.LutRawValue = "";
        }
      }

      private void CheckNameChange(string[] labels)
      {
        if (!this.IsValid || this.Address < 64 || this.Address > 127)
        {
          // only check for valid gates
          return;
        }

        if (this.GateInput1Mode == ValueMode.Label && this.RawGateInput1 != labels[this.GateInput1])
        {
          this.RawDirty = true;
        }
        if (this.GateInput2Mode == ValueMode.Label && this.RawGateInput2 != labels[this.GateInput2])
        {
          this.RawDirty = true;
        }
      }

      public void RegenRaw(string[] labels)
      {
        this.CheckNameChange(labels);
        if (!this.RawDirty)
        {
          return;
        }
        var sb = new StringBuilder();
        switch (this.AddressMode)
        {
          case ValueMode.Decimal:
            sb.AppendFormat("{0:D2}", this.Address);
            break;
          case ValueMode.Hex:
            sb.AppendFormat("${0:X2}", this.Address);
            break;
          default:
            sb.Append(FPGADef.GetName(this.Address));
            break;
        }
        if (!string.IsNullOrEmpty(this.Label))
        {
          sb.Append('=');
          sb.Append(this.Label);
        }

        if (this.Address < 64)
        {
          // input. nothing else to do
        }
        else if (this.Address < 128)
        {
          // gate.
          sb.Append(' ');
          if (this.RawGate)
          {
            if (string.IsNullOrEmpty(this.RawGateOp))
            {
              this.RawGateOp = $"${(int)this.GateOp | (this.GateInput1 << 8) | (this.GateInput2 << 16):X6}";
            }
            sb.Append(this.RawGateOp);
          }
          else
          {
            var info = FPGAOps.GetOpInfo(this.GateOp);
            sb.Append(info.Symbol);
            if (info.Operands >= 1)
            {
              sb.Append(' ');
              switch (this.GateInput1Mode)
              {
                case ValueMode.Name:
                  sb.Append(FPGADef.GetName(this.GateInput1));
                  break;
                case ValueMode.Decimal:
                  sb.AppendFormat("{0:D2}", this.GateInput1);
                  break;
                case ValueMode.Hex:
                  sb.AppendFormat("{0:X2}", this.GateInput1);
                  break;
                case ValueMode.Label:
                  sb.Append(labels[this.GateInput1]);
                  break;
              }
            }
            if (info.Operands >= 2)
            {
              sb.Append(' ');
              switch (this.GateInput2Mode)
              {
                case ValueMode.Name:
                  sb.Append(FPGADef.GetName(this.GateInput2));
                  break;
                case ValueMode.Decimal:
                  sb.AppendFormat("{0:D2}", this.GateInput2);
                  break;
                case ValueMode.Hex:
                  sb.AppendFormat("{0:X2}", this.GateInput2);
                  break;
                case ValueMode.Label:
                  sb.Append(labels[this.GateInput2]);
                  break;
              }
            }
          }
        }
        else if (this.Address < 192)
        {
          // lut.
          sb.Append(' ');
          if (string.IsNullOrEmpty(this.LutRawValue))
          {
            this.LutRawValue = $"{this.LutValue:G}";
          }
          sb.Append(this.LutRawValue);
        }

        this.RawLine = sb.ToString();
        this.RawDirty = false;
      }

      public void ResolveInputs(Dictionary<string, byte> labelToAddress)
      {
        if (this.IsComment || this.IsDuplicate || !this.IsValid || this.Address < 64 || this.Address > 127 || this.RawGate)
        {
          // only resolve if we are a valid non-raw gate
          return;
        }
        var info = FPGAOps.GetOpInfo(this.GateOp);
        if (info.Operands >= 1 && this.GateInput1Mode == ValueMode.Label)
        {
          if (!labelToAddress.TryGetValue(this.RawGateInput1, out this.GateInput1))
          {
            this.IsValid = false;
          }
        }
        if (info.Operands >= 2 && this.GateInput2Mode == ValueMode.Label)
        {
          if (!labelToAddress.TryGetValue(this.RawGateInput2, out this.GateInput2))
          {
            this.IsValid = false;
          }
        }
      }

      public static ConfigLine Parse(string line)
      {
        var cfg = new ConfigLine
        {
          RawLine = line,
          IsValid = true,
        };

        var parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || parts[0].StartsWith("#"))
        {
          cfg.IsComment = true;
          return cfg;
        }

        // try to parse address
        var fullApart = parts[0];
        var labelSep = fullApart.IndexOf('=');
        var apart = labelSep == -1 ? fullApart : fullApart[..labelSep];

        cfg.HasValidAddress = parseAddress(apart, out cfg.AddressMode, out cfg.Address);
        cfg.IsValid = cfg.HasValidAddress;
        if (labelSep != -1)
        {
          cfg.Label = fullApart[(labelSep + 1)..];
        }

        if (!cfg.IsValid)
        {
          return cfg;
        }

        if (cfg.Address < 64)
        {
          // input
          if (parts.Length > 1)
          {
            // input only has label
            cfg.IsValid = false;
          }
        }
        else if (cfg.Address < 128)
        {
          // gate
          if (parts.Length < 2)
          {
            cfg.IsValid = false;
          }
          else
          {
            cfg.RawGateOp = parts[1];
            if (parts.Length >= 3)
            {
              cfg.RawGateInput1 = parts[2];
            }
            if (parts.Length >= 4)
            {
              cfg.RawGateInput2 = parts[3];
            }

            if (cfg.RawGateOp.StartsWith("$"))
            {
              // full raw gate
              cfg.RawGate = true;
              cfg.IsValid = uint.TryParse(cfg.RawGateOp[1..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var rawGateVal);
              cfg.GateOp = (FPGAOp)(rawGateVal & 0xFF);
              cfg.GateInput1 = (byte)((rawGateVal >> 8) & 0xFF);
              cfg.GateInput2 = (byte)((rawGateVal >> 16) & 0xFF);
              if (cfg.GateOp >= FPGAOps.Count || cfg.GateInput1 >= 192 || cfg.GateInput2 >= 192 || (rawGateVal & 0xFFFFFF) != rawGateVal)
              {
                cfg.IsValid = false;
              }
              if (parts.Length > 2)
              {
                cfg.IsValid = false;
              }
            }
            else if (FPGAOps.SymbolToOp.TryGetValue(cfg.RawGateOp, out cfg.GateOp))
            {
              var info = FPGAOps.GetOpInfo(cfg.GateOp);
              if (info.Operands > 0)
              {
                var parsed = parseAddress(cfg.RawGateInput1, out cfg.GateInput1Mode, out cfg.GateInput1);
                if (!parsed || cfg.GateInput1Mode == ValueMode.Decimal)
                {
                  // if we failed to parse or it looked like a number, treat it as a label and check later
                  cfg.GateInput1Mode = ValueMode.Label;
                }
              }
              if (info.Operands == 2)
              {
                var parsed = parseAddress(cfg.RawGateInput2, out cfg.GateInput2Mode, out cfg.GateInput2);
                if (!parsed || cfg.GateInput2Mode == ValueMode.Decimal)
                {
                  // if we failed to parse or it looked like a number, treat it as a label and check later
                  cfg.GateInput2Mode = ValueMode.Label;
                }
              }

              if (parts.Length != info.Operands + 2)
              {
                cfg.IsValid = false;
              }
            }
            else
            {
              cfg.IsValid = false;
            }
          }
        }
        else if (cfg.Address < 192)
        {
          // lut
          if (parts.Length != 2)
          {
            // lut needs value
            cfg.IsValid = false;
          }
          else
          {
            cfg.LutRawValue = parts[1];
            cfg.IsValid = double.TryParse(parts[1], out cfg.LutValue);
          }
        }
        else
        {
          cfg.IsValid = false;
        }

        return cfg;
      }

      static bool parseAddress(string raw, out ValueMode mode, out byte address)
      {
        raw = raw ?? "";
        if (raw.StartsWith("$"))
        {
          mode = ValueMode.Hex;
          return byte.TryParse(raw[1..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out address);
        }
        else if (raw.StartsWith("in"))
        {
          mode = ValueMode.Name;
          var valid = byte.TryParse(raw[2..], out var inputNum);
          address = inputNum;
          return valid && inputNum < 64;
        }
        else if (raw.StartsWith("gate"))
        {
          mode = ValueMode.Name;
          var valid = byte.TryParse(raw[4..], out var gateNum);
          address = (byte)(gateNum + 64);
          return valid && gateNum < 64;
        }
        else if (raw.StartsWith("lut"))
        {
          mode = ValueMode.Name;
          var valid = byte.TryParse(raw[3..], out var lutNum);
          address = (byte)(lutNum + 128);
          return valid && lutNum < 64;
        }
        else
        {
          mode = ValueMode.Decimal;
          return byte.TryParse(raw, out address);
        }
      }
    }
  }
}