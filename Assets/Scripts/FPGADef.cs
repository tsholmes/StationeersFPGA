using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Util;
using UnityEngine;

namespace fpgamod
{
  public class FPGADef
  {
    public const int InputCount = 64;
    public const int InputOffset = 0;
    public const int GateCount = 64;
    public const int GateOffset = InputCount;
    public const int LutCount = 64;
    public const int LutOffset = InputCount + GateCount;
    public const int AddressCount = InputCount + GateCount + LutCount;
    public static string[] InputNames = new string[InputCount];
    public static string[] GateNames = new string[GateCount];
    public static string[] LutNames = new string[LutCount];

    static FPGADef()
    {
      for (var i = 0; i < InputCount; i++)
      {
        InputNames[i] = $"in{i:D2}";
      }
      for (var i = 0; i < GateCount; i++)
      {
        GateNames[i] = $"gate{i:D2}";
      }
      for (var i = 0; i < LutCount; i++)
      {
        LutNames[i] = $"lut{i:D2}";
      }
    }

    public static string GetName(byte address)
    {
      if (IsIOAddress(address))
      {
        return InputNames[address - InputOffset];
      }
      if (IsGateAddress(address))
      {
        return GateNames[address - GateOffset];
      }
      if (IsLutAddress(address))
      {
        return LutNames[address - LutOffset];
      }
      return "invalid";
    }

    private List<ConfigLine> _configLines = new List<ConfigLine>();
    private int[] _configIndex = new int[AddressCount];

    public static FPGADef NewEmpty()
    {
      return new FPGADef();
    }

    public static FPGADef Parse(string raw)
    {
      if (string.IsNullOrWhiteSpace(raw))
      {
        return NewEmpty();
      }
      var lines = raw.Split('\n');
      var cfgLines = new List<ConfigLine>();
      foreach (var line in lines)
      {
        cfgLines.Add(ConfigLine.Parse(line));
      }
      return new FPGADef(cfgLines);
    }

    public static bool IsIOAddress(byte address)
    {
      return address >= InputOffset && address < InputOffset + InputCount;
    }

    public static bool IsGateAddress(byte address)
    {
      return address >= GateOffset && address < GateOffset + GateCount;
    }

    public static bool IsLutAddress(byte address)
    {
      return address >= LutOffset && address < LutOffset + LutCount;
    }

    public static bool IsValidAddress(byte address)
    {
      return IsIOAddress(address) || IsGateAddress(address) | IsLutAddress(address);
    }

    private static void AssertGateAddress(byte address)
    {
      if (!IsGateAddress(address))
      {
        throw new ArgumentOutOfRangeException();
      }
    }

    private static void AssertLutAddress(byte address)
    {
      if (!IsLutAddress(address))
      {
        throw new ArgumentOutOfRangeException();
      }
    }

    private static void AssertValidAddress(byte address)
    {
      if (!IsValidAddress(address))
      {
        throw new IndexOutOfRangeException();
      }
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

    public bool HasConfig(byte address)
    {
      AssertValidAddress(address);
      return this._configIndex[address] != -1;
    }

    public int GetConfigLineCount()
    {
      return this._configLines.Count;
    }

    public Error GetConfigLineError(int index)
    {
      var cfg = this._configLines[index];
      if (cfg.Error != null) {
        return cfg.Error;
      }
      if (cfg.IsDuplicate) {
        return Error.Duplicate;
      }
      return null;
    }

    public string GetLabel(byte address, bool nameFallback = true)
    {
      AssertValidAddress(address);
      var name = nameFallback ? GetName(address) : "";
      var idx = this._configIndex[address];

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
      AssertGateAddress(address);
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
      AssertGateAddress(address);
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
      AssertGateAddress(address);
      var idx = this._configIndex[address];
      if (idx == -1)
      {
        return 0;
      }
      return this._configLines[idx].GateInput1;
    }

    public byte GetGateInput2(byte address)
    {
      AssertGateAddress(address);
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
      AssertGateAddress(address);
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
      AssertGateAddress(address);
      var idx = this._configIndex[address];

      var cfg = this._configLines[idx];
      cfg.RawDirty = true;
      cfg.GateInput2 = inputAddress;
      cfg.GateInput2Mode = ValueMode.Label; // label will fallback to name if not available
      this._configLines[idx] = cfg;
    }

    public double GetLutValue(byte address)
    {
      AssertLutAddress(address);
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
      AssertLutAddress(address);
      var idx = this._configIndex[address];

      var cfg = this._configLines[idx];
      cfg.RawDirty = true;
      cfg.LutValue = value;
      cfg.LutRawValue = ""; // force restringify of value
      this._configLines[idx] = cfg;
    }

    private void EnsureAddress(byte address)
    {
      AssertValidAddress(address);
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
        HasValidAddress = true,

        AddressMode = ValueMode.Name,
        Address = address,
      };
      idx = this._configLines.Count;
      this._configLines.Add(cfg);
      this._configIndex[address] = idx;
    }

