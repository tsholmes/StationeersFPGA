using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using UnityEngine;

namespace fpgamod
{
  [XmlInclude(typeof(FPGAMotherboardSaveData))]
  public class FPGAMotherboardSaveData : MotherboardSaveData
  {
    [XmlElement]
    public int SelectedHolderIndex;
    [XmlElement]
    public string RawConfig;
    [XmlElement]
    public ulong InputOpen;
    [XmlElement]
    public ulong GateOpen;
    [XmlElement]
    public ulong LutOpen;
  }
}
