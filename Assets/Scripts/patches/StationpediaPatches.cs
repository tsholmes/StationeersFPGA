using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using HarmonyLib;
using UnityEngine;

namespace fpgamod
{
  [HarmonyPatch]
  public static class StationpediaPatches
  {
    [HarmonyPatch(typeof(Stationpedia), "AddSlotInfo")]
    [HarmonyPostfix]
    static void StationpediaAddSlotInfo(Thing prefab, ref StationpediaPage page) {
      if (!prefab.HasAnySlots) {
        return;
      }
      for (var i = 0; i < prefab.Slots.Count; i++) {
        if (prefab.Slots[i].Type == FPGAChip.FPGASlotType) {
          page.SlotInserts[i].SlotType = FPGAChip.FPGASlotTypeName;
        }
      }
    }

    [HarmonyPatch(typeof(Slot), nameof(Slot.PopulateSlotTypeSprites))]
    [HarmonyPostfix]
    static void SlotPopulateSlotTypeSprites() {
      var field = typeof(Slot).GetField("_slotTypeLookup", BindingFlags.Static | BindingFlags.NonPublic);
      var lookup = field.GetValue(null) as Dictionary<int, Sprite>;
      lookup[(int)FPGAChip.FPGASlotType] = lookup[(int)Slot.Class.ProgrammableChip];
    }

    [HarmonyPatch(typeof(Localization), nameof(Localization.GetSlotName))]
    [HarmonyPrefix]
    static bool LocalizationGetSlotName(ref string __result, string slotName) {
      // skip builtin lookup for FPGAChip slot since slot type localizations get overridden
      if (slotName == FPGAChip.FPGASlotTypeName) {
        __result = slotName;
        return false;
      }
      return true;
    }
  }
}
