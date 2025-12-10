using System;
using HarmonyLib;
using UnityEngine;
using Assets.Scripts;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using LaunchPadBooster;
using Assets.Scripts.Objects;

namespace fpgamod
{
  public class FPGAMod : MonoBehaviour
  {
    public static Mod MOD = new("FPGA", "0.2.0");

    public void OnLoaded(List<GameObject> prefabs)
    {
      var harmony = new Harmony("FPGAMod");
      ImGuiFPGAEditor.Initialize(prefabs.First(go => go.name == "UIBlocker"));
      harmony.PatchAll();

      MOD.SetVersionCheck(v => v.StartsWith("0.2."));
      MOD.AddPrefabs(prefabs);

      MOD.SetupPrefabs<IPatchOnLoad>().RunFunc(prefab => prefab.PatchOnLoad());
      MOD.SetupPrefabs()
        .SetBlueprintMaterials()
        .RunFunc(PrefabSetup.FixMaterials)
        .RunFunc(PrefabSetup.CheckBlueprint);

      MOD.AddSaveDataType<FPGAChipSaveData>();
      MOD.AddSaveDataType<FPGAMotherboardSaveData>();
      MOD.AddSaveDataType<FPGAReaderHousingSaveData>();
      MOD.AddSaveDataType<FPGALogicHousingSaveData>();

      Debug.Log("FPGAMod Loaded with " + prefabs.Count + " prefab(s)");
    }
  }

  public static class ModUtils
  {
    public static void PatchEnumCollection<T1, T2>(EnumCollection<T1, T2> collection, T1 val, string name)
      where T1 : Enum, new()
      where T2 : IConvertible, IEquatable<T2>
    {
      if (name.Length > collection.LongestName.Length)
      {
        throw new Exception("not implemented");
      }
      var origLength = collection.Length;
      var values = collection.Values;
      var valuesAsInts = collection.ValuesAsInts;
      var names = collection.Names;
      var paddedNames = collection.PaddedNames;
      Array.Resize(ref values, origLength + 1);
      Array.Resize(ref valuesAsInts, origLength + 1);
      Array.Resize(ref names, origLength + 1);
      Array.Resize(ref paddedNames, origLength + 1);

      values[origLength] = val;
      valuesAsInts[origLength] = (T2)(object)val;
      names[origLength] = name;
      paddedNames[origLength] = name.PadRight(collection.LongestName.Length, ' ');

      collection.Values = values;
      collection.ValuesAsInts = valuesAsInts;

      var nameField = collection.GetType().GetField(nameof(collection.Names), BindingFlags.Instance | BindingFlags.Public);
      nameField.SetValue(collection, names);
      var paddedNamesField = collection.GetType().GetField(nameof(collection.PaddedNames), BindingFlags.Instance | BindingFlags.Public);
      paddedNamesField.SetValue(collection, paddedNames);
      var lengthField = collection.GetType().GetField($"<{nameof(collection.Length)}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
      lengthField.SetValue(collection, origLength + 1);
    }

    public static string RelativePath(this Thing thing, GameObject child)
    {
      var thingXform = thing.transform;
      var childXform = child.transform;
      if (thingXform.root != childXform.root)
        throw new InvalidOperationException($"{child.name} is not a child of {thing.PrefabName}");

      var path = "";
      while (childXform != thingXform)
      {
        if (path != "") path = $"/{path}";
        path = $"{childXform.name}{path}";
        childXform = childXform.parent;
      }
      return path;
    }

    public static Vector2 UVTile(float gridSize, float gridX, float gridY) =>
      new((gridX + 0.5f) / gridSize, (gridY + 0.5f) / gridSize);

    public static void PatchMeshUV(Mesh mesh, Vector2 val)
    {
      var uv = new Vector2[mesh.vertices.Length];
      Array.Fill(uv, val);
      mesh.uv = uv;
    }
  }
}