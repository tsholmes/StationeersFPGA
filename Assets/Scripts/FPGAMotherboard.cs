using System.Collections;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using StationeersMods.Interface;
using UnityEngine;
using UnityUI = UnityEngine.UI;

namespace fpgamod
{
  public class FPGAMotherboard :
    Motherboard,
    IPatchOnLoad,
    ILocalizedPrefab
  {
    public readonly List<IFPGAHolder> ConnectedFPGAHolders = new List<IFPGAHolder>();
    // Editor State
    // TODO: savedata and network update for these
    public int SelectedHolderIndex { get; set; }
    public string RawConfig { get; set; }
    public ulong InputOpen { get; set; }
    public ulong GateOpen { get; set; }
    public ulong LutOpen { get; set; }

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

    public Localization.LocalizationThingDat GetLocalization()
    {
      return new Localization.LocalizationThingDat
      {
        PrefabName = "FPGA Editor Motherboard",
        Description = ""
        + "Allows editing a {THING:ItemBasicFPGAChip} placed in a {THING:StructureBasicFPGALogicHousing} on a connected data network."
      };
    }

    public override bool IsOperable => true;

    public void OnEdit()
    {
      ImGuiFPGAEditor.ShowEditor(this);
    }

    public override void OnDeviceListChanged()
    {
      base.OnDeviceListChanged();
      this.LoadConnected();
    }

    public override void OnInsertedToComputer(IComputer computer)
    {
      base.OnInsertedToComputer(computer);
      this.LoadConnected();
    }

    public override void OnRemovedFromComputer(IComputer computer)
    {
      base.OnRemovedFromComputer(computer);
      this.LoadConnected();
    }

    public string GetSelectedFPGAHolderName()
    {
      if (!this.IsSelectedIndexValid)
      {
        return "";
      }
      return this.ConnectedFPGAHolders[this.SelectedHolderIndex].DisplayName;
    }

    private bool IsSelectedIndexValid => this.SelectedHolderIndex >= 0 && this.SelectedHolderIndex < this.ConnectedFPGAHolders.Count;

    private void LoadConnected()
    {
      var current = this.IsSelectedIndexValid ? this.ConnectedFPGAHolders[this.SelectedHolderIndex] : null;
      this.ConnectedFPGAHolders.Clear();
      if (this.ParentComputer == null || !this.ParentComputer.AsThing().isActiveAndEnabled)
      {
        return;
      }
      var deviceList = this.ParentComputer.DeviceList();
      deviceList.Sort((a, b) => a.DisplayName.CompareTo(b.DisplayName));
      foreach (var device in this.ParentComputer.DeviceList())
      {
        if (device is not IFPGAHolder holder)
        {
          continue;
        }
        if (holder == current)
        {
          this.SelectedHolderIndex = this.ConnectedFPGAHolders.Count;
        }
        this.ConnectedFPGAHolders.Add(holder);
      }
      if (this.SelectedHolderIndex < 0 || this.SelectedHolderIndex >= this.ConnectedFPGAHolders.Count)
      {
        this.SelectedHolderIndex = 0;
      }
    }

    public void Import()
    {
      if (this.IsSelectedIndexValid)
      {
        var chip = this.ConnectedFPGAHolders[this.SelectedHolderIndex].GetFPGAChip();
        this.RawConfig = chip.RawConfig;
      }
      else
      {
        this.RawConfig = "";
      }
    }

    public void Export()
    {
      if (!this.IsSelectedIndexValid)
      {
        return;
      }
      var chip = this.ConnectedFPGAHolders[this.SelectedHolderIndex].GetFPGAChip();
      chip.RawConfig = this.RawConfig;
    }
  }
}
