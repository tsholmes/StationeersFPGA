using System;
using System.Collections.Generic;
using Assets.Scripts.Objects.Electrical;

namespace fpgamod
{
  // gate instruction value (by byte low-high): [op] [g0] [g1] [unused]x5
  // g0/g1 = [0-63] input pin, [64-127] gate output value, [128-191] lookup table
  public enum FPGAOp : byte
  {
    None,

    // unary ops perform op on g0
    Lookup,
    Ceil,
    Floor,
    Trunc,
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
    public Func<double> ConstantOp;
    public Func<double, double> UnaryOp;
    public Func<double, double, double> BinaryOp;
  }

  public static class FPGAOps
  {
    public const FPGAOp Count = FPGAOp.BitSrl + 1;
    public static FPGAOpInfo GetOpInfo(FPGAOp op)
    {
      return InfoDict.GetValueOrDefault(op, InvalidOp);
    }
    public static FPGAOpInfo InvalidOp = new() { Symbol = "invalid", Hint = "", ConstantOp = () => double.NaN };
    public static readonly Dictionary<FPGAOp, FPGAOpInfo> InfoDict = new()
    {
      [FPGAOp.None] = new FPGAOpInfo { Symbol = "none", Hint = "", ConstantOp = () => 0 },
      [FPGAOp.Lookup] = new FPGAOpInfo { Symbol = "@", Hint = "lookup table by index (0-63)", Operands = 1 },
      [FPGAOp.Ceil] = new FPGAOpInfo { Symbol = "ceil", Hint = "round up", Operands = 1, UnaryOp = Math.Ceiling },
      [FPGAOp.Floor] = new FPGAOpInfo { Symbol = "floor", Hint = "round down", Operands = 1, UnaryOp = Math.Floor },
      [FPGAOp.Trunc] = new FPGAOpInfo { Symbol = "trunc", Hint = "truncate/ipart", Operands = 1, UnaryOp = Math.Truncate },
      [FPGAOp.Abs] = new FPGAOpInfo { Symbol = "abs", Hint = "absolute value", Operands = 1, UnaryOp = Math.Abs },
      [FPGAOp.Log] = new FPGAOpInfo { Symbol = "log", Hint = "natural logarithm base e", Operands = 1, UnaryOp = Math.Log },
      [FPGAOp.Exp] = new FPGAOpInfo { Symbol = "exp", Hint = "exponential function e^x", Operands = 1, UnaryOp = Math.Exp },
      [FPGAOp.Round] = new FPGAOpInfo { Symbol = "round", Hint = "round to nearest", Operands = 1, UnaryOp = Math.Round },
      [FPGAOp.Sqrt] = new FPGAOpInfo { Symbol = "sqrt", Hint = "square root", Operands = 1, UnaryOp = Math.Sqrt },
      [FPGAOp.Sin] = new FPGAOpInfo { Symbol = "sin", Hint = "trig sine", Operands = 1, UnaryOp = Math.Sin },
      [FPGAOp.Cos] = new FPGAOpInfo { Symbol = "cos", Hint = "trig cosine", Operands = 1, UnaryOp = Math.Cos },
      [FPGAOp.Tan] = new FPGAOpInfo { Symbol = "tan", Hint = "trig tangent", Operands = 1, UnaryOp = Math.Tan },
      [FPGAOp.Asin] = new FPGAOpInfo { Symbol = "asin", Hint = "trig arcsine", Operands = 1, UnaryOp = Math.Asin },
      [FPGAOp.Acos] = new FPGAOpInfo { Symbol = "acos", Hint = "trig arccosine", Operands = 1, UnaryOp = Math.Acos },
      [FPGAOp.Atan] = new FPGAOpInfo { Symbol = "atan", Hint = "trig arctangent", Operands = 1, UnaryOp = Math.Atan },
      [FPGAOp.LogicNot] = new FPGAOpInfo { Symbol = "!", Hint = "logical not (1 if zero, 0 otherwise)", Operands = 1, UnaryOp = OpLogicNot },
      [FPGAOp.BitNot] = new FPGAOpInfo { Symbol = "~", Hint = "bitwise not", Operands = 1, UnaryOp = OpBitNot },
      [FPGAOp.Add] = new FPGAOpInfo { Symbol = "+", Hint = "add/plus", Operands = 2, BinaryOp = OpAdd },
      [FPGAOp.Subtract] = new FPGAOpInfo { Symbol = "-", Hint = "subtract/minus", Operands = 2, BinaryOp = OpSubtract },
      [FPGAOp.Multiply] = new FPGAOpInfo { Symbol = "*", Hint = "multiply/times", Operands = 2, BinaryOp = OpMultiply },
      [FPGAOp.Divide] = new FPGAOpInfo { Symbol = "/", Hint = "divide", Operands = 2, BinaryOp = OpDivide },
      [FPGAOp.Mod] = new FPGAOpInfo { Symbol = "%", Hint = "modulus", Operands = 2, BinaryOp = OpMod },
      [FPGAOp.Atan2] = new FPGAOpInfo { Symbol = "atan2", Hint = "trig arctangent(y,x) from coordinates", Operands = 2, BinaryOp = Math.Atan2 },
      [FPGAOp.Pow] = new FPGAOpInfo { Symbol = "pow", Hint = "exponent a^b", Operands = 2, BinaryOp = Math.Pow },
      [FPGAOp.Less] = new FPGAOpInfo { Symbol = "<", Hint = "less", Operands = 2, BinaryOp = OpLess },
      [FPGAOp.LessEquals] = new FPGAOpInfo { Symbol = "<=", Hint = "less or equal", Operands = 2, BinaryOp = OpLessEquals },
      [FPGAOp.Equals] = new FPGAOpInfo { Symbol = "==", Hint = "equal", Operands = 2, BinaryOp = OpEquals },
      [FPGAOp.GreaterEquals] = new FPGAOpInfo { Symbol = ">=", Hint = "greater or equal", Operands = 2, BinaryOp = OpGreaterEquals },
      [FPGAOp.Greater] = new FPGAOpInfo { Symbol = ">", Hint = "greater", Operands = 2, BinaryOp = OpGreater },
      [FPGAOp.NotEquals] = new FPGAOpInfo { Symbol = "!=", Hint = "not equal", Operands = 2, BinaryOp = OpNotEquals },
      [FPGAOp.Min] = new FPGAOpInfo { Symbol = "min", Hint = "minimum", Operands = 2, BinaryOp = OpMin },
      [FPGAOp.Max] = new FPGAOpInfo { Symbol = "max", Hint = "maximum", Operands = 2, BinaryOp = OpMax },
      [FPGAOp.LogicAnd] = new FPGAOpInfo { Symbol = "&&", Hint = "logical and (both nonzero)", Operands = 2, BinaryOp = OpLogicAnd },
      [FPGAOp.LogicOr] = new FPGAOpInfo { Symbol = "||", Hint = "logical or (either nonzero)", Operands = 2, BinaryOp = OpLogicOr },
      [FPGAOp.BitAnd] = new FPGAOpInfo { Symbol = "&", Hint = "bitwise and", Operands = 2, BinaryOp = OpBitAnd },
      [FPGAOp.BitOr] = new FPGAOpInfo { Symbol = "|", Hint = "bitwise or", Operands = 2, BinaryOp = OpBitOr },
      [FPGAOp.BitXor] = new FPGAOpInfo { Symbol = "^", Hint = "bitwise xor", Operands = 2, BinaryOp = OpBitXor },
      [FPGAOp.BitNand] = new FPGAOpInfo { Symbol = "nand", Hint = "bitwise nand", Operands = 2, BinaryOp = OpBitNand },
      [FPGAOp.BitNor] = new FPGAOpInfo { Symbol = "nor", Hint = "bitwise nor", Operands = 2, BinaryOp = OpBitNor },
      [FPGAOp.BitSla] = new FPGAOpInfo { Symbol = "sla", Hint = "shift left arithmetic", Operands = 2, BinaryOp = OpBitSla },
      [FPGAOp.BitSll] = new FPGAOpInfo { Symbol = "sll", Hint = "shift left logical", Operands = 2, BinaryOp = OpBitSll },
      [FPGAOp.BitSra] = new FPGAOpInfo { Symbol = "sra", Hint = "shift right arithmetic", Operands = 2, BinaryOp = OpBitSra },
      [FPGAOp.BitSrl] = new FPGAOpInfo { Symbol = "srl", Hint = "shift right logical", Operands = 2, BinaryOp = OpBitSrl },
    };

