using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Objects;
using UnityEngine;

namespace fpgamod
{
  [XmlInclude(typeof(BasicFPGAChipSaveData))]
  public class BasicFPGAChipSaveData : DynamicThingSaveData
  {
    [XmlElement]
    public string RawConfig;
  }
}
