using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ExileCore;
using ExileCore.PoEMemory.FilesInMemory;
using ExileCore.Shared.Enums;
using ImGuiNET;

namespace Know_At_All.ui;

public static class MapModifierSelector
{
    private static bool _open;
    private static bool _previousOpen;
    private static Action<IEnumerable<string>> _callback;
    private static readonly HashSet<ModsDat.ModRecord> _mods = [];
    private static readonly HashSet<string> _selectedMods = [];
    private static string _search = string.Empty;

    public static void Render()
    {
        if (!_previousOpen && _open)
            ImGui.OpenPopup("Select Map Modifiers");

        _previousOpen = _open;

        if (!_open) return;

        var displaySize = ImGui.GetIO().DisplaySize;
        var maxHeight = displaySize.Y * 0.5f;
        var minSize = new Vector2(450, Math.Min(maxHeight, 600));
        ImGui.SetNextWindowSizeConstraints(minSize, new Vector2(450, maxHeight));
        ImGui.SetNextWindowSize(minSize, ImGuiCond.Appearing);
        if (ImGui.BeginPopupModal("Select Map Modifiers", ref _open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text("Search: ");
            ImGui.SameLine();
            ImGui.InputText("##Search", ref _search, 64);
            ImGui.Separator();

            ImGui.BeginChild("ModsScrollRegion", new Vector2(0, -ImGui.GetFrameHeightWithSpacing() * 1.5f));
            foreach (var mod in _mods.ToList())
            {
                var search = _search.Trim().ToLower();
                if (search != string.Empty && !mod.Key.Contains(search, StringComparison.CurrentCultureIgnoreCase)) continue;

                var included = _selectedMods.Contains(mod.Key);

                if (included)
                {
                    if (ImGui.Button($"X##{mod}"))
                        _selectedMods.Remove(mod.Key);
                }
                else if (ImGui.Button($"+##{mod}"))
                    _selectedMods.Add(mod.Key);

                ImGui.SameLine();
                if (included) ImGui.BeginDisabled();
                ImGui.Text($"[{mod.AffixType.ToString()[0]}] {mod.Key}");
                if (included) ImGui.EndDisabled();
            }

            ImGui.EndChild();
            ImGui.Separator();

            if (ImGui.Button("Cancel"))
                Dispose();
            ImGui.SameLine();

            if (_selectedMods.Count == 0) ImGui.BeginDisabled();
            if (ImGui.Button($"Add {_selectedMods.Count} mods"))
            {
                _callback?.Invoke(_selectedMods);
                Dispose();
            }

            if (_selectedMods.Count == 0) ImGui.EndDisabled();

            ImGui.EndPopup();
        }
    }

    public static void Select(GameController gameController, Dictionary<string, bool> profile, Action<IEnumerable<string>> callback)
    {
        if (_open)
            throw new InvalidOperationException("Selector already open");

        foreach (var (key, record) in gameController.Files.Mods.records)
        {
            if (record.Domain != ModDomain.Area) continue;
            if (record.AffixType != ModType.Prefix && record.AffixType != ModType.Suffix) continue;
            if (profile.ContainsKey(key)) continue;
            _mods.Add(record);
        }

        _callback = callback;
        _open = true;
    }

    private static void Dispose()
    {
        if (_open) ImGui.CloseCurrentPopup();
        _open = false;
        _previousOpen = false;
        _callback = null;
        _mods.Clear();
    }
}