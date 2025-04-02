using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Objects.Electrical;
using UnityEngine;

namespace fpgamod
{
  [XmlInclude(typeof(BasicFPGAReaderHousingSaveData))]
  public class BasicFPGAReaderHousingSaveData : LogicBaseSaveData
  {
    [XmlElement]
    public long[] DeviceIDs;
  }
}
