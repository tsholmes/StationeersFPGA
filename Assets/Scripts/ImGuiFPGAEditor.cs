using System;
using Assets.Scripts;
using Assets.Scripts.UI;
using BepInEx;
using ImGuiNET;
using UnityEngine;

namespace fpgamod
{
  public static class ImGuiFPGAEditor
  {
    private const ImGuiWindowFlags flags = 0
      | ImGuiWindowFlags.NoSavedSettings
      | ImGuiWindowFlags.NoMove
      | ImGuiWindowFlags.NoResize
      | ImGuiWindowFlags.NoCollapse;

    private static bool _show = false;
    public static bool Show => _show;
    public static GameObject UIBlocker;
    private static Vector2 _size = Vector2.zero;
    private static Vector2 _pos = Vector2.one * 50;

    // custom colors
    private static Vector4[] _colors;
    private static Vector4[] _activeGridColors;
    private static uint _openBorderColor;

    // editor state
    private static FPGAMotherboard Motherboard = null;
    private static FPGADef Def = null;
    private static ulong _inputOpen = 0;
    private static ulong _gateOpen = 0;
    private static ulong _lutOpen = 0;
    private static int _highlightIndex = -1;

    public static void ShowEditor(FPGAMotherboard motherboard)
    {
      Motherboard = motherboard;
      Def = new FPGADef(); // TODO: actually save/load this
      _show = true;
      InputMouse.SetMouseControl(true);
      UIBlocker.SetActive(true);
      CursorManager.SetCursor(false);
    }

    public static void HideEditor()
    {
      _show = false;
      InputMouse.SetMouseControl(true);
      UIBlocker.SetActive(false);
      CursorManager.Instance?.OnApplicationFocus(true);
      Motherboard = null;
      Def = null;
    }

    private static void UpdateSize()
    {
      _size.x = Screen.width - 100;
      _size.y = Screen.height - 100;
    }

    private static void PushColors()
    {
      if (_colors == null)
      {
        _colors = new Vector4[(int)ImGuiCol.COUNT];
        for (var i = 0; i < (int)ImGuiCol.COUNT; i++)
        {
          _colors[i] = ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32((ImGuiCol)i));
          // premultiply alpha and set to opaque if not fully transparent
          _colors[i] = _colors[i] * _colors[i][3];
          if (_colors[i][3] != 0f)
          {
            _colors[i][3] = 1f;
          }
        }
        _activeGridColors = new Vector4[] {
          HSVColor(170/360f, 0.75f, 0.4f), // button
          HSVColor(170/360f, 0.75f, 1f), // buttonhovered
          HSVColor(170/360f, 0.95f, 1f), // buttonactive
        };
        _openBorderColor = ImGui.ColorConvertFloat4ToU32(HSVColor(170 / 360f, 1f, 1f));
      }
      for (var i = (ImGuiCol)0; i < ImGuiCol.COUNT; i++)
      {
        ImGui.PushStyleColor(i, _colors[(int)i]);
      }
    }

    private static Vector4 HSVColor(float h, float s, float v)
    {
      var color = Vector4.one;
      ImGui.ColorConvertHSVtoRGB(h, s, v, out color.x, out color.y, out color.z);
      return color;
    }

    private static void PopColors()
    {
      ImGui.PopStyleColor((int)ImGuiCol.COUNT);
    }

    public static void Draw()
    {
      if (!_show)
      {
        return;
      }
      UpdateSize();
      Localization.PushFont();
      PushColors();

      ImGui.SetNextWindowFocus();
      ImGui.Begin("FPGA Editor", ref _show, flags);
      if (!_show)
      {
        HideEditor(); // if we exited, hide
      }
      else
      {
        ImGui.SetWindowSize(_size);
        ImGui.SetWindowPos(_pos);

        var area = ImGui.GetContentRegionAvail();
        var gridSize = Math.Min(area.x / 16f, area.y / 27f);
        ImGui.BeginTable("top_layout", 2, ImGuiTableFlags.SizingFixedFit);
        ImGui.TableSetupColumn("");
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextColumn();
        DrawInputGrid(gridSize);
        DrawGateGrid(gridSize);
        DrawLutGrid(gridSize);

        ImGui.TableNextColumn();
        if (ImGui.BeginTabBar("FPGATabs"))
        {
          if (ImGui.BeginTabItem("Editor"))
          {
            if (ImGui.BeginChild("EditorChild"))
            {
              DrawEditorInputs();
              DrawEditorGates();
              DrawEditorLUTs();
              ImGui.EndChild();
            }
            ImGui.EndTabItem();
          }
          ImGui.EndTabBar();
        }

        ImGui.EndTable();
      }
      Localization.PopFont();
      PopColors();
      ImGui.End();
    }

    private static string[] _indexText;
    static ImGuiFPGAEditor()
    {
      _indexText = new string[64];
      for (var i = 0; i < 64; i++)
      {
        _indexText[i] = $"{i:D2}";
      }
    }

