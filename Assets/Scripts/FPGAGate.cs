using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Objects.Electrical;
using UnityEngine;

namespace fpgamod
{
  public abstract class FPGAGate
  {
    private double _lastValue;
    private long _lastModCount = -1;

    public double Eval(IFPGAInput input)
    {
      var modCount = input.GetFPGAInputModCount();
      if (modCount != this._lastModCount)
      {
        this._lastValue = this.Op(input);
        this._lastModCount = modCount;
      }
      return this._lastValue;
    }

    public abstract double Op(IFPGAInput input);

    public static void Compile(LogicStack stack, MemoryMapping mapping, FPGAGate[] gates)
    {
      if (stack.Size != mapping.GateCount + mapping.LookupCount)
      {
        throw new Exception("stack size mismatch");
      }
      var parsed = new (byte, byte, byte)[mapping.GateCount];
      for (var i = 0; i < mapping.GateCount; i++)
      {
        parsed[i] = LogicStack.UnpackByteX2(ProgrammableChip.DoubleToLong(stack[i], false));
        gates[i] = null;
      }
      var building = new bool[mapping.GateCount];
      var circular = new bool[mapping.GateCount];

      var inputGates = new FPGAGate[mapping.InputCount];
      for (var i = 0; i < mapping.InputCount; i++)
      {
        inputGates[i] = new InputGate(i);
      }
      var lutGates = new FPGAGate[mapping.LookupCount];
      for (var i = 0; i < mapping.LookupCount; i++)
      {
        lutGates[i] = new ConstantGate(stack[i + mapping.GateCount]);
      }
      var errGate = new ConstantGate(double.NaN);
      Func<Address, FPGAGate> getGate = address =>
      {
        switch (address.Section)
        {
          case AddressSection.IO:
            return inputGates[address.Offset];
          case AddressSection.Gate:
            return gates[address.Offset];
          case AddressSection.LUT:
            return lutGates[address.Offset];
          default:
            return errGate;
        }
      };
      Action<Address> markCircular = address =>
      {
        if (address.Section != AddressSection.Gate)
        {
          throw new Exception("invalid circular mark");
        }
        circular[address.Offset] = true;
        gates[address.Offset] = errGate;
      };
      Func<Address, bool> buildGate = null;
      buildGate = address =>
      {
        if (address.Section != AddressSection.Gate)
        {
          return false;
        }
        if (gates[address.Offset] != null)
        {
          return circular[address.Offset];
        }
        if (building[address.Offset])
        {
          markCircular(address);
          return true;
        }
        building[address.Offset] = true;
        var (op, g0, g1) = parsed[address.Offset];
        var inst = (Instruction)op;
        var a0 = mapping.LookupRead(g0);
        var a1 = mapping.LookupRead(g1);
        FPGAGate gate0 = null, gate1 = null;
        if (InstructionIsValid(inst))
        { // has g0
          if (buildGate(a0))
          {
            markCircular(address);
            return true;
          }
          gate0 = getGate(a0);
        }
        if (InstructionIsBinary(inst))
        { // has g1
          if (buildGate(a1))
          {
            markCircular(address);
            return true;
          }
          gate1 = getGate(a1);
        }
        gates[address.Offset] = MakeGate(inst, gate0, gate1, errGate);
        building[address.Offset] = false;
        return false;
      }
        ;
      for (var i = 0; i < mapping.GateCount; i++)
      {
        buildGate(new Address(AddressSection.Gate, i));
      }
    }

    public struct MemoryMapping
    {
      public readonly int InputCount;
      public readonly int GateCount;
      public readonly int LookupCount;

      public MemoryMapping(int InputCount, int GateCount, int LookupCount)
      {
        this.InputCount = InputCount;
        this.GateCount = GateCount;
        this.LookupCount = LookupCount;
      }

      public int GateOffset => Math.Max(this.InputCount, this.GateCount);
      public int LookupOffset => this.GateOffset + this.GateCount;
      public int TotalSize => this.LookupOffset + this.LookupCount;

      public Address LookupRead(int address) {
        return this.Lookup(address, true);
      }

      public Address LookupWrite(int address) {
        return this.Lookup(address, false);
      }