    public string GetRaw()
    {
      var labels = new string[AddressCount];
      for (byte address = 0; address < AddressCount; address++)
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

    public double ReadRawValue(byte address)
    {
      if (IsGateAddress(address))
      {
        var idx = this._configIndex[address];
        if (idx == -1)
        {
          return 0f;
        }
        var cfg = this._configLines[address];
        if (cfg.RawGate && !string.IsNullOrEmpty(cfg.RawGateOp))
        {
          long.TryParse(cfg.RawGateOp[1..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out long result);
          return ProgrammableChip.LongToDouble(result);
        }
        return ProgrammableChip.LongToDouble((int)cfg.GateOp | (cfg.GateInput1 << 8) | (cfg.GateInput2 << 16));
      }
      else if (IsLutAddress(address))
      {
        var idx = this._configIndex[address];
        if (idx == -1)
        {
          return 0f;
        }
        var cfg = this._configLines[address];
        return cfg.LutValue;
      }
      else
      {
        throw new ArgumentOutOfRangeException();
      }
    }

    public void SetGateRaw(byte address, long value)
    {
      this.EnsureAddress(address);
      AssertGateAddress(address);

      var idx = this._configIndex[address];

      var cfg = this._configLines[idx];
      cfg.RawGate = true;
      cfg.RawGateOp = $"${value:X6}";
      cfg.GateOp = (FPGAOp)(value & 0xFF);
      cfg.RawGateInput1 = "";
      cfg.RawGateInput2 = "";
      cfg.GateInput1 = (byte)((value >> 8) & 0xFF);
      cfg.GateInput2 = (byte)((value >> 16) & 0xFF);
      this._configLines[idx] = cfg;
    }

    public (FPGAOp, byte, byte) GetGateParts(byte address)
    {
      AssertGateAddress(address);
      var idx = this._configIndex[address];
      if (idx == -1)
      {
        return (FPGAOp.None, 0, 0);
      }
      var cfg = this._configLines[idx];
      return (cfg.GateOp, cfg.GateInput1, cfg.GateInput2);
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
      public Error Error;
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

      public bool IsValid => this.Error == null || this.Error.IsWarning;

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
        this.RawDirty = true;
        this.Error = null;

        if (IsIOAddress(this.Address))
        {
          // input. nothing to do but regen raw
        }
        else if (IsGateAddress(this.Address))
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
        else if (IsLutAddress(this.Address))
        {
          // lut. regenerate raw value
          this.LutRawValue = "";
        }
      }

      private void CheckNameChange(string[] labels)
      {
        if (!this.IsValid || !IsGateAddress(this.Address))
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

        if (IsIOAddress(this.Address))
        {
          // input. nothing else to do
        }
        else if (IsGateAddress(this.Address))
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
        else if (IsLutAddress(this.Address))
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
        if (this.IsComment || this.IsDuplicate || !this.IsValid || !IsGateAddress(this.Address) || this.RawGate)
        {
          // only resolve if we are a valid non-raw gate
          return;
        }
        var info = FPGAOps.GetOpInfo(this.GateOp);
        if (info.Operands >= 1 && this.GateInput1Mode == ValueMode.Label)
        {
          if (!labelToAddress.TryGetValue(this.RawGateInput1, out this.GateInput1))
          {
            this.Error = Error.UnknownLabel;
          }
        }
        if (info.Operands >= 2 && this.GateInput2Mode == ValueMode.Label)
        {
          if (!labelToAddress.TryGetValue(this.RawGateInput2, out this.GateInput2))
          {
            this.Error = Error.UnknownLabel;
          }
        }
      }

      public static ConfigLine Parse(string line)
      {
        var cfg = new ConfigLine
        {
          RawLine = line,
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

        cfg.HasValidAddress = ParseAddress(apart, out cfg.AddressMode, out cfg.Address);
        if (!cfg.HasValidAddress) {
          cfg.Error = Error.InvalidAddress;
        }
        if (labelSep != -1)
        {
          cfg.Label = fullApart[(labelSep + 1)..];
        }

        if (!cfg.IsValid)
        {
          return cfg;
        }

        if (IsIOAddress(cfg.Address))
        {
          // input
          if (parts.Length > 1)
          {
            // input only has label
            cfg.Error = Error.TooManyValues;
          }
        }
        else if (IsGateAddress(cfg.Address))
        {
          // gate
          if (parts.Length < 2)
          {
            cfg.Error = Error.MissingGateOp;
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
              var rawValid = uint.TryParse(cfg.RawGateOp[1..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var rawGateVal);
              if (!rawValid) {
                cfg.Error = Error.InvalidRawGate;
              }
              cfg.GateOp = (FPGAOp)(rawGateVal & 0xFF);
              cfg.GateInput1 = (byte)((rawGateVal >> 8) & 0xFF);
              cfg.GateInput2 = (byte)((rawGateVal >> 16) & 0xFF);
              if (cfg.GateOp >= FPGAOps.Count || !IsValidAddress(cfg.GateInput1) || !IsValidAddress(cfg.GateInput2) || (rawGateVal & 0xFFFFFF) != rawGateVal)
              {
                cfg.Error = Error.InvalidRawGate;
              }
              if (parts.Length > 2)
              {
                cfg.Error = Error.TooManyValues;
              }
            }
            else if (FPGAOps.SymbolToOp.TryGetValue(cfg.RawGateOp, out cfg.GateOp))
            {
              var info = FPGAOps.GetOpInfo(cfg.GateOp);
              if (info.Operands >= 1)
              {
                var parsed = ParseAddress(cfg.RawGateInput1, out cfg.GateInput1Mode, out cfg.GateInput1);
                if (!parsed || cfg.GateInput1Mode == ValueMode.Decimal)
                {
                  // if we failed to parse or it looked like a number, treat it as a label and check later
                  cfg.GateInput1Mode = ValueMode.Label;
                }
              }
              if (info.Operands >= 2)
              {
                var parsed = ParseAddress(cfg.RawGateInput2, out cfg.GateInput2Mode, out cfg.GateInput2);
                if (!parsed || cfg.GateInput2Mode == ValueMode.Decimal)
                {
                  // if we failed to parse or it looked like a number, treat it as a label and check later
                  cfg.GateInput2Mode = ValueMode.Label;
                }
              }

              if (parts.Length < info.Operands + 2) {
                cfg.Error = Error.MissingGateInput;
              } else if (parts.Length > info.Operands + 2) {
                cfg.Error = Error.TooManyValues;
              }
            }
            else
            {
              cfg.Error = Error.InvalidGateOp;
            }
          }
        }
        else if (IsLutAddress(cfg.Address))
        {
          // lut
          if (parts.Length >= 2)
          {
            cfg.LutRawValue = parts[1];
            if (!double.TryParse(parts[1], out cfg.LutValue)) {
              cfg.Error = Error.InvalidLutValue;
            }
          }
          if (parts.Length < 2)
          {
            // lut needs value
            cfg.Error = Error.MissingLutValue;
          } else if (parts.Length > 2) {
            cfg.Error = Error.TooManyValues;
          }
        }
        else
        {
          cfg.Error = Error.InvalidAddress;
        }

        return cfg;
      }

      static bool ParseAddress(string raw, out ValueMode mode, out byte address)
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
          return valid && inputNum < InputCount;
        }
        else if (raw.StartsWith("gate"))
        {
          mode = ValueMode.Name;
          var valid = byte.TryParse(raw[4..], out var gateNum);
          address = (byte)(gateNum + GateOffset);
          return valid && gateNum < GateCount;
        }
        else if (raw.StartsWith("lut"))
        {
          mode = ValueMode.Name;
          var valid = byte.TryParse(raw[3..], out var lutNum);
          address = (byte)(lutNum + LutOffset);
          return valid && lutNum < LutCount;
        }
        else
        {
          mode = ValueMode.Decimal;
          return byte.TryParse(raw, out address);
        }
      }
    }

    public class Error
    {
      public static Error Duplicate = new("Duplicate definition. Will be ignored", true);
      public static Error UnknownLabel = new("Unknown label");
      public static Error InvalidAddress = new("Invalid address");
      public static Error TooManyValues = new("Too many values", true);
      public static Error MissingGateOp = new("Missing gate op");
      public static Error InvalidRawGate = new("Invalid raw gate");
      public static Error MissingGateInput = new("Missing gate input");
      public static Error InvalidGateOp = new("Invalid gate op");
      public static Error InvalidLutValue = new("Invalid lookup table value");
      public static Error MissingLutValue = new("Invalid lookup table value");

      public readonly string Message;
      public readonly bool IsWarning;

      private Error(string message, bool warning = false)
      {
        this.Message = message;
        this.IsWarning = warning;
      }
    }
  }
}