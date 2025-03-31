using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using BepInEx;
using ImGuiNET;
using UnityEngine;

namespace fpgamod
{
  public static class ImGuiFPGAEditor
  {
    private const ImGuiWindowFlags windowFlags = 0
      | ImGuiWindowFlags.NoSavedSettings
      | ImGuiWindowFlags.NoMove
      | ImGuiWindowFlags.NoResize
      | ImGuiWindowFlags.NoCollapse
      | ImGuiWindowFlags.NoScrollbar
      | ImGuiWindowFlags.NoScrollWithMouse;

    private const ImGuiTableFlags editTableFlags = 0
      | ImGuiTableFlags.NoSavedSettings
      | ImGuiTableFlags.SizingFixedFit
      | ImGuiTableFlags.BordersInner;

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
    private static string _rawDef = "";
    private static string _gateEditSearch = "";

    private static string[] _gridLabels = new string[FPGADef.AddressCount];
    private static string[] _gridLabelsSquare = new string[FPGADef.AddressCount];

    // prebuilt strings
    private static readonly string[] _indexText;
    static ImGuiFPGAEditor()
    {
      _indexText = new string[64];
      for (var i = 0; i < 64; i++)
      {
        _indexText[i] = $"{i:D2}";
      }
      Array.Fill(_gridLabels, "");
      Array.Fill(_gridLabelsSquare, "");
    }

    public static void ShowEditor(FPGAMotherboard motherboard)
    {
      Motherboard = motherboard;
      ReloadConfig();
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
      if (Motherboard != null)
      {
        Motherboard.RawConfig = Def.GetRaw();
      }
      Motherboard = null;
      Def = null;
    }

    private static void ReloadConfig()
    {
      if (Motherboard == null)
      {
        Def = FPGADef.NewEmpty();
        _rawDef = "";
      }
      else
      {
        Def = FPGADef.Parse(Motherboard.RawConfig);
        _rawDef = Def.GetRaw();
      }
    }

    private static void AutoOpen()
    {
      if (Motherboard == null)
      {
        return;
      }
      Motherboard.InputOpen = 0;
      Motherboard.GateOpen = 0;
      Motherboard.LutOpen = 0;
      for (byte addr = 0; addr < FPGADef.AddressCount; addr++)
      {
        if (Def.HasConfig(addr))
        {
          if (FPGADef.IsIOAddress(addr))
          {
            Motherboard.InputOpen |= 1ul << (addr - FPGADef.InputOffset);
          }
          if (FPGADef.IsGateAddress(addr))
          {
            Motherboard.GateOpen |= 1ul << (addr - FPGADef.GateOffset);
          }
          if (FPGADef.IsLutAddress(addr))
          {
            Motherboard.LutOpen |= 1ul << (addr - FPGADef.LutOffset);
          }
        }
      }
    }

    private static void UpdateSize()
    {
      _size.x = Screen.width - 100;
      _size.y = Screen.height - 100;
    }

    private static void PushDefaultStyle()
    {
      if (_colors == null)
      {
        // initialize style colors
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
      ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(3, 2));
      ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(3, 2));
    }

    private static void PopDefaultStyle()
    {
      ImGui.PopStyleVar(2);
      ImGui.PopStyleColor((int)ImGuiCol.COUNT);
    }

    private static Vector4 HSVColor(float h, float s, float v)
    {
      var color = Vector4.one;
      ImGui.ColorConvertHSVtoRGB(h, s, v, out color.x, out color.y, out color.z);
      return color;
    }

