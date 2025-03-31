using System.Collections;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
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
    ILocalizedPrefab,
    ISourceCode
  {
    public readonly List<IFPGAHolder> ConnectedFPGAHolders = new List<IFPGAHolder>();
    private const ushort FLAG_RAWCONFIG = 256;

    private string _rawConfig = "";
    public string RawConfig
    {
      get => _rawConfig;
      set
      {
        this._rawConfig = value;
        this.SendUpdate();
      }
    }

    // Editor State
    public int SelectedHolderIndex { get; set; }
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

    public override ThingSaveData SerializeSave()
    {
      var saveData = new FPGAMotherboardSaveData();
      var baseData = saveData as ThingSaveData;
      this.InitialiseSaveData(ref baseData);
      return saveData;
    }

    public override void DeserializeSave(ThingSaveData baseData)
    {
      base.DeserializeSave(baseData);
      if (baseData is not FPGAMotherboardSaveData saveData)
      {
        return;
      }
      this.SelectedHolderIndex = saveData.SelectedHolderIndex;
      this.RawConfig = saveData.RawConfig;
      this.InputOpen = saveData.InputOpen;
      this.GateOpen = saveData.GateOpen;
      this.LutOpen = saveData.LutOpen;
    }

    protected override void InitialiseSaveData(ref ThingSaveData baseData)
    {
      base.InitialiseSaveData(ref baseData);
      if (baseData is not FPGAMotherboardSaveData saveData)
      {
        return;
      }
      saveData.SelectedHolderIndex = this.SelectedHolderIndex;
      saveData.RawConfig = this.RawConfig;
      saveData.InputOpen = this.InputOpen;
      saveData.GateOpen = this.GateOpen;
      saveData.LutOpen = this.LutOpen;
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
      if (chip != null)
      {
        chip.RawConfig = this.RawConfig;
      }
    }

    public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
    {
      base.BuildUpdate(writer, networkUpdateType);
      if (Thing.IsNetworkUpdateRequired(FLAG_RAWCONFIG, networkUpdateType))
      {
        writer.WriteAscii(this.GetSourceCode());
      }
    }

    public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
    {
      base.ProcessUpdate(reader, networkUpdateType);
      if (Thing.IsNetworkUpdateRequired(FLAG_RAWCONFIG, networkUpdateType))
      {
        this.SetSourceCode(new string(reader.ReadChars()));
      }
    }

    public override void SerializeOnJoin(RocketBinaryWriter writer)
    {
      base.SerializeOnJoin(writer);
      writer.WriteAscii(this.GetSourceCode());
    }

    public override void DeserializeOnJoin(RocketBinaryReader reader)
    {
      base.DeserializeOnJoin(reader);
      this.SetSourceCode(new string(reader.ReadChars()));
    }

    public char[] SourceCodeCharArray { get; set; }
    public int SourceCodeWritePointer { get; set; }

    public void SendUpdate()
    {
      if (NetworkManager.IsClient)
      {
        ISourceCode.SendSourceCodeToServer(this.GetSourceCode(), this.ReferenceId);
      }
      else
      {
        if (!NetworkManager.IsServer)
          return;
        this.NetworkUpdateFlags |= FLAG_RAWCONFIG;
      }
    }

    public void SetSourceCode(string sourceCode)
    {
      this._rawConfig = sourceCode;
    }

    public AsciiString GetSourceCode()
    {
      return AsciiString.Parse(this.RawConfig);
    }
  }
}
