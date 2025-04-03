using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Objects;
using UnityEngine;

namespace fpgamod
{
  [XmlInclude(typeof(FPGAChipSaveData))]
  public class FPGAChipSaveData : DynamicThingSaveData
  {
    [XmlElement]
    public string RawConfig;
  }
}
