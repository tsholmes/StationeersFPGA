using System.Xml.Serialization;
using Assets.Scripts.Objects.Electrical;

namespace fpgamod
{
  [XmlInclude(typeof(FPGAReaderHousingSaveData))]
  public class FPGAReaderHousingSaveData : LogicBaseSaveData
  {
    [XmlElement]
    public long[] DeviceIDs;
  }
}
