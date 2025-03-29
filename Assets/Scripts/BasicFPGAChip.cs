using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts;
using System;
using StationeersMods.Interface;

namespace fpgamod
{
  public class BasicFPGAChip :
    Item,
    IMemory,
    IMemoryReadable,
    IMemoryWritable,
    IPatchOnLoad,
    ICustomUV,
    ILocalizedPrefab
  {

    public const Slot.Class FPGASlotType = (Slot.Class)0x69;

    private FPGADef _def = FPGADef.NewEmpty();
    public string RawConfig
    {
      get => this._def.GetRaw();
      set
      {
        this._def = FPGADef.Parse(value);
        this.Recompile();
        // TODO: mark for network update
      }
    }

    private readonly FPGAGate[] _gates = new FPGAGate[FPGADef.GateCount];

    private IFPGAInput _Input => this.ParentSlot?.Parent as IFPGAInput;

    public void PatchOnLoad()
    {
      this.SlotType = FPGASlotType;
      this.Thumbnail = StationeersModsUtility.FindPrefab("ItemIntegratedCircuit10").Thumbnail;

      FPGAMod.PatchEnumCollection(EnumCollections.SlotClasses, FPGASlotType, "FPGAChip");
    }

    public Vector2? GetUV(GameObject obj)
    {
      if (obj == this.transform.Find("BasicFPGAChip_pins/default").gameObject)
      {
        return FPGAMod.UVTile(64, 3, 7); // roughly match ic10 pins
      }
      return null;
    }

    public Localization.LocalizationThingDat GetLocalization()
    {
      return new Localization.LocalizationThingDat
      {
        PrefabName = "FPGA Chip",
        Description = ""
        + "The Field-Programmable Gate Array contains 64 configurable gates to automate calculations. "
        + "The gates can be configured by the {THING:MotherboardFPGA}, or through logic when placed in a {THING:StructureBasicFPGALogicHousing}."
      };
    }

    public override ThingSaveData SerializeSave()
    {
      var saveData = new BasicFPGAChipSaveData();
      var baseData = saveData as ThingSaveData;
      this.InitialiseSaveData(ref baseData);
      return saveData;
    }

    public override void DeserializeSave(ThingSaveData saveData)
    {
      base.DeserializeSave(saveData);
      this.RawConfig = (saveData as BasicFPGAChipSaveData)?.RawConfig;
    }

    protected override void InitialiseSaveData(ref ThingSaveData savedData)
    {
      base.InitialiseSaveData(ref savedData);
      if (savedData is BasicFPGAChipSaveData fpgaData)
      {
        fpgaData.RawConfig = this.RawConfig;
      }
    }

    public int GetStackSize()
    {
      return FPGADef.AddressCount;
    }

    public double ReadMemory(int address)
    {
      if (address < 0)
      {
        throw new StackUnderflowException();
      }
      if (address > 255)
      {
        throw new StackOverflowException();
      }
      var addr = (byte)address;
      if (FPGADef.IsIOAddress(addr))
      {
        return this.ReadGateValue(addr);
      }
      if (FPGADef.IsGateAddress(addr) || FPGADef.IsLutAddress(addr))
      {
        return this._def.ReadRawValue(addr);
      }
      throw new StackOverflowException();
    }

    public void ClearMemory()
    {
      this.RawConfig = "";
    }

    public void WriteMemory(int address, double value)
    {
      if (address < 0)
      {
        throw new StackUnderflowException();
      }
      if (address > 255)
      {
        throw new StackUnderflowException();
      }
      var addr = (byte)address;
      if (FPGADef.IsIOAddress(addr))
      {
        throw new StackUnderflowException();
      }
      else if (FPGADef.IsGateAddress(addr))
      {
        this._def.SetGateRaw(addr, ProgrammableChip.DoubleToLong(value, true));
        this.Recompile();
      }
      else if (FPGADef.IsLutAddress(addr))
      {
        this._def.SetLutValue(addr, value);
        this.Recompile();
      }
      else
      {
        throw new StackOverflowException();
      }
    }

    private double ReadGateValue(int index)
    {
      return this._gates[index].Eval(this._Input);
    }

    private void Recompile()
    {
      FPGAGate.Compile(this._def, this._gates);
    }
  }
}
