using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using SharpDX;
using Vector2 = System.Numerics.Vector2;

namespace Know_At_All.modules;

public class ModuleAreaMods(Mod mod) : IModule
{
    private static readonly Color HighlightColor = Color.OrangeRed with { A = 50 };
    private static readonly string AreaHasDangerousMods = "Area has dangerous mods!";

    private readonly Dictionary<RectangleF, LineInfo> _warnings = [];

    private bool _areaModsVisible;
    private Element _orangeTextElement;
    private RectangleF _warningAlertFrame = RectangleF.Empty;

    private SettingsClass Settings => mod.Settings.ModuleAreaMods;
    private GameController GameController => mod.GameController;
    private Graphics Graphics => mod.Graphics;

    public string Name => Settings.Enabled ? "Current Area Mods (enabled)" : "Current Area Mods";
    public ToggleNode Expanded => Settings.Expanded;

    private string CurrentProfile
    {
        get
        {
            Settings.Profiles.TryGetValue(mod.PlayerName, out var warningsText);
            return warningsText?.Trim() ?? "";
        }
        set => Settings.Profiles[mod.PlayerName] = value;
    }

    public void Tick()
    {
        _warnings.Clear();
        _areaModsVisible = false;
        _warningAlertFrame = RectangleF.Empty;
        _orangeTextElement = null;

        if (!Settings.Enabled.Value) return;

        var warningsText = CurrentProfile;
        var warningsLines = warningsText.Split("\n");

        var modsElement = FetchAreaModsText();
        if (modsElement is null) return;

        _areaModsVisible = modsElement.IsVisible;
        if (!_areaModsVisible)
        {
            var textMeasure = Graphics.MeasureText(AreaHasDangerousMods);
            if (GameController?.IngameState?.IngameUi?.Map?.LargeMap?.IsVisible == true)
            {
                // on first game run if mods list where not opened yet - it is wrong positioned, so we look on orange area info
                var frameY = modsElement.GetClientRectCache.TopRight.Y + 35f;
                if (_orangeTextElement is not null)
                    frameY = MathF.Max(frameY, _orangeTextElement.GetClientRectCache.BottomRight.Y + 45f);

                _warningAlertFrame = new RectangleF(
                    modsElement.GetClientRectCache.TopRight.X - textMeasure.X - 10f,
                    frameY,
                    textMeasure.X + 6f,
                    textMeasure.Y + 3f
                );
            }
            else
            {
                var minimap = GameController?.IngameState?.IngameUi?.Map?.SmallMiniMap?.Children?.FirstOrDefault();
                if (minimap is not null && minimap.IsVisible)
                    _warningAlertFrame = new RectangleF(
                        minimap.GetClientRectCache.BottomRight.X - textMeasure.X - 5f,
                        minimap.GetClientRectCache.BottomRight.Y + 5f,
                        textMeasure.X + 6f,
                        textMeasure.Y + 3f
                    );
            }
        }

        var areaModsFullTextLines = modsElement.GetText(4094).Split("\n");
        var lineFrame = modsElement.GetClientRectCache with { Height = modsElement.GetClientRectCache.Height / areaModsFullTextLines.Length };
        foreach (var line in areaModsFullTextLines)
        {
            var isWarning = false;
            foreach (var check in warningsLines)
                if (check != "" && line.Contains(check, StringComparison.OrdinalIgnoreCase))
                {
                    isWarning = true;
                    break;
                }

            _warnings.Add(lineFrame, new LineInfo { Text = line, IsWarning = isWarning });

            lineFrame.Y += lineFrame.Height;
        }
    }

    public void Render()
    {
        if (!Settings.Enabled.Value) return;

        if (_areaModsVisible)
        {
            foreach (var (lineRect, info) in _warnings)
            {
                if (info.IsWarning)
                    Graphics.DrawBox(lineRect, HighlightColor);

                if (Settings.Debug)
                {
                    Graphics.DrawFrame(lineRect, Color.Aqua, 1);
                    Graphics.DrawText(info.Text, lineRect.TopLeft);
                }
            }
        }
        else if (_warningAlertFrame != RectangleF.Empty && _warnings.Count > 0 && _warnings.Any(x => x.Value.IsWarning))
        {
            Graphics.DrawBox(_warningAlertFrame, HighlightColor);
            Graphics.DrawText(AreaHasDangerousMods, _warningAlertFrame.TopLeft with { X = _warningAlertFrame.TopLeft.X + 3f });
        }
    }

    public void DrawSettings()
    {
        ImGui.Text("Looks on current map area modifiers and help you to know if they are dangerous mods.");
        ImGui.Separator();

        Gui.Checkbox("Enabled", Settings.Enabled);
        ImGui.SameLine();
        Gui.Checkbox("Debug", Settings.Debug);
        ImGui.Separator();

        ImGui.Text($"Current player: {mod.PlayerName}");
        ImGui.Separator();
        
        ImGui.Text("Warning on mods (case insensitive, separated by new line, no regexp):");
        var refText = CurrentProfile;
        if (ImGui.InputTextMultiline("##Warnings", ref refText, 4096, new Vector2(0, 300)))
            CurrentProfile = refText;
    }

    private Element FetchAreaModsText()
    {
        var map = GameController?.IngameState?.IngameUi?.Map;
        if (map is null) return null;

        foreach (var mapChild in map.Children)
        {
            if (mapChild.Children is null) continue;
            if (mapChild.Children.Count < 2) continue;

            _orangeTextElement = mapChild.Children.FirstOrDefault();

            var containerElement = mapChild.Children.LastOrDefault();
            if (containerElement is null) continue;

            containerElement = containerElement.Children.FirstOrDefault();
            if (containerElement is null) continue;

            foreach (var possibleModsElement in containerElement.Children)
            {
                if (possibleModsElement.Text is null or "") continue;
                return possibleModsElement;
            }
        }

        return null;
    }

    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    public class SettingsClass
    {
        public static readonly string DefaultProfile = string.Join("\n", "reflect", "less cooldown recovery rate");

        public ToggleNode Enabled { get; set; } = new(true);
        public ToggleNode Expanded { get; set; } = new(true);
        public ToggleNode Debug { get; set; } = new(false);

        public Dictionary<string, string> Profiles = new() { { "default", DefaultProfile } };
    }

    private struct LineInfo
    {
        public bool IsWarning;
        public string Text;
    }
}