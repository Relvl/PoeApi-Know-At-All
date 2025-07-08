using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using Know_At_All.ui;
using Know_At_All.utils;
using SharpDX;
using Map = ExileCore.PoEMemory.Components.Map;
using Vector2 = System.Numerics.Vector2;

namespace Know_At_All.modules;

public class ModuleMapMods(Mod mod) : IModule
{
    private static readonly Color CorruptedColor = Color.Red with { A = 150 };
    private readonly List<NormalInventoryItem> _visibleMaps = [];

    private IngameUIElements _inGameUi;

    private SettingsClass Settings => mod.Settings.MapMods;
    private GameController GameController => mod.GameController;
    private Graphics Graphics => mod.Graphics;

    private SettingsClass.Profile Profile
    {
        get
        {
            var playerName = mod.PlayerName;
            if (Settings.Profiles.TryGetValue(playerName, out var profile)) return profile;
            profile = new SettingsClass.Profile();
            Settings.Profiles.Add(playerName, profile);
            return profile;
        }
    }

    public string Name => Settings.Enabled ? "Map Mods (enabled)" : "Map Mods";
    public ToggleNode Expanded => Settings.Expanded;

    public void Initialise()
    {
        Input.RegisterKey(Settings.PickFromMap.Value);
        Settings.PickFromMap.OnValueChanged += () => Input.RegisterKey(Settings.PickFromMap.Value);
    }

    public void Tick()
    {
        _inGameUi = null;
        _visibleMaps.Clear();
        if (!Settings.Enabled) return;
        _inGameUi = GameController?.IngameState?.IngameUi;
        if (_inGameUi is null) return;

        var playerInventory = _inGameUi.InventoryPanel[InventoryIndex.PlayerInventory];
        if (playerInventory is not null && playerInventory.IsVisible)
            foreach (var item in playerInventory.VisibleInventoryItems)
                ProcessItem(item);

        var stash = _inGameUi.StashElement?.VisibleStash;
        if (stash is not null && stash.IsVisible)
            foreach (var item in stash.VisibleInventoryItems)
                ProcessItem(item);
    }

    public void Render()
    {
        if (!Settings.Enabled) return;
        var mouseCurPosition = NativeInput.GetCursorPosition();

        foreach (var map in _visibleMaps)
        {
            if (!map.IsVisible || !map.IsValid || !map.Item.IsValid) continue;
            if (!map.Item.TryGetComponent<Base>(out var baseComponent)) continue;
            if (!map.Item.TryGetComponent<Mods>(out var modsComponent)) continue;

            if (map.GetClientRectCache.Contains(mouseCurPosition))
                if (Settings.PickFromMap.PressedOnce())
                    MapModifierPicker.Select(Profile, modsComponent);

            var rect = map.GetClientRectCache;

            rect.Inflate(-5, -5);
            var topLeft = new Vector2(rect.X, rect.Y);
            var bottomRight = new Vector2(rect.X + rect.Width, rect.Y + rect.Height);
            var topRight = new Vector2(rect.X + rect.Width, rect.Y);
            var bottomLeft = new Vector2(rect.X, rect.Y + rect.Height);

            if (Settings.MarkCorrupted && baseComponent.isCorrupted && modsComponent.ExplicitMods.Count == 8)
                Graphics.DrawCircleFilled(rect.Center.ToVector2Num(), 12, CorruptedColor, 20);

            foreach (var explicitMod in modsComponent.ExplicitMods)
            {
                if (Settings.MarkDangerous)
                    foreach (var (key, enabled) in Profile.DangerousMods)
                    {
                        if (!enabled) continue;
                        if (explicitMod.ModRecord.Key == key)
                        {
                            Graphics.DrawLine(topLeft, bottomRight, 5, Color.OrangeRed);
                            Graphics.DrawLine(topRight, bottomLeft, 5, Color.OrangeRed);
                        }
                    }

                if (Settings.MarkNice)
                    foreach (var (key, enabled) in Profile.NiceMods)
                    {
                        if (!enabled) continue;
                        if (explicitMod.ModRecord.Key == key)
                        {
                            Graphics.DrawFrame(rect, Color.Lime, 5);
                        }
                    }
            }
        }

        MapModifierPicker.Render();
    }

