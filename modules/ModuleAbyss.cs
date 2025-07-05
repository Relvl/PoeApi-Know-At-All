// Оптимизированный класс с фильтрацией линий, отрисовкой прогресса и замерами производительности

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using Know_At_All.utils;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Know_At_All.modules;

public class ModuleAbyss(Mod mod) : IModule
{
    private const int TickIntervalMs = 1000;
    private readonly List<List<MovementNode>> _cachedPaths = [];
    private readonly Dictionary<long, MovementNode> _knownNodes = new();
    private long _lastTickTime;
    private SubMap _map;
    private SettingsClass Settings => mod.Settings.Abyss;
    private GameController GameController => mod.GameController;
    private Graphics Graphics => mod.Graphics;
    public string Name => Settings.Enabled ? "Abyss (enabled)" : "Abyss";
    public ToggleNode Expanded => Settings.Expanded;

    private readonly CachedValue<bool> _ingameUiCheckVisible = new TimeCache<bool>(() =>
            mod.GameController.IngameState.IngameUi.FullscreenPanels.Any(x => x.IsVisibleLocal) ||
            mod.GameController.IngameState.IngameUi.LargePanels.Any(x => x.IsVisibleLocal),
        250);

    public void Initialise()
    {
        _map = null;
    }

    public void AreaChange(AreaInstance area)
    {
        if (!Settings.Enabled.Value) return;
        _map = null;
        _knownNodes.Clear();
        _cachedPaths.Clear();
    }

    public void Tick()
    {
        if (!Settings.Enabled.Value)
            return;

        var now = Environment.TickCount64;
        if (now - _lastTickTime < TickIntervalMs) return;
        _lastTickTime = now;

        _map = GameController.Game.IngameState.IngameUi.Map.LargeMap.AsObject<SubMap>();
        var idx = -1;
        foreach (var entity in GameController.EntityListWrapper.ValidEntitiesByType[EntityType.MiscellaneousObjects])
        {
            idx++;
            if (!entity.Metadata.StartsWith("Metadata/MiscellaneousObjects/Abyss/")) continue;
            if (entity.Metadata.EndsWith("AbyssNodeMini") || entity.Metadata.EndsWith("AbyssSubAreaTransition") || entity.Metadata.EndsWith("Spawned")) continue;
            if (!entity.TryGetComponent<Transitionable>(out var transitionable)) continue;
            if (!entity.TryGetComponent<Positioned>(out var positioned)) continue;
            var iconShown = entity.TryGetComponent<MinimapIcon>(out var minimapIcon) && minimapIcon.IsVisible && !minimapIcon.IsHide;

            var id = entity.Id;
            if (_knownNodes.TryGetValue(id, out var node))
            {
                node.GridPosNum = entity.GridPosNum;
                node.PosNum = entity.PosNum;
                node.Metadata = entity.Metadata;
                node.Flag1 = transitionable.Flag1;
                node.Idx = idx;
                node.Size = positioned.Size;
                node.IconShown = iconShown;
            }
            else
            {
                _knownNodes[id] = new MovementNode
                {
                    Id = id,
                    GridPosNum = entity.GridPosNum,
                    PosNum = entity.PosNum,
                    Metadata = entity.Metadata,
                    Flag1 = transitionable.Flag1,
                    Idx = idx,
                    Size = positioned.Size,
                    IconShown = iconShown
                };
            }
        }

        RebuildPaths();
    }

    public void Render()
    {
        if (!Settings.Enabled.Value || _map is null)
            return;
        if (!GameController.IsFunctionsReady(true))
            return;
        if (_ingameUiCheckVisible?.Value != false)
            return;

        var player = GameController.Player;
        if (player is null || !player.IsValid) return;

        var camera = GameController.Game.IngameState.Camera;
        var screenRect = GameController.Window.GetWindowRectangle();
        var lineColor = Gui.ToSharpDxColor(Settings.WorldLineColor);
        var pointColor = Gui.ToSharpDxColor(Settings.WorldCircleColor);
        var snapLineColor = Gui.ToSharpDxColor(Settings.SnapLineColor);

        foreach (var path in _cachedPaths)
        {
            var pointFound = false;
            for (var i = 0; i < path.Count; i++)
            {
                var nodeA = path[i];
                var screenA = camera.WorldToScreen(nodeA.PosNum);
                var onScreenA = Gui.IsLocationWithinScreen(screenA, screenRect, 100);

                var showPoint = false;
                if (!pointFound && nodeA.AllowPoint())
                {
                    pointFound = true;
                    showPoint = true;
                }

                if (Settings.Debug)
                {
                    var debug = $"[{nodeA.Idx}] {nodeA.Metadata[(nodeA.Metadata.LastIndexOf('/') + 1)..]} F:{nodeA.Flag1} I: {nodeA.IconShown}";
                    Graphics.DrawText(debug, screenA);
                }

                if (Settings.WorldLine && i < path.Count - 1 && pointFound)
                {
                    var nodeB = path[i + 1];
                    var screenB = camera.WorldToScreen(nodeB.PosNum);
                    var onScreenB = Gui.IsLocationWithinScreen(screenB, screenRect, 100);
                    if (onScreenA && onScreenB)
                        Graphics.DrawLine(screenA, screenB, 5f, lineColor);
                }

                if (Settings.WorldLine && onScreenA && showPoint) Graphics.DrawCircleInWorld(nodeA.PosNum, nodeA.Size, pointColor, nodeA.Size / 10f, 20);

                if (Settings.MapLine && _map.IsVisible)
                {
                    if (i < path.Count - 1 && pointFound)
                        Graphics.DrawLineOnLargeMap(nodeA.GridPosNum, path[i + 1].GridPosNum, 3f, lineColor);
                    if (showPoint)
                        Graphics.DrawCircleOnLargeMap(nodeA.GridPosNum, true, 10f, pointColor, 3f);
                }

                if (Settings.SnapLine && showPoint && player.GridPosNum.Distance(nodeA.GridPosNum) <= 100) Graphics.DrawLineInWorld(player.GridPosNum, nodeA.GridPosNum, 8f, snapLineColor);
            }
        }
    }

