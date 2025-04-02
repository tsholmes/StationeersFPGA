using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Objects.Electrical;
using StationeersMods.Interface;
using UnityEditor;
using UnityEngine;

namespace fpgamod
{
  public class BasicFPGALogicHousing :
    LogicUnitBase,
    IMemory,
    IMemoryReadable,
    IMemoryWritable,
    IFPGAInput,
    IPatchOnLoad,
    ICustomUV,
    ILocalizedPrefab,
    IFPGAHolder
  {
    private readonly double[] _inputValues = new double[FPGADef.InputCount];
    private long _inputModCount = 0;

    private Slot _FPGASlot => this.Slots[0];
    private BasicFPGAChip FPGAChip => this._FPGASlot.Get<BasicFPGAChip>();

    public void PatchOnLoad()
    {
      this.BuildStates[0].Tool.ToolExit = StationeersModsUtility.FindTool(StationeersTool.DRILL);
      this._FPGASlot.Type = BasicFPGAChip.FPGASlotType;

      FPGAMod.AddLocalizationString("FallbackSlotsName", "FPGAChip", "FpgaChip");

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
      if (obj == this.transform.Find("BasicFPGAHousing_logicbase/default").gameObject)
      {
        return FPGAMod.UVTile(16, 0, 6); // match IC housing base
      }
      if (obj == this.transform.Find("PowerSymbol/default").gameObject)
      {
        return FPGAMod.UVTile(16, 2, 5); // match builtin power symbol
      }
      if (obj == this.transform.Find("DataSymbol/default").gameObject)
      {
        return FPGAMod.UVTile(16, 1, 4); // match builtin data symbol
      }
      if (obj == this.transform.Find("BasicFPGAHousing_pins/default").gameObject) {
        return FPGAMod.UVTile(64, 3, 7);
      }
      return null;
    }

    public Localization.LocalizationThingDat GetLocalization()
    {
      return new Localization.LocalizationThingDat
      {
        PrefabName = "FPGA Logic Housing",
        Description = ""
        + "Holds a {THING:ItemBasicFPGAChip} to be accessed by {THING:ItemIntegratedCircuit10}. "
        + "The chip is memory-mapped allowing read/write access to both configuration and calculations via IC10 get/getd/put/putd instructions.\n"
        + "- Writing to addresses 0-63 sets input values\n"
        + "- Reading from addresses 0-63 reads gate calculation results\n"
        + "- Reading/Writing addresses 64-127 accesses raw gate configuration values (address 64 accesses configuration for gate00)\n"
        + "- Reading/Writing address 128-191 accesses lookup table values (address 128 accesses lut00)\n"
        + "The gate calculations happen continuously, so inputs can be written to and results read multiple times within a logic tick. "
        + "Input values are volatile and should always be written to on the same tick as gate outputs are read."
      };
    }

    public void ClearMemory()
    {
      if (this.FPGAChip == null)
      {
        throw new NullReferenceException();
      }
      this.FPGAChip.ClearMemory();
    }

    public int GetStackSize()
    {
      var chip = this.FPGAChip;
      return chip != null ? chip.GetStackSize() : 0;
    }

    public double ReadMemory(int address)
    {
      if (this.FPGAChip == null)
      {
        throw new NullReferenceException();
      }
      if (address < 0)
      {
        throw new StackUnderflowException();
      }
      if (address > 255)
      {
        throw new StackOverflowException();
      }
      var addr = (byte)address;
      if (FPGADef.IsIOAddress(addr) && (!this.OnOff || !this.Powered)) {
        // we only require power for reading gate outputs. everything else is just configuration.
        return 0;
      }
      return this.FPGAChip.ReadMemory(address);
    }

    public void WriteMemory(int address, double value)
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
        this._inputValues[address] = value;
        this._inputModCount++;
        return;
      }
      if (this.FPGAChip == null)
      {
        throw new NullReferenceException();
      }
      this.FPGAChip.WriteMemory(address, value);
    }

    public double GetFPGAInputPin(int index)
    {
      if (index < 0 || index > FPGADef.InputCount)
      {
        return double.NaN;
      }
      return this._inputValues[index];
    }

    public long GetFPGAInputModCount()
    {
      return this._inputModCount;
    }

    public BasicFPGAChip GetFPGAChip()
    {
      return this.FPGAChip;
    }
  }
}