      private Address Lookup(int address, bool read)
      {
        if (address < 0)
        {
          return new Address(AddressSection.Invalid, 0);
        }
        if (address < this.GateOffset)
        {
          if (!read && address >= this.InputCount) { // write input values
            return new Address(AddressSection.Invalid, 0);
          }
          if (read && address >= this.GateCount) { // read gate outputs
            return new Address(AddressSection.Invalid, 0);
          }
          return new Address(AddressSection.IO, address);
        }
        if (address < this.GateOffset + this.GateCount)
        {
          return new Address(AddressSection.Gate, address - this.GateOffset);
        }
        if (address < this.LookupOffset + this.LookupCount)
        {
          return new Address(AddressSection.LUT, address - this.LookupOffset);
        }
        return new Address(AddressSection.Invalid, 0);
      }
    }

    public enum AddressSection
    {
      Invalid,
      IO,
      Gate,
      LUT
    }
    public struct Address
    {
      public readonly AddressSection Section;
      public readonly int Offset;

      public Address(AddressSection Section, int Offset) { this.Section = Section; this.Offset = Offset; }
    }

    private static FPGAGate MakeGate(Instruction inst, FPGAGate g0, FPGAGate g1, FPGAGate err)
    {
      if (!InstructionIsValid(inst))
      {
        return err;
      }
      if (InstructionIsBinary(inst))
      {
        return new BinaryGate(binaryOps[inst], g0, g1);
      }
      else
      {
        return new UnaryGate(unaryOps[inst], g0);
      }
    }

    private static bool InstructionIsValid(Instruction inst)
    {
      return inst >= Instruction.Ceil && inst <= Instruction.BitSrl;
    }

    private static bool InstructionIsBinary(Instruction inst)
    {
      return inst >= Instruction.Add && inst <= Instruction.BitSrl;
    }

    public static readonly EnumCollection<Instruction, byte> Instructions = new EnumCollection<Instruction, byte>(false);
    // gate instruction value (by byte low-high): [op] [g0] [g1] [unused]x5
    // g0/g1 = [0-63] input pin, [64-127] gate output value, [128-191] lookup table
    public enum Instruction : byte
    {
      None,

      // unary ops perform op on g0
      Ceil,
      Floor,
      Abs,
      Log,
      Exp,
      Round,
      Sqrt,
      Sin,
      Cos,
      Tan,
      Asin,
      Acos,
      Atan,
      LogicNot,
      BitNot,

      // binary ops perform g0[op]g1
      // binary math ops
      Add,
      Subtract,
      Multiply,
      Divide,
      Mod,
      Atan2, // y=g0, x=g1
      Pow,
      // binary compare ops
      Less,
      LessEquals,
      Equals,
      GreaterEquals,
      Greater,
      NotEquals,
      Min,
      Max,
      // binary boolean ops
      LogicAnd,
      LogicOr,
      LogicXor,
      LogicNand,
      LogicNor,
      LogicXnor,
      // binary bit ops
      BitAnd,
      BitOr,
      BitXor,
      BitNand,
      BitNor,
      BitXnor,
      BitSla,
      BitSll,
      BitSra,
      BitSrl,
    }

    private static Dictionary<Instruction, Func<double, double>> unaryOps = new()
    {
      [Instruction.Ceil] = Math.Ceiling,
      [Instruction.Floor] = Math.Floor,
      [Instruction.Abs] = Math.Abs,
      [Instruction.Log] = Math.Log,
      [Instruction.Exp] = Math.Exp,
      [Instruction.Round] = Math.Round,
      [Instruction.Sqrt] = Math.Sqrt,
      [Instruction.Sin] = Math.Sin,
      [Instruction.Cos] = Math.Cos,
      [Instruction.Tan] = Math.Tan,
      [Instruction.Asin] = Math.Asin,
      [Instruction.Acos] = Math.Acos,
      [Instruction.Atan] = Math.Atan,
      [Instruction.LogicNot] = val => val == 0 ? 1 : 0,
      [Instruction.BitNot] = val => ProgrammableChip.LongToDouble(~ProgrammableChip.DoubleToLong(val, true)),
    };

    private static Func<double, double, double> makeBinaryBitOp(Func<long, long, long> op, bool firstSigned = true)
    {
      return (val1, val2) => ProgrammableChip.LongToDouble(op(ProgrammableChip.DoubleToLong(val1, firstSigned), ProgrammableChip.DoubleToLong(val2, true)));
    }

