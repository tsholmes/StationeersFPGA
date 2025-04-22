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
  public class FPGALogicHousing :
    LogicUnitBase,
    IMemory,
    IMemoryReadable,
    IMemoryWritable,
    IFPGAInput,
    IPatchOnLoad,
    ICustomUV,
    IFPGAHolder
  {
    private readonly double[] _inputValues = new double[FPGADef.InputCount];
    private long _inputModCount = 0;

    private Slot _FPGASlot => this.Slots[0];
    private FPGAChip FPGAChip => this._FPGASlot.Get<FPGAChip>();

    public void PatchOnLoad()
    {
      this.BuildStates[0].Tool.ToolExit = StationeersModsUtility.FindTool(StationeersTool.DRILL);
      this._FPGASlot.Type = FPGAChip.FPGASlotType;
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
      if (obj == this.transform.Find("FPGAHousing_logicbase/default").gameObject)
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
      if (obj == this.transform.Find("FPGAHousing_pins/default").gameObject) {
        return FPGAMod.UVTile(64, 3, 7);
      }
      return null;
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
      return this.FPGAChip.ReadMemory(address, this);
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

    public FPGAChip GetFPGAChip()
    {
      return this.FPGAChip;
    }
  }
}
