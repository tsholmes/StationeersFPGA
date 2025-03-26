using Assets.Scripts;
using Assets.Scripts.UI;
using ImGuiNET;
using UnityEngine;

namespace fpgamod
{
  public static class ImGuiFPGAEditor
  {
    private const ImGuiWindowFlags flags = 0
      | ImGuiWindowFlags.NoMove
      | ImGuiWindowFlags.NoResize
      | ImGuiWindowFlags.NoCollapse;

    private static bool _show = false;
    public static bool Show => _show;
    public static FPGAMotherboard Motherboard = null;
    private static Vector2 _size = Vector2.zero;
    private static Vector2 _pos = Vector2.one * 50;
    private static Vector4 _bgColor = new Vector4(8, 9, 8, 255) / 255f;

    public static void ShowEditor(bool show)
    {
      _show = show;
      if (show)
      {
        InputMouse.SetMouseControl(true);
        CursorManager.SetCursor(false);
      }
      else
      {
        InputMouse.SetMouseControl(false);
        CursorManager.Instance?.OnApplicationFocus(true);
        Motherboard = null;
      }
    }

    private static void UpdateSize()
    {
      _size.x = Screen.width - 100;
      _size.y = Screen.height - 100;
    }

    public static void Draw()
    {
      if (!_show)
      {
        return;
      }
      Localization.PushFont();
      ImGui.PushStyleColor(ImGuiCol.WindowBg, _bgColor);
      ImGui.Begin("FPGA Editor", ref _show, flags);
      ImGui.PopStyleColor();
      if (!_show)
      {
        ShowEditor(false); // if we exited, hide
      }
      else
      {
        UpdateSize();
        ImGui.SetWindowSize(_size);
        ImGui.SetWindowPos(_pos);

        if (ImGui.BeginTabBar("FPGATabs"))
        {
          if (ImGui.BeginTabItem("Inputs"))
          {
            ImGui.EndTabItem();
          }
          if (ImGui.BeginTabItem("Gates"))
          {
            ImGui.EndTabItem();
          }
          if (ImGui.BeginTabItem("LUT"))
          {
            ImGui.EndTabItem();
          }
          ImGui.EndTabBar();
        }
      }
      Localization.PopFont();
      ImGui.End();
    }
  }
}