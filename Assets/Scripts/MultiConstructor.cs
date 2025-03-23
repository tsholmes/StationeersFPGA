using System.Collections;
using System.Collections.Generic;
using StationeersMods.Interface;
using UnityEngine;

namespace fpgamod
{
  public class MultiConstructor :
  Assets.Scripts.Objects.MultiConstructor,
  IPatchOnLoad
  {
    [SerializeField]
    public string ThumbnailCopyPrefab;

    public void PatchOnLoad()
    {
      if (this.ThumbnailCopyPrefab != "") {
        this.Thumbnail = StationeersModsUtility.FindPrefab(this.ThumbnailCopyPrefab).Thumbnail;
      }
    }
  }
}
