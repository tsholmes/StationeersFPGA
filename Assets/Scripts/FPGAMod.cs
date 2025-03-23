using System;
using fpgamod;
using HarmonyLib;
using StationeersMods.Interface;
using Assets.Scripts.Objects;
using UnityEngine;
[StationeersMod("FPGAMod", "FPGAMod [StationeersMods]", "0.2.4657.21547.1")]
public class FPGAMod : ModBehaviour
{
  // private ConfigEntry<bool> configBool;

  public override void OnLoaded(ContentHandler contentHandler)
  {
    UnityEngine.Debug.Log("FPGAMod says: Hello World!");

    //Config example
    // configBool = Config.Bind("Input",
    //     "Boolean",
    //     true,
    //     "Boolean description");

    Harmony harmony = new Harmony("FPGAMod");
    PrefabPatch.prefabs = contentHandler.prefabs;
    harmony.PatchAll();

    Debug.Log("FPGAMod Loaded with " + contentHandler.prefabs.Count + " prefab(s)");
  }
}
