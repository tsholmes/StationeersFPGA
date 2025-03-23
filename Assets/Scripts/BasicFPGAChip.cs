using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts;
using Assets.Scripts.Objects.Motherboards;
using System;

namespace fpgamod
{
  public class BasicFPGAChip :
    Item,
    IMemory,
    IMemoryReadable,
    IMemoryWritable,
    ILogicStack,
    IInstructable
  {
    public const int STACK_SIZE = 64;

    private readonly LogicStack _stack = new LogicStack(STACK_SIZE);
    private readonly Gate[] _gates = new Gate[STACK_SIZE];
    private bool _inputDirty = true;
    private long _lastEvalIndex = -1;

    public int GetStackSize()
    {
      return STACK_SIZE;
    }

    public double ReadMemory(int address)
    {
      return this._stack[address];
    }

    public void ClearMemory()
    {
      this._stack.Clear();
      this.Recompile();
    }

    public void WriteMemory(int address, double value)
    {
      this._stack[address] = value;
      this.Recompile();
    }

    public LogicStack GetLogicStack()
    {
      return _stack;
    }

    public IEnumCollection GetInstructions()
    {
      return Instructions;
    }

    public string GetInstructionDescription(int i)
    {
      // TODO
      return "";
    }

    public void MarkInputDirty()
    {
      this._inputDirty = true;
    }

    public double ReadGateValue(int index, IFPGAInput input)
    {
      var evalIndex = this._lastEvalIndex;
      if (this._inputDirty)
      {
        evalIndex++;
        this._inputDirty = false;
        this._lastEvalIndex = evalIndex;
      }
      return this._gates[index].Eval(input, evalIndex);
    }

    private void Recompile()
    {
      this.MarkInputDirty();
      this._lastEvalIndex = -1;
      var parsed = new (byte, byte, byte)[STACK_SIZE];
      for (var i = 0; i < STACK_SIZE; i++)
      {
        parsed[i] = LogicStack.UnpackByteX2(ProgrammableChip.DoubleToLong(this._stack[i], false));
        this._gates[i] = null;
      }
      var visited = new bool[STACK_SIZE];
      var circular = new bool[STACK_SIZE];
      var inputGates = new Dictionary<int, Gate>();
      Func<int, Gate> getGate = index =>
      {
        if (index < STACK_SIZE)
        {
          return this._gates[index];
        }
        if (!inputGates.ContainsKey(index))
        {
          inputGates[index] = new InputGate(index - STACK_SIZE);
        }
        return inputGates[index];
      };
      Action<int> markCircular = index =>
      {
        circular[index] = true;
        this._gates[index] = new ConstantGate(double.NaN);
      };
      Func<int, bool> buildGate = null;
      buildGate = index =>
      {
        if (index >= STACK_SIZE)
        {
          return false;
        }
        if (this._gates[index] != null)
        {
          return circular[index];
        }
        if (visited[index])
        {
          markCircular(index);
          return true;
        }
        visited[index] = true;
        var (op, g0, g1) = parsed[index];
        var inst = (Instruction)op;
        Gate gate0 = null, gate1 = null;
        if (inst >= Instruction.Ceil && inst < Instruction.Add)
        {
          if (buildGate(g0))
          {
            markCircular(index);
            return true;
          }
          gate0 = getGate(g0);
        }
        if (inst >= Instruction.Ceil && inst <= Instruction.BitSrl)
        {
          if (buildGate(g1))
          {
            markCircular(index);
            return true;
          }
          gate1 = getGate(g1);
        }
        this._gates[index] = makeGate(inst, g0, gate0, gate1);
        visited[index] = false;
        return false;
      };
      for (var i = 0; i < STACK_SIZE; i++)
      {
        buildGate(i);
      }
    }

    private Gate makeGate(Instruction inst, byte g0, Gate gate0, Gate gate1)
    {
      if (inst == Instruction.Constant)
      {
        return new ConstantGate(this._stack[g0]);
      }
      if (inst >= Instruction.Ceil && inst < Instruction.Add)
      {
        return new UnaryGate(unaryOps[inst], gate0);
      }
      if (inst >= Instruction.Add && inst <= Instruction.BitSrl)
      {
        return new BinaryGate(binaryOps[inst], gate0, gate1);
      }
      return new ConstantGate(double.NaN); // invalid gates become nan
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

    public static readonly EnumCollection<Instruction, byte> Instructions = new EnumCollection<Instruction, byte>();
    // gate instruction value (by byte low-high): [op] [g0] [g1] [unused]x5
    // g0 and g1 point to other gates when <64, and input pin+64 when >=64
    public enum Instruction : byte
    {
      // read ops
      Constant, // interpret g0 as double

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

      // dummy ops
      ConstantValue = 255, // entire value is constant double. referenced by Constant op
    }


    private abstract class Gate
    {
      public double lastValue;
      public long lastEvalIndex = -1;

      public double Eval(IFPGAInput input, long evalIndex)
      {
        if (evalIndex == this.lastEvalIndex)
        {
          return this.lastValue;
        }
        this.lastValue = this.Op(input, evalIndex);
        this.lastEvalIndex = evalIndex;
        return this.lastValue;
      }

      public abstract double Op(IFPGAInput input, long evalIndex);
    }

    private class ConstantGate : Gate
    {
      private readonly double value;
      public ConstantGate(double value) { this.value = value; }

      public override double Op(IFPGAInput input, long evalIndex)
      {
        return value;
      }
    }

    private class InputGate : Gate
    {
      private readonly int index;
      public InputGate(int index) { this.index = index; }

      public override double Op(IFPGAInput input, long evalIndex)
      {
        return input.GetFPGAInputPin(this.index);
      }
    }

    private class UnaryGate : Gate
    {
      private readonly Func<double, double> op;
      private readonly Gate g0;
      public UnaryGate(Func<double, double> op, Gate g0)
      {
        this.op = op;
        this.g0 = g0;
      }

      public override double Op(IFPGAInput input, long evalIndex)
      {
        return this.op(this.g0.Eval(input, evalIndex));
      }
    }

    private class BinaryGate : Gate
    {
      private readonly Func<double, double, double> op;
      private readonly Gate g0;
      private readonly Gate g1;

      public BinaryGate(Func<double, double, double> op, Gate g0, Gate g1)
      {
        this.op = op;
        this.g0 = g0;
        this.g1 = g1;
      }

      public override double Op(IFPGAInput input, long evalIndex)
      {
        return this.op(this.g0.Eval(input, evalIndex), this.g1.Eval(input, evalIndex));
      }
    }
  }
}