    public void EachEntityRender(Entity entity, Vector2 screenPos, Positioned positioned)
    {
    }

    public void DrawSettings()
    {
        ImGui.Text("Shows the Abyss track/path and nearest nodes.");
        ImGui.Spacing();

        Gui.Checkbox("Enabled", Settings.Enabled);

        Gui.Checkbox("Render in world: ", Settings.WorldLine);
        ImGui.SameLine();
        Gui.ColorPicker("Track", val => Settings.WorldLineColor = val, Settings.WorldLineColor);
        ImGui.SameLine();
        Gui.ColorPicker("Node", val => Settings.WorldCircleColor = val, Settings.WorldCircleColor);

        Gui.Checkbox("Render on large map: ", Settings.MapLine);
        ImGui.SameLine();
        Gui.ColorPicker("Track", val => Settings.MapLineColor = val, Settings.MapLineColor);
        ImGui.SameLine();
        Gui.ColorPicker("Node", val => Settings.MapCircleColor = val, Settings.MapCircleColor);

        Gui.Checkbox("Render snap line:", Settings.SnapLine);
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
            ImGui.TextUnformatted("Line from player to nearest active or start node");
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }

        ImGui.SameLine();
        Gui.ColorPicker("Color", val => Settings.SnapLineColor = val, Settings.SnapLineColor);

        ImGui.Spacing();
        ImGui.Separator();
        Gui.Checkbox("Debug: node names/info", Settings.Debug);
    }

    private void RebuildPaths()
    {
        _cachedPaths.Clear();
        var sorted = _knownNodes.Values.OrderBy(n => n.Id).ToList();

        List<MovementNode> currentPath = null;
        foreach (var node in sorted)
        {
            if (node.IsStart || currentPath == null)
            {
                if (currentPath is { Count: > 1 })
                    _cachedPaths.Add(currentPath);
                currentPath = [];
            }

            currentPath.Add(node);

            if (node.IsEnd && currentPath.Count > 1)
            {
                _cachedPaths.Add(currentPath);
                currentPath = null;
            }
        }

        if (currentPath is { Count: > 1 })
            _cachedPaths.Add(currentPath);
    }

    private class MovementNode
    {
        public int Flag1;
        public Vector2 GridPosNum;
        public bool IconShown;
        public long Id;
        public int Idx;
        public string Metadata;
        public Vector3 PosNum;
        public float Size;

        public bool IsStart => Metadata.EndsWith("AbyssStartNode");
        public bool IsEnd => Metadata.Contains("Final");

        public bool AllowPoint()
        {
            if (IsStart && Flag1 != 1) return false;
            if (IsEnd && Flag1 == 4) return false;
            return Flag1 <= 2;
        }
    }

    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
    public class SettingsClass
    {
        public ToggleNode Enabled { get; set; } = new(true);
        public ToggleNode Expanded { get; set; } = new(false);
        public ToggleNode Debug { get; set; } = new(false);

        public ToggleNode WorldLine { get; set; } = new(true);
        public ToggleNode SnapLine { get; set; } = new(true);
        public Vector4 WorldLineColor { get; set; } = new(0f, 1f, 1f, 1f);
        public Vector4 WorldCircleColor { get; set; } = new(0.5f, 1f, 0f, 0.5f);
        public Vector4 SnapLineColor { get; set; } = new(0.5f, 1f, 0f, 0.5f);

        public ToggleNode MapLine { get; set; } = new(true);
        public Vector4 MapLineColor { get; set; } = new(0f, 1f, 1f, 1f);
        public Vector4 MapCircleColor { get; set; } = new(0.5f, 1f, 0f, 0.5f);
    }
}