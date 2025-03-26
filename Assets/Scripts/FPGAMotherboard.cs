using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Objects.Items;
using StationeersMods.Interface;
using UnityEngine;
using UnityUI = UnityEngine.UI;

namespace fpgamod
{
  public class FPGAMotherboard :
    Motherboard,
    IPatchOnLoad
  {
    public void PatchOnLoad()
    {
      var existing = StationeersModsUtility.FindPrefab("MotherboardProgrammableChip");

      this.Thumbnail = existing.Thumbnail;
      this.Blueprint = existing.Blueprint;

      // copy whole model from existing motherboard
      var erenderer = existing.GetComponent<MeshRenderer>();
      var renderer = this.GetComponent<MeshRenderer>();
      renderer.materials = erenderer.materials;
      var emesh = existing.GetComponent<MeshFilter>();
      var mesh = this.GetComponent<MeshFilter>();
      mesh.mesh = emesh.mesh;

      // copy font
      var etitle = existing.transform.Find("ProgrammingWindow/ScreenTitle").GetComponent<UnityUI.Text>();
      var title = this.transform.Find("EditWindow/ScreenTitle").GetComponent<UnityUI.Text>();
      title.font = etitle.font;

      var btnText = this.transform.Find("EditWindow/EditButton/Text").GetComponent<UnityUI.Text>();
      btnText.font = etitle.font;
    }

    bool IPatchOnLoad.SkipMaterialPatch() => true;

    public override bool IsOperable => true;

    public void OnEdit()
    {
      ImGuiFPGAEditor.Motherboard = this;
      ImGuiFPGAEditor.ShowEditor(true);
    }
  }
}
