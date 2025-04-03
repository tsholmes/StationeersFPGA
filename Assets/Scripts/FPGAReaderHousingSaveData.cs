using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Objects.Electrical;
using UnityEngine;

namespace fpgamod
{
  [XmlInclude(typeof(FPGAReaderHousingSaveData))]
  public class FPGAReaderHousingSaveData : LogicBaseSaveData
  {
    [XmlElement]
    public long[] DeviceIDs;
  }
}
