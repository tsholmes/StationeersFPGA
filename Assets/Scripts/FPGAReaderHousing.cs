using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using LaunchPadBooster.Utils;
using Objects.Electrical;
using UnityEngine;

namespace fpgamod
{
  public class FPGAReaderHousing :
    LogicUnitBase,
    IFPGAInput,
    IPatchOnLoad,
    ICustomUV,
    IFPGAHolder
  {
    private const ushort FLAG_DEVICES = 512;

    private Slot _FPGASlot => this.Slots[0];
    private FPGAChip FPGAChip => this._FPGASlot.Get<FPGAChip>();

    public ILogicable[] Devices = new ILogicable[8];
    private long[] _DeviceIDs = new long[8];

    private long _modCount = 0;
    private double[] _outputs = new double[8];

    public void PatchOnLoad()
    {
      this.SetExitTool(PrefabNames.Drill);
      this._FPGASlot.Type = FPGAChip.FPGASlotType;
    }


    public override void OnPrefabLoad()
    {
      var src = PrefabUtils.FindPrefab<Structure>("StructureCircuitHousing");
      var srcOnOff = src.transform.Find("OnOffNoShadow");
      var onOff = GameObject.Instantiate(srcOnOff, this.transform);
      this.Interactables[2].Collider = onOff.GetComponent<SphereCollider>();

      var pos = onOff.transform.localPosition;
      pos.x = pos.y = 0.155f;
      onOff.transform.localPosition = pos;

      this.OnOffButton = onOff.GetComponent<LogicOnOffButton>();

      base.OnPrefabLoad();
    }

    public Vector2? GetUV(GameObject obj) => this.RelativePath(obj) switch
    {

      "FPGAHousing_readerbase/default" => ModUtils.UVTile(16, 0, 6),
      "PowerSymbol/default" => ModUtils.UVTile(16, 2, 5),
      "DataInSymbol/default" => ModUtils.UVTile(16, 1, 4),
      "DataOutSymbol/default" => ModUtils.UVTile(16, 1, 4),
      "FPGAHousing_pins/default" => ModUtils.UVTile(64, 3, 7),
      _ => null,
    };

    public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
    {
      base.BuildUpdate(writer, networkUpdateType);
      if (Thing.IsNetworkUpdateRequired(FLAG_DEVICES, networkUpdateType))
      {
        for (var i = 0; i < 8; i++)
          writer.WriteInt64(this.Devices[i]?.ReferenceId ?? 0);
      }
    }

    public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
    {
      base.ProcessUpdate(reader, networkUpdateType);
      if (Thing.IsNetworkUpdateRequired(FLAG_DEVICES, networkUpdateType))
      {
        for (var i = 0; i < 8; i++)
          this.Devices[i] = Referencable.Find<ILogicable>(reader.ReadInt64());
      }
    }

    public override void SerializeOnJoin(RocketBinaryWriter writer)
    {
      base.SerializeOnJoin(writer);
      for (var i = 0; i < 8; i++)
        writer.WriteInt64(this.Devices[i]?.ReferenceId ?? 0);
    }

    public override void DeserializeOnJoin(RocketBinaryReader reader)
    {
      base.DeserializeOnJoin(reader);
      for (var i = 0; i < 8; i++)
        this._DeviceIDs[i] = reader.ReadInt64();
    }

    public override ThingSaveData SerializeSave()
    {
      var saveData = new FPGAReaderHousingSaveData();
      var baseData = saveData as ThingSaveData;
      this.InitialiseSaveData(ref baseData);
      return saveData;
    }

    public override void DeserializeSave(ThingSaveData baseData)
    {
      base.DeserializeSave(baseData);
      if (baseData is not FPGAReaderHousingSaveData saveData)
        return;
      if (saveData.DeviceIDs != null)
      {
        for (var i = 0; i < saveData.DeviceIDs.Length && i < 8; i++)
          this._DeviceIDs[i] = saveData.DeviceIDs[i];
      }
    }

    protected override void InitialiseSaveData(ref ThingSaveData baseData)
    {
      base.InitialiseSaveData(ref baseData);
      if (baseData is not FPGAReaderHousingSaveData saveData)
        return;
      saveData.DeviceIDs = new long[8];
      for (var i = 0; i < 8; i++)
        saveData.DeviceIDs[i] = this.Devices[i]?.ReferenceId ?? 0;
    }

    public override void OnFinishedLoad()
    {
      base.OnFinishedLoad();
      for (var i = 0; i < 8; i++)
        this.Devices[i] = Thing.Find<Device>(this._DeviceIDs[i]);
    }

    public double GetFPGAInputPin(int index)
    {
      if (index < 0 || index >= 8)
        return double.NaN;
      if (this.Devices[index] == null)
        return 0;
      if (!this.InputNetwork1.DeviceList.Contains(this))
        return 0;
      return this.Devices[index].GetLogicValue(LogicType.Setting);
    }

    public long GetFPGAInputModCount() => this._modCount;

    public FPGAChip GetFPGAChip() => this.FPGAChip;

    public override void OnPowerTick()
    {
      base.OnPowerTick();
      this.LogicChanged();
      var chip = this.FPGAChip;
      if (!this.OnOff || !this.Powered || chip == null)
        return;
      this._modCount++;
      for (var i = 0; i < 8; i++)
        this._outputs[i] = chip.ReadMemory(i, this);
      this.Setting = this._outputs[0];
    }

    private string GetDeviceNameWithLabel(int index)
    {
      var chip = this.FPGAChip;
      string name = this.Devices[index] != null ? this.Devices[index].DisplayName : $"<color=red>{InterfaceStrings.LogicNoDevice}</color>";
      string label = chip != null ? chip.GetInputLabel(index) : FPGADef.GetName((byte)index);
      return $"<color=yellow>{label}</color> {name}";
    }

    public override string GetContextualName(Interactable interactable)
    {
      var index = InteractableIndex(interactable.Action);
      if (index != -1)
        return GetDeviceNameWithLabel(index);
      return base.GetContextualName(interactable);
    }

    public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
    {
      if (interactable == null)
        return null;
      var index = InteractableIndex(interactable.Action);
      if (index != -1)
      {
        var action = new DelayedActionInstance
        {
          Duration = 0f,
          ActionMessage = interactable.ContextualName,
        };
        if (!interaction.SourceSlot.Contains<Screwdriver>())
          return action.Fail(GameStrings.RequiresScrewdriver);
        action = Logicable._TryGetNextLogicDevice(interactable, interaction, ref this.Devices[index], this.InputNetwork1DevicesSorted, doAction, 255);
        if (doAction)
          this.NetworkUpdateFlags |= FLAG_DEVICES;
        return action;
      }
      return base.InteractWith(interactable, interaction, doAction);
    }

    private int InteractableIndex(InteractableType typ) => typ switch
    {
      InteractableType.Button1 => 0,
      InteractableType.Button2 => 1,
      InteractableType.Button3 => 2,
      InteractableType.Button4 => 3,
      InteractableType.Button5 => 4,
      InteractableType.Button6 => 5,
      InteractableType.Button7 => 6,
      InteractableType.Button8 => 7,
      _ => -1,
    };

    public override bool CanLogicRead(LogicType logicType) =>
      base.CanLogicRead(logicType) ||
      logicType == LogicType.Setting ||
      (logicType >= LogicType.Channel0 && logicType <= LogicType.Channel7);

    public override double GetLogicValue(LogicType logicType)
    {
      if (logicType >= LogicType.Channel0 && logicType <= LogicType.Channel7)
        return this._outputs[logicType - LogicType.Channel0];
      return base.GetLogicValue(logicType);
    }
  }
}
