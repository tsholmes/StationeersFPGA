using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts;
using System;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;

namespace fpgamod
{
  public class FPGAChip :
    Item,
    IPatchOnLoad,
    ICustomUV,
    ISourceCode
  {
    public const Slot.Class FPGASlotType = (Slot.Class)0x69;
    public const string FPGASlotTypeName = "FPGAChip";

    private const ushort FLAG_RAWCONFIG = 256;

    private FPGADef _def = FPGADef.NewEmpty();
    public string RawConfig
    {
      get => this._def.GetRaw();
      set
      {
        this._def = FPGADef.Parse(value);
        this.Recompile();
        this.SendUpdate();
      }
    }

    private readonly FPGAGate[] _gates = new FPGAGate[FPGADef.GateCount];

    public void PatchOnLoad()
    {
      this.SlotType = FPGASlotType;

      FPGAMod.PatchEnumCollection(EnumCollections.SlotClasses, FPGASlotType, FPGASlotTypeName);
    }

    public Vector2? GetUV(GameObject obj)
    {
      if (obj == this.transform.Find("FPGAChip_pins/default").gameObject)
      {
        return FPGAMod.UVTile(64, 3, 7); // roughly match ic10 pins
      }
      return null;
    }

    public override ThingSaveData SerializeSave()
    {
      var saveData = new FPGAChipSaveData();
      var baseData = saveData as ThingSaveData;
      this.InitialiseSaveData(ref baseData);
      return saveData;
    }

    public override void DeserializeSave(ThingSaveData saveData)
    {
      base.DeserializeSave(saveData);
      this.RawConfig = (saveData as FPGAChipSaveData)?.RawConfig;
    }

    protected override void InitialiseSaveData(ref ThingSaveData savedData)
    {
      base.InitialiseSaveData(ref savedData);
      if (savedData is FPGAChipSaveData fpgaData)
      {
        fpgaData.RawConfig = this.RawConfig;
      }
    }

    public override void Awake()
    {
      base.Awake();
      this.Recompile();
    }

    public int GetStackSize()
    {
      return FPGADef.AddressCount;
    }

    public double ReadMemory(int address, IFPGAInput input)
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
        return this.ReadGateValue(addr, input);
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

    private double ReadGateValue(int index, IFPGAInput input)
    {
      return this._gates[index].Eval(input);
    }

    private void Recompile()
    {
      FPGAGate.Compile(this._def, this._gates);
    }

    public override Thing.DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
    {
      if (attack.SourceItem is not Labeller labeller)
      {
        return base.AttackWith(attack, doAction);
      }
      var action = new Thing.DelayedActionInstance()
      {
        Duration = 0f,
        ActionMessage = ActionStrings.Rename
      };
      if (!labeller.OnOff)
      {
        return action.Fail(GameStrings.DeviceNotOn);
      }
      if (!labeller.IsOperable)
      {
        return action.Fail(GameStrings.DeviceNoPower);
      }
      if (!doAction)
      {
        return action;
      }
      labeller.Rename(this);
      return action;
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

    char[] ISourceCode.SourceCodeCharArray { get; set; }
    int ISourceCode.SourceCodeWritePointer { get; set; }

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
      this._def = FPGADef.Parse(sourceCode);
      this.Recompile();
    }

    public AsciiString GetSourceCode()
    {
      return AsciiString.Parse(this._def.GetRaw());
    }

    public string GetInputLabel(int index) => this._def.GetLabel((byte)index);
  }
}
