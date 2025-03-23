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
    IPatchOnLoad
  {
    private readonly double[] _inputValues = new double[64];

    private Slot _FPGASlot => this.Slots[0];
    private BasicFPGAChip FPGAChip => this._FPGASlot.Get<BasicFPGAChip>();

    public void PatchOnLoad()
    {
      this.BuildStates[0].Tool.ToolExit = StationeersModsUtility.FindTool(StationeersTool.DRILL);
      this.Thumbnail = StationeersModsUtility.FindPrefab("StructureCircuitHousing").Thumbnail;
      this._FPGASlot.Type = BasicFPGAChip.FPGASlotType;
    }

    public void ClearMemory()
    {
      if (this.FPGAChip == null) {
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
      if (this.FPGAChip == null) {
        throw new NullReferenceException();
      }
      return this.FPGAChip.ReadMemory(address);
    }

    public void WriteMemory(int address, double value)
    {
      if (address < 64) {
        if (address < 0) {
          throw new StackUnderflowException();
        }
        if (address > 7) {
          throw new StackOverflowException();
        }
        this._inputValues[address] = value;
        if (this.FPGAChip != null) {
          this.FPGAChip.MarkInputDirty();
        }
        return;
      }
      if (this.FPGAChip == null) {
        throw new NullReferenceException();
      }
      this.FPGAChip.WriteMemory(address, value);
    }

    public double GetFPGAInputPin(int index)
    {
      if (index < 0 || index > 7) {
        return double.NaN;
      }
      return this._inputValues[index];
    }
  }
}
