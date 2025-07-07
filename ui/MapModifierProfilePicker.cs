using System;
using System.Numerics;
using ImGuiNET;
using Know_At_All.modules;

namespace Know_At_All.ui;

public static class MapModifierProfilePicker
{
    private static bool _open;
    private static bool _previousOpen;
    private static ModuleMapMods.SettingsClass.Profile _currentProfile;
    private static ModuleMapMods.SettingsClass _settings;

    public static void Render()
    {
        if (!_previousOpen && _open)
            ImGui.OpenPopup("Copy (rewrite) all mods from profile");

        _previousOpen = _open;

        if (!_open) return;

        var displaySize = ImGui.GetIO().DisplaySize;
        var maxHeight = displaySize.Y * 0.5f;
        var minSize = new Vector2(450, Math.Min(maxHeight, 300));
        ImGui.SetNextWindowSizeConstraints(minSize, new Vector2(450, maxHeight));
        ImGui.SetNextWindowSize(minSize, ImGuiCond.Appearing);
        if (ImGui.BeginPopupModal("Copy (rewrite) all mods from profile", ref _open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text("Select the profile to copy from:");
            ImGui.Separator();

            foreach (var (name, profile) in _settings.Profiles)
            {
                if (profile == _currentProfile) continue;
                if (ImGui.Button(name))
                {
                    _currentProfile.NiceMods = profile.NiceMods;
                    _currentProfile.DangerousMods = profile.DangerousMods;
                    Dispose();
                }

                ImGui.SameLine();
                ImGui.Text($"Dangerous: {profile.DangerousMods.Count}, Nice: {profile.NiceMods.Count}");
            }

            ImGui.Separator();
            ImGui.Text("ALL existing mod in current profile will be overwritten!");
            ImGui.Text("This action cannot be undone!");

            if (ImGui.Button("Cancel"))
                Dispose();

            ImGui.EndPopup();
        }
    }

    public static void Select(ModuleMapMods.SettingsClass.Profile currentProfile, ModuleMapMods.SettingsClass settings)
    {
        if (_open)
            throw new InvalidOperationException("Selector already open");

        _currentProfile = currentProfile;
        _settings = settings;
        _open = true;
    }

    private static void Dispose()
    {
        if (_open) ImGui.CloseCurrentPopup();
        _open = false;
        _previousOpen = false;
        _currentProfile = null;
        _settings = null;
    }
}