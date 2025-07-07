using System;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;
using ImGuiNET;
using Know_At_All.modules;
using SharpDX;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace Know_At_All.ui;

public static class MapModifierPicker
{
    private static readonly Vector4 NiceColor = Gui.ToVector4(Color.DarkGreen);
    private static readonly Vector4 BadColor = Gui.ToVector4(Color.OrangeRed);
    private static readonly Vector4 RemoveColor = Gui.ToVector4(Color.SlateGray);

    private static bool _open;
    private static bool _previousOpen;
    private static ModuleMapMods.SettingsClass.Profile _currentProfile;
    private static Mods _mods;

    public static void Render()
    {
        if (!_previousOpen && _open)
            ImGui.OpenPopup("Pick mods from the map");

        _previousOpen = _open;

        if (!_open) return;

        var buttonSize = new Vector2(30, ImGui.GetFrameHeight());

        var displaySize = ImGui.GetIO().DisplaySize;
        var maxHeight = displaySize.Y * 0.5f;
        var maxWidth = displaySize.X * 0.5f;
        var minSize = new Vector2(250, Math.Min(maxHeight, 300));

        ImGui.SetNextWindowSizeConstraints(minSize, new Vector2(maxWidth, maxHeight));
        if (ImGui.BeginPopupModal("Pick mods from the map", ref _open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (ImGui.BeginTable("##MapModTable", 3, ImGuiTableFlags.BordersH))
            {
                ImGui.TableSetupColumn("Dangerous", ImGuiTableColumnFlags.None, 80);
                ImGui.TableSetupColumn("Nice", ImGuiTableColumnFlags.None, 80);
                ImGui.TableSetupColumn("");
                ImGui.TableHeadersRow();
                
                foreach (var mod in _mods.ExplicitMods)
                {
                    ImGui.TableNextRow();
                    var isAlreadyNiceMod = _currentProfile.NiceMods.ContainsKey(mod.ModRecord.Key);
                    var isAlreadyDangerousMod = _currentProfile.DangerousMods.ContainsKey(mod.ModRecord.Key);
                    
                    #region Dangerous
                    ImGui.TableSetColumnIndex(0);
                    

                    if (isAlreadyDangerousMod)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, RemoveColor);
                        if (ImGui.Button($"-##{mod.ModRecord.Key}", buttonSize))
                        {
                            _currentProfile.DangerousMods.Remove(mod.ModRecord.Key);
                        }
                        ImGui.PopStyleColor();
                        ImGui.SameLine();
                        var checkedRef = _currentProfile.DangerousMods[mod.ModRecord.Key];
                        if (ImGui.Checkbox($"##{mod.ModRecord.Key}", ref checkedRef))
                        {
                            _currentProfile.DangerousMods[mod.ModRecord.Key] = checkedRef;   
                        }
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
                            ImGui.TextUnformatted("Quick enable and disable mod without removing it from profile.");
                            ImGui.PopTextWrapPos();
                            ImGui.EndTooltip();
                        }
                    }
                    else
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, BadColor);
                        if (ImGui.Button($"+##{mod.ModRecord.Key}", buttonSize))
                        {
                            _currentProfile.DangerousMods.Add(mod.ModRecord.Key, true);
                        }
                        ImGui.PopStyleColor();
                    }
                    
                    
                    #endregion
                    
                    #region Nice
                    ImGui.TableSetColumnIndex(1);
                    
                    
                    if (isAlreadyNiceMod)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, RemoveColor);
                        if (ImGui.Button($"-##{mod.ModRecord.Key}", buttonSize))
                        {
                            _currentProfile.NiceMods.Remove(mod.ModRecord.Key);
                        }
                        ImGui.PopStyleColor();
                        ImGui.SameLine();
                        var checkedRef = _currentProfile.NiceMods[mod.ModRecord.Key];
                        if (ImGui.Checkbox($"##{mod.ModRecord.Key}", ref checkedRef))
                        {
                            _currentProfile.NiceMods[mod.ModRecord.Key] = checkedRef;   
                        }
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
                            ImGui.TextUnformatted("Quick enable and disable mod without removing it from profile.");
                            ImGui.PopTextWrapPos();
                            ImGui.EndTooltip();
                        }
                    }
                    else
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, NiceColor);
                        if (ImGui.Button($"+##{mod.ModRecord.Key}", buttonSize))
                        {
                            _currentProfile.NiceMods.Add(mod.ModRecord.Key, true);
                        }
                        ImGui.PopStyleColor();
                    }
                    
                    
                    #endregion
                    
                    #region Name
                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text(mod.Translation);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
                        ImGui.TextUnformatted(mod.ModRecord.Key);
                        ImGui.PopTextWrapPos();
                        ImGui.EndTooltip();
                    }
                    #endregion
                }
                
                ImGui.EndTable();
            }

            if (ImGui.Button("Close"))
                Dispose();

            ImGui.EndPopup();
        }
    }

    public static void Select(ModuleMapMods.SettingsClass.Profile currentProfile, Mods mods)
    {
        if (_open)
            throw new InvalidOperationException("Selector already open");

        _currentProfile = currentProfile;
        _open = true;
        _mods = mods;
    }

    private static void Dispose()
    {
        if (_open) ImGui.CloseCurrentPopup();
        _open = false;
        _previousOpen = false;
        _currentProfile = null;
        _mods = null;
    }
}