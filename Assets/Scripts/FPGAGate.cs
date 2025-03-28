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

    public static void Compile(FPGADef def, FPGAGate[] gates)
    {
      var parsed = new (FPGAOp, byte, byte)[FPGADef.GateCount];
      for (var i = 0; i < FPGADef.GateCount; i++)
      {
        parsed[i] = def.GetGateParts((byte)(i + FPGADef.GateOffset));
        gates[i] = null;
      }
      var building = new bool[FPGADef.GateCount];
      var circular = new bool[FPGADef.GateCount];

      var inputGates = new FPGAGate[FPGADef.InputCount];
      for (var i = 0; i < FPGADef.InputCount; i++)
      {
        inputGates[i] = new InputGate(i);
      }
      var lutGates = new FPGAGate[FPGADef.LutCount];
      for (var i = 0; i < FPGADef.LutCount; i++)
      {
        lutGates[i] = new ConstantGate(def.GetLutValue((byte)(i + FPGADef.LutOffset)));
      }
      var errGate = new ConstantGate(double.NaN);
      Func<byte, FPGAGate> getGate = address =>
      {
        if (FPGADef.IsIOAddress(address))
        {
          return inputGates[address - FPGADef.InputOffset];
        }
        if (FPGADef.IsGateAddress(address))
        {
          return gates[address - FPGADef.GateOffset];
        }
        if (FPGADef.IsLutAddress(address))
        {
          return lutGates[address - FPGADef.LutOffset];
        }
        return errGate;
      };
      Action<byte> markCircular = address =>
      {
        if (!FPGADef.IsGateAddress(address))
        {
          throw new Exception("invalid circular mark");
        }
        circular[address - FPGADef.GateOffset] = true;
        gates[address - FPGADef.GateOffset] = errGate;
      };
      Func<byte, bool> buildGate = null;
      buildGate = address =>
      {
        if (!FPGADef.IsGateAddress(address))
        {
          return false;
        }
        var index = address - FPGADef.GateOffset;
        if (gates[index] != null)
        {
          return circular[index];
        }
        if (building[index])
        {
          markCircular(address);
          return true;
        }
        building[index] = true;
        var (op, g0, g1) = parsed[index];
        var info = FPGAOps.GetOpInfo(op);
        FPGAGate gate0 = null, gate1 = null;
        if (info.Operands >= 1)
        { // has g0
          if (buildGate(g0))
          {
            markCircular(address);
            return true;
          }
          gate0 = getGate(g0);
        }
        if (info.Operands >= 2)
        { // has g1
          if (buildGate(g1))
          {
            markCircular(address);
            return true;
          }
          gate1 = getGate(g1);
        }
        gates[index] = MakeGate(op, gate0, gate1);
        building[index] = false;
        return false;
      }
        ;
      for (var i = 0; i < FPGADef.GateCount; i++)
      {
        buildGate((byte)(i + FPGADef.GateOffset));
      }
    }

    private static FPGAGate MakeGate(FPGAOp op, FPGAGate g0, FPGAGate g1)
    {
      var info = FPGAOps.GetOpInfo(op);
      switch (info.Operands)
      {
        case 0:
          return new ConstantGate(info.ConstantOp());
        case 1:
          return new UnaryGate(info.UnaryOp, g0);
        case 2:
          return new BinaryGate(info.BinaryOp, g0, g1);
      }
      throw new Exception("invalid operand count");
    }

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
