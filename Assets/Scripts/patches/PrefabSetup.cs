using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using LaunchPadBooster.Utils;
using UnityEngine;

namespace fpgamod
{
  public static class PrefabSetup
  {
    public static void CheckBlueprint(Thing thing)
    {
      if (thing.Blueprint == null)
        ConsoleWindow.PrintError($"{thing.PrefabName} is missing Blueprint");
      else if (!thing.Blueprint.TryGetComponent<Assets.Scripts.UI.Wireframe>(out var wireframe))
        ConsoleWindow.PrintError($"{thing.PrefabName} Blueprint is missing wireframe");
      else if (wireframe.WireframeEdges == null || wireframe.WireframeEdges.Count == 0)
        ConsoleWindow.PrintError($"{thing.PrefabName} Blueprint Wireframe is missing edges");
    }

    private static Dictionary<string, ColorType> MATERIAL_MAP = new() {
      {"ColorBlu", ColorType.Blue},
      {"ColorGra", ColorType.Gray},
      {"ColorGre", ColorType.Green},
      {"ColorOra", ColorType.Orange},
      {"ColorRed", ColorType.Red},
      {"ColorYel", ColorType.Yellow},
      {"ColorWhi", ColorType.White},
      {"ColorBla", ColorType.Black},
      {"ColorBro", ColorType.Brown},
      {"ColorKha", ColorType.Khaki},
      {"ColorPin", ColorType.Pink},
      {"ColorPur", ColorType.Purple},
    };

    public static void FixMaterials(Thing thing)
    {
      if (thing is IPatchOnLoad patchable && patchable.SkipMaterialPatch())
        return;

      var custom = thing as ICustomUV;
      var defaultUV = ModUtils.UVTile(4, 3, 3); // top corner solid color by default
      foreach (var renderer in thing.GetComponentsInChildren<MeshRenderer>())
      {
        var mats = renderer.sharedMaterials;
        for (var i = 0; i < mats.Length; i++)
          mats[i] = MatchMaterial(mats[i]);
        renderer.sharedMaterials = mats;

        var mesh = renderer.GetComponent<MeshFilter>().mesh;
        ModUtils.PatchMeshUV(mesh, custom?.GetUV(renderer.gameObject) ?? defaultUV);
      }
      if (thing.PaintableMaterial != null)
        thing.PaintableMaterial = MatchMaterial(thing.PaintableMaterial);
    }

    private static Material MatchMaterial(Material mat)
    {
      var key = mat.name[..8];
      if (!MATERIAL_MAP.TryGetValue(key, out var color))
        return mat;
      return PrefabUtils.GetColorMaterial(color);
    }
  }
}
