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
        // don't overwrite existing blueprints, but generate the wireframe for our own
        GenerateWireframe(thing.Blueprint, thing.Blueprint.transform);
        return;
      }

      if (blueprintContainer == null)
      {
        blueprintContainer = new GameObject("~Blueprints");
        UnityEngine.Object.DontDestroyOnLoad(blueprintContainer);
        blueprintContainer.SetActive(false);
      }

      var blueprint = new GameObject(thing.PrefabName);
      blueprint.transform.parent = blueprintContainer.transform;
      blueprint.AddComponent<MeshFilter>();
      blueprint.AddComponent<MeshRenderer>();
      blueprint.AddComponent<Wireframe>();

      GenerateWireframe(blueprint, thing.transform);

      thing.Blueprint = blueprint;
    }

    private static void GenerateWireframe(GameObject blueprint, Transform srcTransform)
    {
      var wireframe = blueprint.GetComponent<Wireframe>();
      if (wireframe == null)
      {
        return;
      }

      var meshFilter = blueprint.GetComponent<MeshFilter>();
      var meshRenderer = blueprint.GetComponent<MeshRenderer>();

      wireframe.BlueprintTransform = blueprint.transform;
      wireframe.BlueprintMeshFilter = meshFilter;
      wireframe.BlueprintRenderer = meshRenderer;

      var gen = new WireframeGenerator(srcTransform);

      meshFilter.mesh = gen.CombinedMesh;
      meshRenderer.materials = StationeersModsUtility.GetBlueprintMaterials(1);
      wireframe.WireframeEdges = gen.Edges;
      wireframe.ShowTransformArrow = false;
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