    public static Dictionary<string, FPGAOp> SymbolToOp;
    static FPGAOps()
    {
      SymbolToOp = new();
      for (FPGAOp op = FPGAOp.None; op < Count; op++)
        SymbolToOp[InfoDict[op].Symbol] = op;
    }

    // unary op impl
    private static double OpLogicNot(double val) => val == 0 ? 1 : 0;
    private static double OpBitNot(double val) => ProgrammableChip.LongToDouble(~ProgrammableChip.DoubleToLong(val, true));
    // binary op impl
    private static double OpAdd(double val1, double val2) => val1 + val2;
    private static double OpSubtract(double val1, double val2) => val1 - val2;
    private static double OpMultiply(double val1, double val2) => val1 * val2;
    private static double OpDivide(double val1, double val2) => val1 / val2;
    private static double OpMod(double val1, double val2) { var res = val1 % val2; return res < 0 ? res + val2 : res; }
    private static double OpLess(double val1, double val2) => val1 < val2 ? 1 : 0;
    private static double OpLessEquals(double val1, double val2) => val1 <= val2 ? 1 : 0;
    private static double OpEquals(double val1, double val2) => val1 == val2 ? 1 : 0;
    private static double OpGreaterEquals(double val1, double val2) => val1 >= val2 ? 1 : 0;
    private static double OpGreater(double val1, double val2) => val1 > val2 ? 1 : 0;
    private static double OpNotEquals(double val1, double val2) => val1 != val2 ? 1 : 0;
    private static double OpMin(double val1, double val2) => val1 < val2 ? val1 : val2;
    private static double OpMax(double val1, double val2) => val1 > val2 ? val1 : val2;
    private static double OpLogicAnd(double val1, double val2) => val1 != 0 && val2 != 0 ? 1 : 0;
    private static double OpLogicOr(double val1, double val2) => val1 != 0 || val2 != 0 ? 1 : 0;
    private static Func<double, double, double> OpBitAnd = BinaryBitOp((val1, val2) => val1 & val2);
    private static Func<double, double, double> OpBitOr = BinaryBitOp((val1, val2) => val1 | val2);
    private static Func<double, double, double> OpBitXor = BinaryBitOp((val1, val2) => val1 ^ val2);
    private static Func<double, double, double> OpBitNand = BinaryBitOp((val1, val2) => ~(val1 & val2));
    private static Func<double, double, double> OpBitNor = BinaryBitOp((val1, val2) => ~(val1 | val2));
    private static Func<double, double, double> OpBitSla = BinaryBitOp((val1, val2) => val1 << (int)val2);
    private static Func<double, double, double> OpBitSll = BinaryBitOp((val1, val2) => val1 << (int)val2);
    private static Func<double, double, double> OpBitSra = BinaryBitOp((val1, val2) => val1 >> (int)val2);
    private static Func<double, double, double> OpBitSrl = BinaryBitOp((val1, val2) => val1 >> (int)val2, firstSigned: false);

    private static Func<double, double, double> BinaryBitOp(Func<long, long, long> op, bool firstSigned = true) =>
      (val1, val2) => ProgrammableChip.LongToDouble(
        op(ProgrammableChip.DoubleToLong(val1, firstSigned),
        ProgrammableChip.DoubleToLong(val2, true)));
  }
}