    public static void Draw()
    {
      if (!_show)
      {
        return;
      }
      UpdateSize();
      Localization.PushFont();
      PushDefaultStyle();

      ImGui.Begin("FPGA Editor", ref _show, windowFlags);
      if (!_show)
      {
        HideEditor(); // if we exited, hide
      }
      else
      {
        ImGui.SetWindowSize(_size);
        ImGui.SetWindowPos(_pos);

        DrawTopBar();

        var area = ImGui.GetContentRegionAvail();
        var gridSize = Math.Min(area.x / 16f, area.y / 27f);
        if (ImGui.BeginTable("top_layout", 2, ImGuiTableFlags.SizingFixedFit))
        {
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
              ImGui.BeginChild("EditorChild");
              DrawEditors();
              ImGui.EndChild();
              ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Raw"))
            {
              ImGui.BeginChild("RawChild");
              DrawRawEditor();
              ImGui.EndChild();
              ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
          }

          ImGui.EndTable();
        }
      }
      Localization.PopFont();
      PopDefaultStyle();
      ImGui.End();
    }

    private static void DrawTopBar()
    {
      var region = ImGui.GetContentRegionAvail();
      ImGui.PushID("topBar");
      ImGui.SetNextItemWidth(region.x * 0.25f);
      if (ImGui.BeginCombo("##holderCombo", Motherboard.GetSelectedFPGAHolderName()))
      {
        for (var i = 0; i < Motherboard.ConnectedFPGAHolders.Count; i++)
        {
          var name = Motherboard.ConnectedFPGAHolders[i].DisplayName;
          var selected = i == Motherboard.SelectedHolderIndex;
          ImGui.PushID(i);
          if (ImGui.Selectable(name, selected))
          {
            Motherboard.SelectedHolderIndex = i;
          }
          ImGui.PopID();
        }
        ImGui.EndCombo();
      }
      ImGui.SameLine();
      if (ImGui.Button("Import"))
      {
        Motherboard.Import();
        ReloadConfig();
        AutoOpen();
      }
      ItemTooltip("Warning: this will overwrite editor contents");
      ImGui.SameLine();
      if (ImGui.Button("Export"))
      {
        Motherboard.RawConfig = Def.GetRaw();
        Motherboard.Export();
      }
      ImGui.SameLine(region.x * 0.75f);
      ImGui.SetNextItemWidth(region.x * 0.25f);
      if (ImGui.BeginCombo("##exampleCombo", "Examples"))
      {
        foreach (var example in FPGAExamples.Examples)
        {
          if (ImGui.Selectable(example.Title))
          {
            Def = FPGADef.Parse(example.RawConfig);
            _rawDef = example.RawConfig;
          }
          if (!string.IsNullOrEmpty(example.Tooltip))
          {
            ItemTooltip(example.Tooltip);
          }
        }
        ImGui.EndCombo();
      }
      ItemTooltip("Warning: this will overwrite editor contents");
      ImGui.PopID();
    }

    private static void DrawInputGrid(float size)
    {
      var open = Motherboard.InputOpen;
      DrawGrid(size, "Inputs", "inputTable", 0, ref open);
      Motherboard.InputOpen = open;
    }

    private static void DrawGateGrid(float size)
    {
      var open = Motherboard.GateOpen;
      DrawGrid(size, "Gates", "gateTable", 64, ref open);
      Motherboard.GateOpen = open;
    }

    private static void DrawLutGrid(float size)
    {
      var open = Motherboard.LutOpen;
      DrawGrid(size, "Lookup Table", "lutTable", 128, ref open);
      Motherboard.LutOpen = open;
    }

