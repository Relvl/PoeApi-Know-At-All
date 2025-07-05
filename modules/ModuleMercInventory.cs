using System.Collections.Generic;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using Know_At_All.utils;
using SharpDX;
using Vector2 = System.Numerics.Vector2;

namespace Know_At_All.modules;

public class ModuleMercInventory(Mod mod) : IModule
{
    private readonly Color _chaosColor = new(0xF7, 0x00, 0xE7, 255);

    private readonly List<string> _itemNames =
    [
        // currencies
        "divine orb", "hinekora's lock", "mirror shard", "mirror of kalandra",
        // uniques
        "mageblood", "headhunter", "kalandra's touch", "rakiata's dance", "defiance of destiny", "soul taker"
    ];

    private readonly Color _uniqueColor = new(0xAF, 0x5F, 0x1C, 255);

    private int _frameCount;

    private IngameUIElements _inGameUi;
    private MercenaryEncounterWindow _mercWindow;
    private SettingsClass Settings => mod.Settings.MercInventory;
    private GameController GameController => mod.GameController;
    private Graphics Graphics => mod.Graphics;
    public string Name => Settings.Enabled ? "Merc Inventory (enabled)" : "Merc Inventory (disabled)";
    public ToggleNode Expanded => Settings.Expanded;

    public void Initialise()
    {
        Input.RegisterKey(Settings.DumpTooltipKey.Value);
        Settings.DumpTooltipKey.OnValueChanged += () => Input.RegisterKey(Settings.DumpTooltipKey.Value);
    }

    public void Tick()
    {
        _inGameUi = null;
        _mercWindow = null;
        if (!Settings.Enabled) return;
        _inGameUi = GameController?.IngameState?.IngameUi;
        _mercWindow = _inGameUi?.MercenaryEncounterWindow;
    }

    public void Render()
    {
        if (!Settings.Enabled) return;
        if (_inGameUi is null || _mercWindow is null) return;
        if (!_mercWindow.IsVisible) return;

        _frameCount++;
        if (_frameCount == int.MaxValue) _frameCount = 0;
        var blink = _frameCount % 10 >= 5;
        var frameColor = blink ? Color.Red : Color.Aqua;
        var mouseCurPosition = NativeInput.GetCursorPosition();

        foreach (var inventory in _mercWindow.Inventories)
        {
            if (!inventory.IsVisible) continue;
            foreach (var itemElement in inventory.VisibleInventoryItems)
            {
                if (!itemElement.IsVisible) continue;
                var item = itemElement.Item;
                if (item is null) continue;
                var itemRect = itemElement.GetClientRectCache;
                var isMouseOver = itemRect.Contains(mouseCurPosition);

                if (!item.TryGetComponent<Base>(out var baseComponent)) continue;

                // simple by name
                if (_itemNames.Contains(baseComponent.Name.ToLower()))
                {
                    ShowFrame();
                    continue;
                }

                if (item.TryGetComponent<Mods>(out var mods))
                {
                    if (mods.ItemRarity == ItemRarity.Unique)
                    {
                        Graphics.DrawFrame(itemRect, _uniqueColor, 6);
                        continue;
                    }


                    var t1Stars = 0;
                    var t2Stars = 0;
                    var unkModStars = 0;
                    var chaosRes = false;

                    var validTiers = new HashSet<string>();
                    var lines = new List<string>();

                    foreach (var itemMod in mods.ExplicitMods)
                    {
                        var modInfo = mod.ModDictionary.Get(item, itemMod);
                        if (modInfo.Tier == 1) t1Stars++;
                        if (modInfo.Tier == 2) t2Stars++;
                        if (modInfo.Tier == -1) unkModStars++;
                        if (modInfo.Record.Key.Contains("ChaosResist")) chaosRes = true;
                        if (isMouseOver && Settings.Debug)
                        {
                            foreach (var tier in modInfo.ValidTiers)
                                validTiers.Add(tier);
                            lines.Add($"{modInfo.Record.Key} - {modInfo.Tier} / {modInfo.TotalTiers}");
                        }
                    }

                    var offset = new Vector2(itemRect.X + 10, itemRect.Y + 10);
                    if (t1Stars > 0)
                    {
                        offset.X = itemRect.X + 10;
                        for (var i = 0; i < t1Stars; i++)
                        {
                            Graphics.DrawCircleFilled(offset, 7, Color.Black, 4);
                            Graphics.DrawCircleFilled(offset, 6, Color.Gold, 4);
                            offset.X += 12;
                        }
                    }

                    if (t2Stars > 0)
                    {
                        offset.X = itemRect.X + 10;
                        offset.Y += 13;
                        for (var i = 0; i < t2Stars; i++)
                        {
                            Graphics.DrawCircleFilled(offset, 7, Color.Black, 4);
                            Graphics.DrawCircleFilled(offset, 6, Color.LightGray, 4);
                            offset.X += 12;
                        }
                    }

                    if (unkModStars > 0)
                    {
                        offset.X = itemRect.X + 10;
                        offset.Y += 13;
                        for (var i = 0; i < unkModStars; i++)
                        {
                            Graphics.DrawCircleFilled(offset, 7, Color.Black, 4);
                            Graphics.DrawCircleFilled(offset, 6, Color.Purple, 4);
                            offset.X += 12;
                        }
                    }

                    if (chaosRes)
                    {
                        offset = new Vector2(itemRect.BottomRight.X - 10, itemRect.BottomRight.Y - 10);
                        Graphics.DrawCircleFilled(offset, 7, Color.Black, 4);
                        Graphics.DrawCircleFilled(offset, 6, _chaosColor, 4);

                        if (t1Stars > 1)
                            Graphics.DrawFrame(itemRect, _chaosColor, 2);
                    }

                    var y = 0;
                    foreach (var line in lines)
                    {
                        Graphics.DrawTextWithBackground(line, new Vector2(itemRect.X, itemRect.Y + y * 16), Color.Aqua, Color.Black);
                        y++;
                    }

                    if (isMouseOver && Settings.DumpTooltipKey.PressedOnce() && Settings.Debug)
                    {
                        ImGui.SetClipboardText(string.Join("\n", validTiers));
                        DebugWindow.LogMsg("Valid tiers copied to clipboard");
                    }
                }

                continue;

                void ShowFrame()
                {
                    Graphics.DrawFrame(itemRect, frameColor, 3);
                }
            }
        }
    }

    public void DrawSettings()
    {
        ImGui.Text("Shows the mercenary inventory highlighting on the valuable items.");
        ImGui.Separator();

        Gui.Checkbox("Enabled", Settings.Enabled);
        ImGui.Separator();

        Gui.Checkbox("Debug", Settings.Debug);
        if (Settings.Debug)
        {
            ImGui.Text("[Num5] on hovered Merc item to copy all the tiers");
        }
    }

    public class SettingsClass
    {
        public ToggleNode Enabled { get; set; } = new(true);
        public ToggleNode Expanded { get; set; } = new(true);

        public HotkeyNodeV2 DumpTooltipKey { get; set; } = new(Keys.NumPad5);
        public ToggleNode Debug { get; set; } = new(false);
    }
}