    private static void DrawInputGrid(float size)
    {
      DrawGrid(size, "Inputs", "inputTable", FPGADef.InputNames, Def.InputLabels, ref _inputOpen);
    }

    private static void DrawGateGrid(float size)
    {
      DrawGrid(size, "Gates", "gateTable", FPGADef.GateNames, Def.GateLabels, ref _gateOpen);
    }

    private static void DrawLutGrid(float size)
    {
      DrawGrid(size, "Lookup Table", "lutTable", FPGADef.LutNames, Def.LutLabels, ref _lutOpen);
    }

    private static void DrawGrid(float size, string title, string id, string[] defLabels, string[] labels, ref ulong open)
    {
      var vecSize = Vector2.one * size;
      ImGui.Text(title);
      ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, Vector2.zero);
      ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.one);
      ImGui.BeginTable(id, 8, new Vector2(size*8, 0));
      for (var col = 0; col < 8; col++)
      {
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, size);
      }
      for (var i = 0; i < 64; i++)
      {
        var col = i % 8;
        if (col == 0)
        {
          ImGui.TableNextRow(0, size);
        }
        ImGui.TableSetColumnIndex(col);
        var srcLabel = labels[i];
        var label = srcLabel.IsNullOrWhiteSpace() ? _indexText[i] : srcLabel;
        var tooltip = srcLabel.IsNullOrWhiteSpace() ? defLabels[i] : srcLabel;
        var isOpen = (open & (1ul << i)) != 0;
        if (isOpen)
        {
          ImGui.PushStyleColor(ImGuiCol.Button, _activeGridColors[0]);
          ImGui.PushStyleColor(ImGuiCol.ButtonHovered, _activeGridColors[1]);
          ImGui.PushStyleColor(ImGuiCol.ButtonActive, _activeGridColors[2]);
        }
        var textWidth = ImGui.CalcTextSize(label).x;
        ImGui.SetWindowFontScale(Math.Min(1f, (size - 2) / textWidth));
        if (ImGui.Button(label, vecSize))
        {
          open ^= 1ul << i;
        }
        ImGui.SetWindowFontScale(1f);
        if (ImGui.IsItemHovered())
        {
          ImGui.SetTooltip(tooltip);
        }
        if (isOpen)
        {
          var rmin = ImGui.GetItemRectMin();
          var rmax = rmin + vecSize - Vector2.one;
          ImGui.GetWindowDrawList().AddRect(rmin, rmax, _openBorderColor);
          ImGui.PopStyleColor(3);
        }
      }
      ImGui.EndTable();
      ImGui.PopStyleVar(2);
    }

    private static void DrawEditorInputs()
    {
      if (Def == null || _inputOpen == 0)
      {
        return;
      }
      ImGui.Separator();
      ImGui.Text("Inputs");
      ImGui.PushID("##input");
      for (var i = 0; i < 64; i++)
      {
        if ((_inputOpen & (1ul << i)) != 0)
        {
          DrawEditorInput(i);
        }
      }
      ImGui.PopID();
    }

    private static void DrawEditorInput(int index)
    {
      ImGui.PushID(index);
      ImGui.BeginGroup();
      ImGui.Text(_indexText[index]);
      ImGui.SameLine(); ImGui.InputTextWithHint("##label", FPGADef.InputNames[index], ref Def.InputLabels[index], 128u);
      ImGui.EndGroup();
      ImGui.PopID();
    }

    private static void DrawEditorGates()
    {
      if (Def == null || _gateOpen == 0)
      {
        return;
      }
      ImGui.Separator();
      ImGui.Text("Gates");
      ImGui.PushID("##gate");
      for (var i = 0; i < 64; i++)
      {
        if ((_gateOpen & (1ul << i)) != 0)
        {
          DrawEditorGate(i);
        }
      }
      ImGui.PopID();
    }

    private static void DrawEditorGate(int index)
    {
      ImGui.PushID(index);
      ImGui.BeginGroup();
      ImGui.Text(_indexText[index]);
      ImGui.SameLine(); ImGui.InputTextWithHint("##label", FPGADef.GateNames[index], ref Def.GateLabels[index], 128u);
      ImGui.EndGroup();
      ImGui.PopID();
    }

    private static void DrawEditorLUTs()
    {
      if (Def == null || _lutOpen == 0)
      {
        return;
      }
      ImGui.Separator();
      ImGui.Text("Lookup Table");
      ImGui.PushID("##lut");
      for (var i = 0; i < 64; i++)
      {
        if ((_lutOpen & (1ul << i)) != 0)
        {
          DrawEditorLUT(i);
        }
      }
      ImGui.PopID();
    }

    private static void DrawEditorLUT(int index)
    {
      ImGui.PushID(index);
      ImGui.Text(_indexText[index]);
      ImGui.SameLine(); ImGui.InputTextWithHint("##label", FPGADef.LutNames[index], ref Def.LutLabels[index], 128u);
      ImGui.SameLine(); ImGui.InputDouble("##value", ref Def.LutValues[index], "%f");
      ImGui.PopID();
    }
  }
}