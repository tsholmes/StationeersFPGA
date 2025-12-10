using System;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;

namespace fpgamod
{
  public interface IFPGAHolder : ILogicable
  {
    public FPGAChip GetFPGAChip();
  }

  public class FPGAHolderChipStackAdapter : IFPGAHolder
  {
    private readonly ILogicable chipStack;
    private readonly ILogicable rack;
    private readonly Func<Slot> getSlot;
    private readonly int rackIndex;

    public FPGAHolderChipStackAdapter(ILogicable chipStack, ILogicable rack)
    {
      this.chipStack = chipStack;
      this.rack = rack;
      var getMethod = rack.GetType().GetProperty("Slot").GetGetMethod();
      getSlot = (Func<Slot>)getMethod.CreateDelegate(typeof(Func<Slot>), rack);

      rackIndex = (int)rack.GetType().GetProperty("RackIndex").GetValue(rack);
    }

    // these are the only methods used by the FPGA Motherboard
    public string DisplayName => $"{chipStack.DisplayName} d{100 + rackIndex}";
    public FPGAChip GetFPGAChip() => getSlot().Get<FPGAChip>();

    // these are just to satisfy the ILogicable interface
    public int TotalSlots => throw new NotImplementedException();
    public Thing GetAsThing => throw new NotImplementedException();
    public bool HasAnySlots => throw new NotImplementedException();
    public ushort NetworkUpdateFlags { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public long ReferenceId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public bool BeingDestroyed { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public bool CanLogicRead(LogicType logicType) => throw new NotImplementedException();
    public bool CanLogicRead(LogicSlotType logicSlotType, int slotId) => throw new NotImplementedException();
    public bool CanLogicWrite(LogicType logicType) => throw new NotImplementedException();
    public double GetLogicValue(LogicType logicType) => throw new NotImplementedException();
    public double GetLogicValue(LogicSlotType logicSlotType, int slotId) => throw new NotImplementedException();
    public int GetNameHash() => throw new NotImplementedException();
    public int GetNextSlotId(int slotIndex, bool isForward) => throw new NotImplementedException();
    public int GetPrefabHash() => throw new NotImplementedException();
    public Slot GetSlot(int slotIndex) => throw new NotImplementedException();
    public bool IsLogicReadable() => throw new NotImplementedException();
    public bool IsLogicSlotReadable() => throw new NotImplementedException();
    public bool IsLogicWritable() => throw new NotImplementedException();
    public void OnAssignedReference() => throw new NotImplementedException();
    public void PrintDebugInfo(bool verbose = false) => throw new NotImplementedException();
    public void SetLogicValue(LogicType logicType, double value) => throw new NotImplementedException();
    public string ToTooltip() => throw new NotImplementedException();
  }
}
