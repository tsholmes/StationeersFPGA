using System.Collections;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
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
    private Slot _FPGASlot => this.Slots[0];
    private BasicFPGAChip FPGAChip => this._FPGASlot.Get<BasicFPGAChip>();

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
      if (index < 0 || index > FPGADef.InputCount)
      {
        return double.NaN;
      }
      // TODO
      return double.NaN;
    }

    public long GetFPGAInputModCount()
    {
      // TODO
      return 0;
    }

    public BasicFPGAChip GetFPGAChip()
    {
      return this.FPGAChip;
    }
  }
}
