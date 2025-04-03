using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Serialization;
using HarmonyLib;
using UnityEngine;

namespace fpgamod
{
  [HarmonyPatch]
  public class SaveDataPatch
  {
    [HarmonyPatch(typeof(XmlSaveLoad), nameof(XmlSaveLoad.AddExtraTypes))]
    public static void Prefix(ref List<System.Type> extraTypes)
    {
      extraTypes.Add(typeof(FPGAChipSaveData));
      extraTypes.Add(typeof(FPGAMotherboardSaveData));
      extraTypes.Add(typeof(FPGAReaderHousingSaveData));
    }
  }
}
