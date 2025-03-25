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
    ILogicStack,
    IInstructable,
    IPatchOnLoad,
    ICustomUV
  {
    public const int INPUT_COUNT = 64;
    public const int GATE_COUNT = 64;
    public const int LUT_COUNT = 64;
    public static readonly FPGAGate.MemoryMapping MEMORY_MAPPING = new(INPUT_COUNT, GATE_COUNT, LUT_COUNT);

    public const Slot.Class FPGASlotType = (Slot.Class)0x69;

    private readonly LogicStack _stack = new LogicStack(GATE_COUNT + LUT_COUNT);
    private readonly FPGAGate[] _gates = new FPGAGate[GATE_COUNT];

    private IFPGAInput _Input => this.ParentSlot?.Parent as IFPGAInput;

    public void PatchOnLoad()
    {
      this.SlotType = FPGASlotType;
      this.Thumbnail = StationeersModsUtility.FindPrefab("ItemIntegratedCircuit10").Thumbnail;
    }

    public Vector2? GetUV(GameObject obj)
    {
      if (obj == this.transform.Find("BasicFPGAChip_pins/default").gameObject) {
        return FPGAMod.UVTile(64, 3, 7); // roughly match ic10 pins
      }
      return null;
    }

    public override void DeserializeSave(ThingSaveData saveData)
    {
      base.DeserializeSave(saveData);
      this.Recompile();
    }

    public int GetStackSize()
    {
      return GATE_COUNT + LUT_COUNT;
    }

    public double ReadMemory(int address)
    {
      var addr = MEMORY_MAPPING.LookupRead(address);
      if (address < 0)
      {
        throw new StackUnderflowException();
      }
      switch (addr.Section)
      {
        case FPGAGate.AddressSection.IO:
          return this.ReadGateValue(addr.Offset);
        case FPGAGate.AddressSection.Gate | FPGAGate.AddressSection.LUT:
          return this._stack[address - MEMORY_MAPPING.GateOffset];
        default:
          throw new StackOverflowException();
      }
    }

    public void ClearMemory()
    {
      this._stack.Clear();
      this.Recompile();
    }

    public void WriteMemory(int address, double value)
    {
      if (address < MEMORY_MAPPING.GateOffset)
      {
        throw new StackUnderflowException();
      }
      if (address >= MEMORY_MAPPING.TotalSize)
      {
        throw new StackOverflowException();
      }
      this._stack[address - MEMORY_MAPPING.GateOffset] = value;
      this.Recompile();
    }

    public LogicStack GetLogicStack()
    {
      return _stack;
    }

    public IEnumCollection GetInstructions()
    {
      return FPGAGate.Instructions;
    }

    public string GetInstructionDescription(int i)
    {
      // TODO
      return "";
    }

    private double ReadGateValue(int index)
    {
      return this._gates[index].Eval(this._Input);
    }

    private void Recompile()
    {
      FPGAGate.Compile(this._stack, MEMORY_MAPPING, this._gates);
    }
  }
}
