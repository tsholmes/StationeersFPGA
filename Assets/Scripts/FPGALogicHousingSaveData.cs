using System.Xml.Serialization;
using Assets.Scripts.Objects.Electrical;

namespace fpgamod
{
  [XmlInclude(typeof(FPGALogicHousingSaveData))]
  public class FPGALogicHousingSaveData : LogicBaseSaveData
  {
    [XmlElement]
    public ulong NonZero;
    [XmlElement]
    public double[] Values;
  }
}
