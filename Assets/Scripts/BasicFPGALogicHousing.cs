using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using StationeersMods.Interface;
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
    ICustomUV
  {
    private readonly double[] _inputValues = new double[FPGADef.InputCount];
    private long _inputModCount = 0;

    private Slot _FPGASlot => this.Slots[0];
    private BasicFPGAChip FPGAChip => this._FPGASlot.Get<BasicFPGAChip>();

    public void PatchOnLoad()
    {
      this.BuildStates[0].Tool.ToolExit = StationeersModsUtility.FindTool(StationeersTool.DRILL);
      this.Thumbnail = StationeersModsUtility.FindPrefab("StructureCircuitHousing").Thumbnail;
      this._FPGASlot.Type = BasicFPGAChip.FPGASlotType;
    }

    public Vector2? GetUV(GameObject obj)
    {
      if (obj == this.transform.Find("BasicFPGAHousing_base/default").gameObject)
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
  }
}
