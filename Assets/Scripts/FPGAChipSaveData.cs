using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace fpgamod
{
  [XmlInclude(typeof(FPGAChipSaveData))]
  public class FPGAChipSaveData : DynamicThingSaveData
  {
    [XmlElement]
    public string RawConfig;
  }
}
