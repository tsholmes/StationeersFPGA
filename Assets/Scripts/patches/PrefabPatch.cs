using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using HarmonyLib;
using StationeersMods.Interface;
using UnityEngine;
using UnityEngine.Rendering;
namespace fpgamod
{
  [HarmonyPatch]
  public class PrefabPatch
  {
    public static ReadOnlyCollection<GameObject> prefabs { get; set; }
    [HarmonyPatch(typeof(Prefab), nameof(Prefab.LoadAll))]
    public static void Prefix()
    {
      try
      {
        Debug.Log("Prefab Patch started");
        foreach (var gameObject in prefabs)
        {
          Thing thing = gameObject.GetComponent<Thing>();
          if (thing == null)
          {
            continue;
          }
          var doMatPatch = true;
          if (thing is IPatchOnLoad patchable)
          {
            patchable.PatchOnLoad();
            doMatPatch = !patchable.SkipMaterialPatch();
          }
          if (thing is ILocalizedPrefab localized)
          {
            FPGAMod.AddLocalizationThing(thing.PrefabHash, localized.GetLocalization());
          }
          Blueprintify(thing);
          if (doMatPatch)
          {
            FixMaterials(thing);
          }
          WorldManager.Instance.AddPrefab(thing);
        }
      }
      catch (Exception ex)
      {
        Debug.Log(ex.Message);
        Debug.LogException(ex);
      }
    }

    private static GameObject blueprintContainer;

    private static void Blueprintify(Thing thing)
    {
      if (thing.Blueprint != null)
      {
        return; // don't overwrite if we get around to making manual blueprints
      }

      if (blueprintContainer == null)
      {
        blueprintContainer = new GameObject("~Blueprints");
        UnityEngine.Object.DontDestroyOnLoad(blueprintContainer);
        blueprintContainer.SetActive(false);
      }

      var blueprint = new GameObject(thing.PrefabName);
      blueprint.transform.parent = blueprintContainer.transform;
      var meshFilter = blueprint.AddComponent<MeshFilter>();
      var meshRenderer = blueprint.AddComponent<MeshRenderer>();
      var wireframe = blueprint.AddComponent<Wireframe>();

      wireframe.BlueprintTransform = blueprint.transform;
      wireframe.BlueprintMeshFilter = meshFilter;
      wireframe.BlueprintRenderer = meshRenderer;

      var gen = new WireframeGenerator(thing.transform);

      meshFilter.mesh = gen.CombinedMesh;
      meshRenderer.materials = StationeersModsUtility.GetBlueprintMaterials(1);
      wireframe.WireframeEdges = gen.Edges;
      wireframe.ShowTransformArrow = false;

      thing.Blueprint = blueprint;
    }

    private static Dictionary<string, StationeersColor> MATERIAL_MAP = new() {
      {"ColorBlu", StationeersColor.BLUE},
      {"ColorGra", StationeersColor.GRAY},
      {"ColorGre", StationeersColor.GREEN},
      {"ColorOra", StationeersColor.ORANGE},
      {"ColorRed", StationeersColor.RED},
      {"ColorYel", StationeersColor.YELLOW},
      {"ColorWhi", StationeersColor.WHITE},
      {"ColorBla", StationeersColor.BLACK},
      {"ColorBro", StationeersColor.BROWN},
      {"ColorKha", StationeersColor.KHAKI},
      {"ColorPin", StationeersColor.PINK},
      {"ColorPur", StationeersColor.PURPLE},
    };

    private static void FixMaterials(Thing thing)
    {
      var custom = thing as fpgamod.ICustomUV;
      var defaultUV = FPGAMod.UVTile(4, 3, 3); // top corner solid color by default
      foreach (var renderer in thing.GetComponentsInChildren<MeshRenderer>())
      {
        var mats = renderer.sharedMaterials;
        for (var i = 0; i < mats.Length; i++)
        {
          mats[i] = MatchMaterial(mats[i]);
        }
        renderer.sharedMaterials = mats;

        var mesh = renderer.GetComponent<MeshFilter>().mesh;
        FPGAMod.PatchMeshUV(mesh, custom?.GetUV(renderer.gameObject) ?? defaultUV);
      }
      if (thing.PaintableMaterial != null)
      {
        thing.PaintableMaterial = MatchMaterial(thing.PaintableMaterial);
      }
    }

    private static Material MatchMaterial(Material mat)
    {
      var key = mat.name[..8];
      StationeersColor match;
      if (!MATERIAL_MAP.TryGetValue(key, out match))
      {
        return mat;
      }
      return StationeersModsUtility.GetMaterial(match, ShaderType.NORMAL);
    }
  }
}
