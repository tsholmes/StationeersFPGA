using System.Collections;
using System.Collections.Generic;
using Assets.Scripts;
using StationeersMods.Interface;
using UnityEngine;

namespace fpgamod
{
  public class MultiConstructor :
  Assets.Scripts.Objects.MultiConstructor,
  IPatchOnLoad,
  ILocalizedPrefab
  {
    [SerializeField]
    public string ModelCopyPrefab;

    [SerializeField]
    public string BaseLocalizedName;

    [SerializeField]
    [Multiline]
    public string BaseLocalizedDescription;

    public void PatchOnLoad()
    {
      if (this.ModelCopyPrefab != "")
      {
        var src = StationeersModsUtility.FindPrefab(this.ModelCopyPrefab);
        this.Thumbnail = src.Thumbnail;
        this.Blueprint = src.Blueprint;
        this.PaintableMaterial = src.PaintableMaterial;

        var srcMf = src.GetComponent<MeshFilter>();
        var mf = this.GetComponent<MeshFilter>();
        mf.mesh = srcMf.mesh;

        var srcRenderer = src.GetComponent<MeshRenderer>();
        var renderer = this.GetComponent<MeshRenderer>();
        renderer.materials = srcRenderer.materials;

        var srcCollider = src.GetComponent<BoxCollider>();
        var collider = this.GetComponent<BoxCollider>();
        collider.center = srcCollider.center;
        collider.size = srcCollider.size;
      }
    }

    bool IPatchOnLoad.SkipMaterialPatch() => this.ModelCopyPrefab != "";

    public Localization.LocalizationThingDat GetLocalization()
    {
      if (string.IsNullOrEmpty(this.BaseLocalizedName) && string.IsNullOrEmpty(this.BaseLocalizedDescription))
      {
        return null;
      }
      return new Localization.LocalizationThingDat
      {
        PrefabName = BaseLocalizedName ?? this.PrefabName,
        Description = BaseLocalizedDescription ?? ""
      };
    }
  }
}