    private static Dictionary<Instruction, Func<double, double, double>> binaryOps = new()
    {
      [Instruction.Add] = (val1, val2) => val1 + val2,
      [Instruction.Subtract] = (val1, val2) => val1 - val2,
      [Instruction.Multiply] = (val1, val2) => val1 * val2,
      [Instruction.Divide] = (val1, val2) => val1 / val2,
      [Instruction.Mod] = (val1, val2) => { var res = val1 % val2; return res < 0 ? res + val2 : res; },
      [Instruction.Atan2] = Math.Atan2, // y=g0, x=g1
      [Instruction.Pow] = Math.Pow,
      [Instruction.Less] = (val1, val2) => val1 < val2 ? 1 : 0,
      [Instruction.LessEquals] = (val1, val2) => val1 <= val2 ? 1 : 0,
      [Instruction.Equals] = (val1, val2) => val1 == val2 ? 1 : 0,
      [Instruction.GreaterEquals] = (val1, val2) => val1 >= val2 ? 1 : 0,
      [Instruction.Greater] = (val1, val2) => val1 > val2 ? 1 : 0,
      [Instruction.NotEquals] = (val1, val2) => val1 != val2 ? 1 : 0,
      [Instruction.Min] = (val1, val2) => val1 < val2 ? val1 : val2,
      [Instruction.Max] = (val1, val2) => val1 > val2 ? val1 : val2,
      [Instruction.LogicAnd] = (val1, val2) => val1 != 0 && val2 != 0 ? 1 : 0,
      [Instruction.LogicOr] = (val1, val2) => val1 != 0 || val2 != 0 ? 1 : 0,
      [Instruction.LogicXor] = (val1, val2) => (val1 != 0) != (val2 != 0) ? 1 : 0,
      [Instruction.LogicNand] = (val1, val2) => val1 == 0 || val2 == 0 ? 1 : 0,
      [Instruction.LogicNor] = (val1, val2) => val1 == 0 && val2 == 0 ? 1 : 0,
      [Instruction.LogicXnor] = (val1, val2) => (val1 == 0) == (val2 == 0) ? 1 : 0,
      [Instruction.BitAnd] = makeBinaryBitOp((val1, val2) => val1 & val2),
      [Instruction.BitOr] = makeBinaryBitOp((val1, val2) => val1 | val2),
      [Instruction.BitXor] = makeBinaryBitOp((val1, val2) => val1 ^ val2),
      [Instruction.BitNand] = makeBinaryBitOp((val1, val2) => ~(val1 & val2)),
      [Instruction.BitNor] = makeBinaryBitOp((val1, val2) => ~(val1 | val2)),
      [Instruction.BitXnor] = makeBinaryBitOp((val1, val2) => ~(val1 ^ val2)),
      [Instruction.BitSla] = makeBinaryBitOp((val1, val2) => val1 << (int)val2),
      [Instruction.BitSll] = makeBinaryBitOp((val1, val2) => val1 << (int)val2),
      [Instruction.BitSra] = makeBinaryBitOp((val1, val2) => val1 >> (int)val2),
      [Instruction.BitSrl] = makeBinaryBitOp((val1, val2) => val1 >> (int)val2, firstSigned: false),
    };



    private class ConstantGate : FPGAGate
    {
      private readonly double value;
      public ConstantGate(double value) { this.value = value; }

      public override double Op(IFPGAInput input)
      {
        return value;
      }
    }

    private class InputGate : FPGAGate
    {
      private readonly int index;
      public InputGate(int index) { this.index = index; }

      public override double Op(IFPGAInput input)
      {
        return input.GetFPGAInputPin(this.index);
      }
    }

    private class UnaryGate : FPGAGate
    {
      private readonly Func<double, double> op;
      private readonly FPGAGate g0;
      public UnaryGate(Func<double, double> op, FPGAGate g0)
      {
        this.op = op;
        this.g0 = g0;
      }

      public override double Op(IFPGAInput input)
      {
        return this.op(this.g0.Eval(input));
      }
    }

    private class BinaryGate : FPGAGate
    {
      private readonly Func<double, double, double> op;
      private readonly FPGAGate g0;
      private readonly FPGAGate g1;

      public BinaryGate(Func<double, double, double> op, FPGAGate g0, FPGAGate g1)
      {
        this.op = op;
        this.g0 = g0;
        this.g1 = g1;
      }

      public override double Op(IFPGAInput input)
      {
        return this.op(this.g0.Eval(input), this.g1.Eval(input));
      }
    }
  }
}