    private static void DrawGrid(float size, string title, string id, byte addressOffset, ref ulong open)
    {
      var vecSize = Vector2.one * size;
      ImGui.Text(title);
      ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, Vector2.zero);
      ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.one);
      if (ImGui.BeginTable(id, 8, new Vector2(size * 8, 0)))
      {
        for (var col = 0; col < 8; col++)
        {
          ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, size);
        }
        for (var i = 0; i < 64; i++)
        {
          ImGui.PushID(i);
          var col = i % 8;
          if (col == 0)
          {
            ImGui.TableNextRow(0, size);
          }
          ImGui.TableSetColumnIndex(col);
          var address = (byte)(i + addressOffset);
          var srcLabel = Def.GetLabel(address, nameFallback: false);
          var label = srcLabel == "" ? _indexText[i] : srcLabel;
          var tooltip = Def.GetLabel(address);
          var sqlabel = GetGridLabel(address, label, size - 2);
          var isOpen = (open & (1ul << i)) != 0;
          var hasConfig = Def.HasConfig(address);
          if (hasConfig)
          {
            ImGui.PushStyleColor(ImGuiCol.Button, _activeGridColors[0]);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, _activeGridColors[1]);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, _activeGridColors[2]);
          }
          var textSize = ImGui.CalcTextSize(sqlabel);
          ImGui.SetWindowFontScale(Math.Min(1f, (size - 2) / Math.Max(textSize.x, textSize.y)));
          if (ImGui.Button(sqlabel, vecSize))
          {
            open ^= 1ul << i;
          }
          ImGui.SetWindowFontScale(1f);
          ItemTooltip(tooltip);
          if (isOpen)
          {
            var rmin = ImGui.GetItemRectMin();
            var rmax = rmin + vecSize - Vector2.one;
            ImGui.GetWindowDrawList().AddRect(rmin, rmax, _openBorderColor);
          }
          if (hasConfig)
          {
            ImGui.PopStyleColor(3);
          }
          if (ImGui.BeginDragDropSource())
          {
            ImGui.SetDragDropPayload<byte>("gate_input", address);
            ImGui.Text(label);
            ImGui.EndDragDropSource();
          }
          ImGui.PopID();
        }
        ImGui.EndTable();
      }
      ImGui.PopStyleVar(2);
    }

    private static void DrawEditors()
    {
      if ((Motherboard.InputOpen | Motherboard.GateOpen | Motherboard.LutOpen) == 0)
      {
        ImGui.BeginDisabled();
        ImGui.Text("Select a configuration in the grid to edit");
        ImGui.EndDisabled();
      }
      else
      {
        DrawEditorInputs();
        DrawEditorGates();
        DrawEditorLUTs();
      }
    }

    private static void DrawEditorInputs()
    {
      if (Def == null || Motherboard.InputOpen == 0)
      {
        return;
      }
      if (ImGui.CollapsingHeader("Inputs", ImGuiTreeNodeFlags.DefaultOpen) &&
          ImGui.BeginTable("inputEditTable", 2, editTableFlags))
      {
        ImGui.TableSetupColumn("");
        ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();
        for (var i = 0; i < 64; i++)
        {
          if ((Motherboard.InputOpen & (1ul << i)) != 0)
          {
            ImGui.TableNextRow();
            DrawEditorInput(i);
          }
        }
        ImGui.EndTable();
      }
    }

    private static void DrawEditorInput(int index)
    {
      var address = (byte)index;
      ImGui.PushID(index);
      {
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.Text(_indexText[index]);
      }
      {
        var label = Def.GetLabel(address, nameFallback: false);
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-float.Epsilon);
        if (ImGui.InputTextWithHint("##label", FPGADef.InputNames[index], ref label, 32u, ImGuiInputTextFlags.CharsNoBlank))
        {
          Def.SetLabel(address, label);
        }
      }
      ImGui.PopID();
    }

    private static void DrawEditorGates()
    {
      if (Def == null || Motherboard.GateOpen == 0)
      {
        return;
      }
      if (ImGui.CollapsingHeader("Gates", ImGuiTreeNodeFlags.DefaultOpen) &&
          ImGui.BeginTable("gateEditTable", 5, editTableFlags))
      {
        ImGui.TableSetupColumn("");
        ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Op", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Input 1", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Input 2", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();
        for (var i = 0; i < 64; i++)
        {
          if ((Motherboard.GateOpen & (1ul << i)) != 0)
          {
            ImGui.TableNextRow();
            DrawEditorGate(i);
          }
        }
        ImGui.EndTable();
      }
    }

    private static void DrawEditorGate(int index)
    {
      var address = (byte)(index + 64);
      var currentOp = Def.GetGateOp(address);
      var currentInfo = FPGAOps.GetOpInfo(currentOp);

      ImGui.PushID(index);
      {
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.Text(_indexText[index]);
      }
      {
        var label = Def.GetLabel(address, nameFallback: false);
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-float.Epsilon);
        if (ImGui.InputTextWithHint("##label", FPGADef.GateNames[index], ref label, 32u, ImGuiInputTextFlags.CharsNoBlank))
        {
          Def.SetLabel(address, label);
        }
      }
      {
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-float.Epsilon);
        if (ImGui.BeginCombo("##gateOpCombo", currentInfo.Symbol))
        {
          ImGui.SetNextItemWidth(-float.Epsilon);
          var pickFirst = ImGui.InputTextWithHint("##opsearch", "search", ref _gateEditSearch, 6u, ImGuiInputTextFlags.CharsNoBlank | ImGuiInputTextFlags.EnterReturnsTrue);
          if (ImGui.IsWindowAppearing())
          {
            ImGui.SetKeyboardFocusHere(-1);
          }
          var searchLower = _gateEditSearch.ToLower();
          var hasFound = false;
          for (var op = FPGAOp.None; op < FPGAOps.Count; op++)
          {
            var info = FPGAOps.GetOpInfo(op);
            if (info.Symbol.StartsWith(searchLower) || info.Hint.Contains(searchLower))
            {
              hasFound = true;
              if (ImGui.Selectable(info.Symbol, op == currentOp) || pickFirst)
              {
                pickFirst = false;
                Def.SetGateOp(address, op);
                ImGui.CloseCurrentPopup();
              }
              ItemTooltip(info.Hint);
            }
          }
          if (!hasFound)
          {
            ImGui.BeginDisabled();
            ImGui.Text("no matching ops");
            ImGui.EndDisabled();
            if (pickFirst)
            {
              ImGui.CloseCurrentPopup();
            }
          }
          ImGui.EndCombo();
        }
        ItemTooltip(currentInfo.Hint);
        if (ImGui.IsItemDeactivated())
        {
          _gateEditSearch = "";
        }
      }
      {
        ImGui.TableNextColumn();
        ImGui.PushID("input1");
        if (currentInfo.Operands > 0)
        {
          DrawEditorGateInput(address, false);
        }
        else
        {
          ImGui.Text("-");
        }
        ImGui.PopID();
      }
      {
        ImGui.TableNextColumn();
        ImGui.PushID("input2");
        if (currentInfo.Operands > 1)
        {
          DrawEditorGateInput(address, true);
        }
        else
        {
          ImGui.Text("-");
        }
        ImGui.PopID();
      }
      ImGui.PopID();
    }

    private static void DrawEditorGateInput(byte address, bool isInput2)
    {
      ImGui.SetNextItemWidth(-float.Epsilon);
      var curInput = isInput2 ? Def.GetGateInput2(address) : Def.GetGateInput1(address);
      if (ImGui.BeginCombo("##gateinputcombo", Def.GetLabel(curInput)))
      {
        ImGui.SetNextItemWidth(-float.Epsilon);
        var pickFirst = ImGui.InputTextWithHint("##inputsearch", "search", ref _gateEditSearch, 32u, ImGuiInputTextFlags.CharsNoBlank | ImGuiInputTextFlags.EnterReturnsTrue);
        if (ImGui.IsWindowAppearing())
        {
          ImGui.SetKeyboardFocusHere(-1);
        }
        var searchLower = _gateEditSearch.ToLower();
        var hasFound = false;
        for (byte inputAddress = 0; inputAddress < 192; inputAddress++)
        {
          var label = Def.GetLabel(inputAddress);
          var name = FPGADef.GetName(inputAddress);

          if (label.ToLower().Contains(searchLower) || name.Contains(searchLower))
          {
            hasFound = true;
            if (ImGui.Selectable(label, inputAddress == curInput) || pickFirst)
            {
              pickFirst = false;
              if (isInput2)
              {
                Def.SetGateInput2(address, inputAddress);
              }
              else
              {
                Def.SetGateInput1(address, inputAddress);
              }
              ImGui.CloseCurrentPopup();
            }
            if (label != name)
            {
              ItemTooltip(name);
            }
          }
        }
        if (!hasFound)
        {
          ImGui.BeginDisabled();
          ImGui.Text("no matches");
          ImGui.EndDisabled();
          if (pickFirst)
          {
            ImGui.CloseCurrentPopup();
          }
        }
        ImGui.EndCombo();
      }
      if (ImGui.IsItemDeactivated())
      {
        _gateEditSearch = "";
      }
      if (ImGui.BeginDragDropTarget())
      {
        if (ImGui.AcceptDragDropPayload<byte>("gate_input", out byte inputAddress))
        {
          if (isInput2)
          {
            Def.SetGateInput2(address, inputAddress);
          }
          else
          {
            Def.SetGateInput1(address, inputAddress);
          }
        }
        ImGui.EndDragDropTarget();
      }
    }

    private static void DrawEditorLUTs()
    {
      if (Def == null || Motherboard.LutOpen == 0)
      {
        return;
      }
      if (ImGui.CollapsingHeader("Lookup Table", ImGuiTreeNodeFlags.DefaultOpen) &&
          ImGui.BeginTable("lutEditTable", 3, editTableFlags))
      {
        ImGui.TableSetupColumn("");
        ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();
        for (var i = 0; i < 64; i++)
        {
          if ((Motherboard.LutOpen & (1ul << i)) != 0)
          {
            ImGui.TableNextRow();
            DrawEditorLUT(i);
          }
        }
        ImGui.EndTable();
      }
    }

    private static void DrawEditorLUT(int index)
    {
      var address = (byte)(index + 128);
      ImGui.PushID(index);
      {
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.Text(_indexText[index]);
      }
      {
        var label = Def.GetLabel(address, nameFallback: false);
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-float.Epsilon);
        if (ImGui.InputTextWithHint("##label", FPGADef.LutNames[index], ref label, 32u, ImGuiInputTextFlags.CharsNoBlank))
        {
          Def.SetLabel(address, label);
        }
      }
      {
        var value = Def.GetLutValue(address);
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-float.Epsilon);
        if (ImGui.InputDouble("##value", ref value, "%f"))
        {
          Def.SetLutValue(address, value);
        }
      }
      ImGui.PopID();
    }

    private static void DrawRawEditor()
    {
      if (ImGui.IsWindowAppearing())
      {
        _rawDef = Def.GetRaw();
      }

      if (ImGui.InputTextMultiline("##raw", ref _rawDef, 65536, Vector2.one * -float.Epsilon))
      {
        Def = FPGADef.Parse(_rawDef);
      }
    }

    private static void ItemTooltip(string text)
    {
      if (text.Length > 0 && ImGui.IsItemHovered())
      {
        ImGui.SetTooltip(text);
      }
    }

    private static string GetGridLabel(byte address, string src, float minWrapWidth)
    {
      if (!string.ReferenceEquals(_gridLabels[address], src))
      {
        var lo = 1;
        var hi = src.Length;
        if (ImGui.CalcTextSize(src).x <= minWrapWidth)
        {
          lo = hi;
        }
        while (lo < hi)
        {
          var mid = (lo + hi) >> 1;
          var sz = ImGui.CalcTextSize(WrapString(src, mid));
          if (sz.x >= sz.y)
          {
            hi = mid;
          }
          else
          {
            lo = mid + 1;
          }
        }
        _gridLabels[address] = src;
        // make as even as possible while keeping number of lines
        var lines = (src.Length + lo - 1) / lo;
        lo = (src.Length + lines - 1) / lines;
        _gridLabelsSquare[address] = WrapString(src, lo);
      }
      return _gridLabelsSquare[address];
    }

    private static string WrapString(string src, int charsPerLine)
    {
      var first = true;
      var sb = new StringBuilder();
      foreach (var line in src.SplitBy(charsPerLine))
      {
        if (!first)
        {
          sb.Append('\n');
        }
        first = false;
        sb.Append(line);
      }
      return sb.ToString();
    }
  }
}