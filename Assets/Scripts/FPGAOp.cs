using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace fpgamod
{
  // gate instruction value (by byte low-high): [op] [g0] [g1] [unused]x5
  // g0/g1 = [0-63] input pin, [64-127] gate output value, [128-191] lookup table
  public enum FPGAOp
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
    // binary bit ops
    BitAnd,
    BitOr,
    BitXor,
    BitNand,
    BitNor,
    BitSla,
    BitSll,
    BitSra,
    BitSrl,
  }

  public struct FPGAOpInfo
  {
    public string Symbol;
    public string Hint;
    public int Operands;
  }

  public static class FPGAOps
  {
    public const FPGAOp Count = FPGAOp.BitSrl + 1;
    public static FPGAOpInfo GetOpInfo(FPGAOp op)
    {
      return InfoDict.GetValueOrDefault(op, InvalidOp);
    }
    public static FPGAOpInfo InvalidOp = new FPGAOpInfo { Symbol = "invalid", Hint = "" };
    public static readonly Dictionary<FPGAOp, FPGAOpInfo> InfoDict = new()
    {
      [FPGAOp.None] = new FPGAOpInfo { Symbol = "none", Hint = "" },
      [FPGAOp.Ceil] = new FPGAOpInfo { Symbol = "ceil", Hint = "round up", Operands = 1 },
      [FPGAOp.Floor] = new FPGAOpInfo { Symbol = "floor", Hint = "round down", Operands = 1 },
      [FPGAOp.Abs] = new FPGAOpInfo { Symbol = "abs", Hint = "absolute value", Operands = 1 },
      [FPGAOp.Log] = new FPGAOpInfo { Symbol = "log", Hint = "natural logarithm base e", Operands = 1 },
      [FPGAOp.Exp] = new FPGAOpInfo { Symbol = "exp", Hint = "exponential function e^x", Operands = 1 },
      [FPGAOp.Round] = new FPGAOpInfo { Symbol = "round", Hint = "round to nearest", Operands = 1 },
      [FPGAOp.Sqrt] = new FPGAOpInfo { Symbol = "sqrt", Hint = "square root", Operands = 1 },
      [FPGAOp.Sin] = new FPGAOpInfo { Symbol = "sin", Hint = "trig sine", Operands = 1 },
      [FPGAOp.Cos] = new FPGAOpInfo { Symbol = "cos", Hint = "trig cosine", Operands = 1 },
      [FPGAOp.Tan] = new FPGAOpInfo { Symbol = "tan", Hint = "trig tangent", Operands = 1 },
      [FPGAOp.Asin] = new FPGAOpInfo { Symbol = "asin", Hint = "trig arcsine", Operands = 1 },
      [FPGAOp.Acos] = new FPGAOpInfo { Symbol = "acos", Hint = "trig arccosine", Operands = 1 },
      [FPGAOp.Atan] = new FPGAOpInfo { Symbol = "atan", Hint = "trig arctangent", Operands = 1 },
      [FPGAOp.LogicNot] = new FPGAOpInfo { Symbol = "!", Hint = "logical not (1 if zero, 0 otherwise)", Operands = 1 },
      [FPGAOp.BitNot] = new FPGAOpInfo { Symbol = "~", Hint = "bitwise not", Operands = 1 },
      [FPGAOp.Add] = new FPGAOpInfo { Symbol = "+", Hint = "add/plus", Operands = 2 },
      [FPGAOp.Subtract] = new FPGAOpInfo { Symbol = "-", Hint = "subtract/minus", Operands = 2 },
      [FPGAOp.Multiply] = new FPGAOpInfo { Symbol = "*", Hint = "multiply/times", Operands = 2 },
      [FPGAOp.Divide] = new FPGAOpInfo { Symbol = "/", Hint = "divide", Operands = 2 },
      [FPGAOp.Mod] = new FPGAOpInfo { Symbol = "%", Hint = "modulus", Operands = 2 },
      [FPGAOp.Atan2] = new FPGAOpInfo { Symbol = "atan2", Hint = "trig arctangent(y,x) from coordinates", Operands = 2 },
      [FPGAOp.Pow] = new FPGAOpInfo { Symbol = "pow", Hint = "exponent a^b", Operands = 2 },
      [FPGAOp.Less] = new FPGAOpInfo { Symbol = "<", Hint = "less", Operands = 2 },
      [FPGAOp.LessEquals] = new FPGAOpInfo { Symbol = "<=", Hint = "less or equal", Operands = 2 },
      [FPGAOp.Equals] = new FPGAOpInfo { Symbol = "==", Hint = "equal", Operands = 2 },
      [FPGAOp.GreaterEquals] = new FPGAOpInfo { Symbol = ">=", Hint = "greater or equal", Operands = 2 },
      [FPGAOp.Greater] = new FPGAOpInfo { Symbol = ">", Hint = "greater", Operands = 2 },
      [FPGAOp.NotEquals] = new FPGAOpInfo { Symbol = "!=", Hint = "not equal", Operands = 2 },
      [FPGAOp.Min] = new FPGAOpInfo { Symbol = "min", Hint = "minimum", Operands = 2 },
      [FPGAOp.Max] = new FPGAOpInfo { Symbol = "max", Hint = "maximum", Operands = 2 },
      [FPGAOp.LogicAnd] = new FPGAOpInfo { Symbol = "&&", Hint = "logical and (both nonzero)", Operands = 2 },
      [FPGAOp.LogicOr] = new FPGAOpInfo { Symbol = "||", Hint = "logical or (either nonzero)", Operands = 2 },
      [FPGAOp.BitAnd] = new FPGAOpInfo { Symbol = "&", Hint = "bitwise and", Operands = 2 },
      [FPGAOp.BitOr] = new FPGAOpInfo { Symbol = "|", Hint = "bitwise or", Operands = 2 },
      [FPGAOp.BitXor] = new FPGAOpInfo { Symbol = "^", Hint = "bitwise xor", Operands = 2 },
      [FPGAOp.BitNand] = new FPGAOpInfo { Symbol = "nand", Hint = "bitwise nand", Operands = 2 },
      [FPGAOp.BitNor] = new FPGAOpInfo { Symbol = "nor", Hint = "bitwise nor", Operands = 2 },
      [FPGAOp.BitSla] = new FPGAOpInfo { Symbol = "sla", Hint = "shift left arithmetic", Operands = 2 },
      [FPGAOp.BitSll] = new FPGAOpInfo { Symbol = "sll", Hint = "shift left logical", Operands = 2 },
      [FPGAOp.BitSra] = new FPGAOpInfo { Symbol = "sra", Hint = "shift right arithmetic", Operands = 2 },
      [FPGAOp.BitSrl] = new FPGAOpInfo { Symbol = "srl", Hint = "shift right logical", Operands = 2 },
    };
  }
}
