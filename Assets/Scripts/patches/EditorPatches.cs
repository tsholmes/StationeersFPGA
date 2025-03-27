using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.UI;
using Assets.Scripts.UI.ImGuiUi;
using HarmonyLib;
using UnityEngine;

namespace fpgamod
{
  public static class EditorPatches
  {
    [HarmonyPatch(typeof(ImguiCreativeSpawnMenu))]
    [HarmonyPatch(nameof(ImguiCreativeSpawnMenu.Draw))]
    class ImguiCreativeSpawnMenuDrawPatch
    {
      static void Postfix()
      {
        ImGuiFPGAEditor.Draw();
      }
    }

    [HarmonyPatch(typeof(InputWindowBase))]
    class InputWindowBasePatch
    {
      [HarmonyPostfix]
      [HarmonyPatch(nameof(InputWindowBase.IsInputWindow), MethodType.Getter)]
      static void IsInputWindow(ref bool __result)
      {
        __result = __result || ImGuiFPGAEditor.Show;
      }

      [HarmonyPostfix]
      [HarmonyPatch(nameof(InputWindowBase.Cancel))]
      static void Cancel()
      {
        ImGuiFPGAEditor.HideEditor();
      }
    }
  }
}