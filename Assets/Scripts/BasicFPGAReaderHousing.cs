using System.Collections;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Objects.Electrical;
using StationeersMods.Interface;
using UnityEngine;

namespace fpgamod
{
  public class BasicFPGAReaderHousing :
    LogicUnitBase,
    IFPGAInput,
    IPatchOnLoad,
    ICustomUV,
    ILocalizedPrefab,
    IFPGAHolder
  {
    private const ushort FLAG_DEVICES = 512;

    private Slot _FPGASlot => this.Slots[0];
    private BasicFPGAChip FPGAChip => this._FPGASlot.Get<BasicFPGAChip>();

    // TODO: save and network updates
    public ILogicable[] Devices = new ILogicable[8];
    private long[] _DeviceIDs = new long[8];

    private long _modCount = 0;
    private double[] _outputs = new double[8];

    public void PatchOnLoad()
    {
      this.BuildStates[0].Tool.ToolExit = StationeersModsUtility.FindTool(StationeersTool.DRILL);
      this._FPGASlot.Type = BasicFPGAChip.FPGASlotType;

      var src = StationeersModsUtility.FindPrefab("StructureCircuitHousing");
      this.Thumbnail = src.Thumbnail;
    }


    public override void OnPrefabLoad()
    {
      var src = StationeersModsUtility.FindPrefab("StructureCircuitHousing");
      var srcOnOff = src.transform.Find("OnOffNoShadow");
      var onOff = GameObject.Instantiate(srcOnOff, this.transform);
      this.Interactables[2].Collider = onOff.GetComponent<SphereCollider>();

      var pos = onOff.transform.localPosition;
      pos.x = pos.y = 0.155f;
      onOff.transform.localPosition = pos;

      this.OnOffButton = onOff.GetComponent<LogicOnOffButton>();

      base.OnPrefabLoad();
    }

    public Vector2? GetUV(GameObject obj)
    {
      if (obj == this.transform.Find("BasicFPGAHousing_readerbase/default").gameObject)
      {
        return FPGAMod.UVTile(16, 0, 6); // match IC housing base
      }
      if (obj == this.transform.Find("PowerSymbol/default").gameObject)
      {
        return FPGAMod.UVTile(16, 2, 5); // match builtin power symbol
      }
      if (obj == this.transform.Find("DataInSymbol/default").gameObject)
      {
        return FPGAMod.UVTile(16, 1, 4); // match builtin data symbol
      }
      if (obj == this.transform.Find("DataOutSymbol/default").gameObject)
      {
        return FPGAMod.UVTile(16, 1, 4); // match builtin data symbol
      }
      if (obj == this.transform.Find("BasicFPGAHousing_pins/default").gameObject)
      {
        return FPGAMod.UVTile(64, 3, 7);
      }
      return null;
    }

    public Localization.LocalizationThingDat GetLocalization()
    {
      return new Localization.LocalizationThingDat
      {
        PrefabName = "FPGA Reader Housing",
        Description = ""
        + "Holds a {THING:ItemBasicFPGAChip}. "
      };
    }

    public double GetFPGAInputPin(int index)
    {
      if (index < 0 || index >= 8)
      {
        return double.NaN;
      }
      if (this.Devices[index] == null)
      {
        return double.NaN;
      }
      if (!this.InputNetwork1.DeviceList.Contains(this))
      {
        return double.NaN;
      }
      return this.Devices[index].GetLogicValue(LogicType.Setting);
    }

    public long GetFPGAInputModCount()
    {
      return this._modCount;
    }

    public BasicFPGAChip GetFPGAChip()
    {
      return this.FPGAChip;
    }

    public override void OnPowerTick()
    {
      base.OnPowerTick();
      this.LogicChanged();
      var chip = this.FPGAChip;
      if (!this.OnOff || !this.Powered || chip == null)
      {
        return;
      }
      this._modCount++;
      for (var i = 0; i < 8; i++)
      {
        this._outputs[i] = chip.ReadMemory(i);
      }
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
      {
        return GetDeviceNameWithLabel(index);
      }
      return base.GetContextualName(interactable);
    }

    public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
    {
      if (interactable == null)
      {
        return null;
      }
      var index = InteractableIndex(interactable.Action);
      if (index != -1)
      {
        var action = new DelayedActionInstance
        {
          Duration = 0f,
          ActionMessage = interactable.ContextualName,
        };
        if (!interaction.SourceSlot.Contains<Screwdriver>())
        {
          return action.Fail(GameStrings.RequiresScrewdriver);
        }
        return Logicable._TryGetNextLogicDevice(interactable, interaction, ref this.Devices[index], this.InputNetwork1DevicesSorted, doAction, 255);
      }
      return base.InteractWith(interactable, interaction, doAction);
    }

    private int InteractableIndex(InteractableType typ)
    {
      switch (typ)
      {
        case InteractableType.Button1:
          return 0;
        case InteractableType.Button2:
          return 1;
        case InteractableType.Button3:
          return 2;
        case InteractableType.Button4:
          return 3;
        case InteractableType.Button5:
          return 4;
        case InteractableType.Button6:
          return 5;
        case InteractableType.Button7:
          return 6;
        case InteractableType.Button8:
          return 7;
        default:
          return -1;
      }
    }

    public override bool CanLogicRead(LogicType logicType)
    {
      return base.CanLogicRead(logicType) || (logicType >= LogicType.Channel0 && logicType <= LogicType.Channel7);
    }

    public override double GetLogicValue(LogicType logicType)
    {
      if (logicType >= LogicType.Channel0 && logicType <= LogicType.Channel7)
      {
        return this._outputs[logicType - LogicType.Channel0];
      }
      return base.GetLogicValue(logicType);
    }
  }
}
