using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using SharpDX;
using Vector2 = System.Numerics.Vector2;

namespace Know_At_All.modules;

public class ModuleCurrentAreaMods(Mod mod) : IModule
{
    private static readonly Color HighlightColor = Color.OrangeRed with { A = 50 };
    private static readonly string AreaHasDangerousMods = "Area has dangerous mods!";

    private SettingsClass Settings => mod.Settings.CurrentAreaMods;
    private GameController GameController => mod.GameController;
    private Graphics Graphics => mod.Graphics;

    public string Name => Settings.Enabled ? "Current Area Mods (enabled)" : "Current Area Mods";
    public ToggleNode Expanded => Settings.Expanded;

    private bool _areaModsVisible;
    private RectangleF _warningAlertFrame = RectangleF.Empty;

    private readonly Dictionary<RectangleF, LineInfo> _warnings = [];

    public void Tick()
    {
        _warnings.Clear();
        _areaModsVisible = false;
        _warningAlertFrame = RectangleF.Empty;

        if (!Settings.Enabled.Value) return;

        var modsElement = FetchAreaModsText();
        if (modsElement is null) return;

        _areaModsVisible = modsElement.IsVisible;
        if (!_areaModsVisible)
        {
            var textMeasure = Graphics.MeasureText(AreaHasDangerousMods);
            if (GameController?.IngameState?.IngameUi?.Map?.LargeMap?.IsVisible == true)
            {
                _warningAlertFrame = new RectangleF(
                    modsElement.GetClientRectCache.TopRight.X - textMeasure.X - 10f,
                    modsElement.GetClientRectCache.TopRight.Y + 35f,
                    textMeasure.X + 6f,
                    textMeasure.Y + 3f
                );
            }
            else
            {
                var minimap = GameController?.IngameState?.IngameUi?.Map?.SmallMiniMap?.Children?.FirstOrDefault();
                if (minimap is not null && minimap.IsVisible)
                {
                    _warningAlertFrame = new RectangleF(
                        minimap.GetClientRectCache.BottomRight.X - textMeasure.X - 5f,
                        minimap.GetClientRectCache.BottomRight.Y + 5f,
                        textMeasure.X + 6f,
                        textMeasure.Y + 3f
                    );
                }
            }
        }

        var lineFrame = modsElement.GetClientRectCache with { Height = 24f };
        var fullText = modsElement.GetText(4094);
        foreach (var line in fullText.Split("\n"))
        {
            var isWarning = false;
            foreach (var check in Settings.Warnings.Value.Split("\n"))
            {
                if (line.Contains(check, StringComparison.OrdinalIgnoreCase))
                {
                    isWarning = true;
                    break;
                }
            }

            _warnings.Add(lineFrame, new LineInfo { Text = line, IsWarning = isWarning });

            lineFrame.Y += lineFrame.Height;
        }
    }

    private Element FetchAreaModsText()
    {
        var map = GameController?.IngameState?.IngameUi?.Map;
        if (map is null) return null;

        foreach (var mapChild in map.Children)
        {
            if (mapChild.Children is null) continue;
            if (mapChild.Children.Count < 2) continue;

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

    public void Render()
    {
        if (!Settings.Enabled.Value) return;

        if (_areaModsVisible)
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
        else if (_warningAlertFrame != RectangleF.Empty)
        {
            Graphics.DrawBox(_warningAlertFrame, HighlightColor);
            Graphics.DrawText(AreaHasDangerousMods, _warningAlertFrame.TopLeft with { X = _warningAlertFrame.TopLeft.X + 3f });
        }
    }

    public void DrawSettings()
    {
        Gui.Checkbox("Enabled", Settings.Enabled);
        ImGui.SameLine();
        Gui.Checkbox("Debug", Settings.Debug);
        ImGui.Separator();

        var refText = Settings.Warnings.Value;
        if (ImGui.InputTextMultiline("##Warnings", ref refText, 4096, new Vector2(0, 300)))
            Settings.Warnings.Value = refText.Trim();
    }

    public class SettingsClass
    {
        private static readonly string[] DefaultWarnings =
        [
            "reflect",
            "less cooldown recovery rate"
        ];

        public ToggleNode Enabled { get; set; } = new(true);
        public ToggleNode Expanded { get; set; } = new(true);
        public TextNode Warnings { get; set; } = new(string.Join("\n", DefaultWarnings));
        public ToggleNode Debug { get; set; } = new(false);
    }

    private struct LineInfo
    {
        public bool IsWarning;
        public string Text;
    }
}