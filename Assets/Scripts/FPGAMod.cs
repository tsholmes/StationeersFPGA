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

  public static Vector2 UVTile(float gridSize, float gridX, float gridY) {
    return new Vector2((gridX + 0.5f) / gridSize, (gridY + 0.5f) / gridSize);
  }

  public static void PatchMeshUV(Mesh mesh, Vector2 val) {
    var uv = new Vector2[mesh.vertices.Length];
    Array.Fill(uv, val);
    mesh.uv = uv;
  }
}