    public void DrawSettings()
    {
        ImGui.Text("Shows dangerous and nice mods on the map item.");
        ImGui.Separator();
        Gui.Checkbox("Enabled", Settings.Enabled);
        ImGui.Separator();

        Gui.HotkeySelector($"Pick mods from the map hotkey: {Settings.PickFromMap.Value}", Settings.PickFromMap);
        ImGui.Text("Hover over the map item and press the hotkey to pick mods.");

        ImGui.Separator();
        ImGui.Text($"Current player: {mod.PlayerName}");
        ImGui.SameLine();
        if (ImGui.Button("Pick settings from..."))
            MapModifierProfilePicker.Select(Profile, Settings);

        ImGui.Separator();

        Gui.Checkbox("Mark dangerous mods", Settings.MarkDangerous);
        Gui.Checkbox("Mark nice mods", Settings.MarkNice);
        Gui.Checkbox("Mark corrupted 8-mod maps", Settings.MarkCorrupted);

        ImGui.Separator();

        if (ImGui.TreeNodeEx("Dangerous Mods##TreeOne", ImGuiTreeNodeFlags.CollapsingHeader | ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Text("I can't run these mods:");
            DrawSettingsMods(Profile.DangerousMods);
            // don't need TreePop here! ImGuiTreeNodeFlags.CollapsingHeader deals with it!
        }

        if (ImGui.TreeNodeEx("Nice Mods##TreeTwo", ImGuiTreeNodeFlags.CollapsingHeader | ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Text("I want to run these mods:");
            DrawSettingsMods(Profile.NiceMods);
            // don't need TreePop here! ImGuiTreeNodeFlags.CollapsingHeader deals with it!
        }

        MapModifierSelector.Render();
        MapModifierProfilePicker.Render();
    }

    private void ProcessItem(NormalInventoryItem item)
    {
        if (item is null) return;
        if (!item.Item.TryGetComponent<Map>(out _)) return;
        if (!item.Item.TryGetComponent<Mods>(out var mods)) return;
        if (mods.ItemRarity is ItemRarity.Normal or ItemRarity.Unique) return;
        _visibleMaps.Add(item);
    }

    private void DrawSettingsMods(Dictionary<string, bool> profile)
    {
        var buttonSize = new Vector2(ImGui.GetFrameHeight(), ImGui.GetFrameHeight());
        ImGui.SameLine();
        if (ImGui.Button("Add...##Nice"))
            MapModifierSelector.Select(GameController, profile, newModIds => AddMods(newModIds, profile));

        ImGui.Spacing();
        ImGui.Indent();
        foreach (var key in profile.Keys.ToList())
        {
            var enabled = profile[key];
            if (ImGui.Checkbox($"##{key}", ref enabled))
                profile[key] = enabled;

            ImGui.SameLine();

            if (ImGui.Button($"X##{key}", buttonSize))
                profile.Remove(key);

            ImGui.SameLine();
            ImGui.Text(key);
        }

        ImGui.Unindent();
        ImGui.Spacing();
    }

    private static void AddMods(IEnumerable<string> mods, Dictionary<string, bool> profile)
    {
        foreach (var modKey in mods)
            profile.TryAdd(modKey, true);
    }

    public class SettingsClass
    {
        public ToggleNode Enabled { get; set; } = new(true);
        public ToggleNode Expanded { get; set; } = new(true);
        public HotkeyNode PickFromMap { get; set; } = new(Keys.F4);
        public ToggleNode MarkDangerous { get; set; } = new(true);
        public ToggleNode MarkNice { get; set; } = new(true);
        public ToggleNode MarkCorrupted { get; set; } = new(true);

        public Dictionary<string, Profile> Profiles { get; set; } = new() { { "default", new Profile() } };

        public class Profile
        {
            public Dictionary<string, bool> DangerousMods { get; set; } = [];
            public Dictionary<string, bool> NiceMods { get; set; } = [];
        }
    }
}