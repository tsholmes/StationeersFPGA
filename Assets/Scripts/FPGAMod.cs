using System;
using fpgamod;
using HarmonyLib;
using StationeersMods.Interface;
using UnityEngine;
using Assets.Scripts;
using System.Reflection;
using System.Linq;
[StationeersMod("FPGAMod", "FPGAMod", "0.1.6")]
public class FPGAMod : ModBehaviour
{
  public override void OnLoaded(ContentHandler contentHandler)
  {
    base.OnLoaded(contentHandler);

    Harmony harmony = new Harmony("FPGAMod");
    PrefabPatch.prefabs = contentHandler.prefabs;
    ImGuiFPGAEditor.Initialize(contentHandler.prefabs.First(go => go.name == "UIBlocker"));
    harmony.PatchAll();
    foreach (var m in harmony.GetPatchedMethods())
    {
      Debug.Log(m.FullDescription());
    }

    Debug.Log("FPGAMod Loaded with " + contentHandler.prefabs.Count + " prefab(s)");
  }

  public static Vector2 UVTile(float gridSize, float gridX, float gridY)
  {
    return new Vector2((gridX + 0.5f) / gridSize, (gridY + 0.5f) / gridSize);
  }

  public static void PatchMeshUV(Mesh mesh, Vector2 val)
  {
    var uv = new Vector2[mesh.vertices.Length];
    Array.Fill(uv, val);
    mesh.uv = uv;
  }

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
}
