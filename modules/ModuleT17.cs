using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using SharpDX;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace Know_At_All.modules;

public class ModuleT17(Mod mod) : IModule
{
    private SettingsClass Settings => mod.Settings.T17;
    public string Name => Settings.Enabled ? "T17 map mods (enabled)" : "T17 map mods";
    public ToggleNode Expanded => Settings.Expanded;
    private GameController GameController => mod.GameController;
    private Graphics Graphics => mod.Graphics;

    private int _frameCount;
    private bool _volatileFound;
    private bool _volatileFoundLast;

    private readonly List<Entity> _volatileCores = [];
    private readonly List<Entity> _drowningOrbs = [];
    private readonly List<Entity> _exarchRunes = [];
    private readonly List<Entity> _awakenerDisolation = [];
    private readonly List<Entity> _sawblades = [];


    public void Tick()
    {
        _volatileCores.Clear();
        _drowningOrbs.Clear();
        _exarchRunes.Clear();
        _awakenerDisolation.Clear();
        _sawblades.Clear();
        _volatileFound = false;

        if (!mod.Settings.Abyss.Enabled)
            return;

        foreach (var entity in GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Daemon])
        {
            if (Settings.ExarchRunes && entity.Metadata.Contains("UberMapExarchDaemon"))
                _exarchRunes.Add(entity);
        }

        foreach (var entity in GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Effect])
        {
            if (!entity.Metadata.Contains("ground_effects"))
                continue;
            if (Settings.Sawblades && entity.Buffs is not null && entity.Buffs.Any(buff => buff.Name == "architect_ground_blood"))
                _sawblades.Add(entity);
            if (Settings.AwakenerDisolation && entity.Buffs is not null && entity.Buffs.Any(buff => buff.Name == "atlas_orion_meteor_ground"))
                _awakenerDisolation.Add(entity);
        }

        foreach (var entity in GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster])
        {
            if (Settings.VolatileCore && entity.Metadata == "Metadata/Monsters/VolatileCore/VolatileCoreUberMap")
                _volatileCores.Add(entity);
            if (Settings.DrowningOrbs && !entity.IsDead && entity.Metadata.StartsWith("Metadata/Monsters/AtlasInvaders/ConsumeMonsters/ConsumeBossStalkerOrbUberMaps"))
                _drowningOrbs.Add(entity);
        }

        _volatileFound = _volatileFound || _volatileCores.Count > 0;
        if (_volatileFoundLast != _volatileFound)
        {
            _volatileFoundLast = _volatileFound;
            if (_volatileFound && Settings.VolatileCoreSound)
                GameController.SoundController.PlaySound("attention");
        }
    }

    public void Render()
    {
        if (_frameCount == int.MaxValue) _frameCount = 0;
        _frameCount++;

        if (!mod.Settings.Abyss.Enabled.Value) 
            return;

        var blink = _frameCount % 10 >= 5;
        var windowRect = GameController.Window.GetWindowRectangle();

        // todo: do think about cache till settings changed?
        var sawbladesColor = Gui.ToSharpDxColor(Settings.SawbladesColor);
        var awakenerDisolationColor = Gui.ToSharpDxColor(Settings.AwakenerDisolationColor);
        var exarchRunesColor = Gui.ToSharpDxColor(Settings.ExarchRunesColor);
        var drowningOrbsColor = Gui.ToSharpDxColor(Settings.DrowningOrbsColor);
        var volatileCoreColor = Gui.ToSharpDxColor(Settings.VolatileCoreColor);

        var camera = GameController.Game.IngameState.Camera;

        if (Settings.VolatileCore)
            foreach (var entity in _volatileCores)
            {
                var screenPos = camera.WorldToScreen(entity.PosNum);
                if (!Gui.IsLocationWithinScreen(screenPos, windowRect, 100))
                    continue;
                Graphics.DrawCircleInWorld(entity.PosNum, 40f, volatileCoreColor, 10f, 20);
                if (blink)
                    Graphics.DrawFilledCircleInWorld(entity.PosNum, 40f, Color.Red, 20);
            }

        if (Settings.DrowningOrbs)
            foreach (var entity in _drowningOrbs)
            {
                var screenPos = camera.WorldToScreen(entity.PosNum);
                if (!Gui.IsLocationWithinScreen(screenPos, windowRect, 100))
                    continue;
                Graphics.DrawCircleInWorld(entity.PosNum, 40f, drowningOrbsColor, 10f, 20);
            }

        if (Settings.ExarchRunes)
            foreach (var entity in _exarchRunes)
            {
                var screenPos = camera.WorldToScreen(entity.PosNum);
                if (!Gui.IsLocationWithinScreen(screenPos, windowRect, 100))
                    continue;
                if (!entity.TryGetComponent<Positioned>(out var positioned))
                    continue;
                var useBlink = false;
                const float sizeFactor = 1.4f;
                if (entity.TryGetComponent<Animated>(out var animated))
                    if (animated.BaseAnimatedObjectEntity.TryGetComponent<AnimationController>(out var animationController))
                    {
                        if (animationController.CurrentAnimationId != 4)
                            useBlink = true;
                        if (animationController.CurrentAnimationId == 0)
                            continue;
                    }

                if (!useBlink || blink)
                    Graphics.DrawFilledCircleInWorld(entity.PosNum, positioned.Size * sizeFactor, exarchRunesColor with { A = 50 }, 20, true);
                Graphics.DrawCircleInWorld(entity.PosNum, positioned.Size * sizeFactor, exarchRunesColor, positioned.Size * 0.2f, 20, true);
                if (Settings.ExarchRunesOnMap)
                    Graphics.DrawCircleOnLargeMap(entity.GridPosNum, true, 15f, exarchRunesColor, 2f);
            }

        if (Settings.AwakenerDisolation)
            foreach (var entity in _awakenerDisolation)
            {
                var screenPos = camera.WorldToScreen(entity.PosNum);
                if (!Gui.IsLocationWithinScreen(screenPos, windowRect, 100))
                    continue;
                if (!entity.TryGetComponent<Positioned>(out var positioned))
                    continue;
                Graphics.DrawFilledCircleInWorld(entity.PosNum, positioned.Size, awakenerDisolationColor, 20, true);
            }

        if (Settings.Sawblades)
            foreach (var entity in _sawblades)
            {
                var screenPos = camera.WorldToScreen(entity.PosNum);
                if (!Gui.IsLocationWithinScreen(screenPos, windowRect, 100))
                    continue;
                if (!entity.TryGetComponent<Positioned>(out var positioned))
                    continue;
                Graphics.DrawCircleInWorld(entity.PosNum, positioned.Size, sawbladesColor, 2f, 20, true);
            }
    }

    public void DrawSettings()
    {
        Gui.Checkbox("Enabled", Settings.Enabled);
        ImGui.Separator();

        Gui.Checkbox("Volatile Core", Settings.VolatileCore);
        ImGui.SameLine();
        Gui.Checkbox("Snap Line##Volatile", Settings.VolatileCoreSnapLine);
        ImGui.SameLine();
        Gui.Checkbox("Sound", Settings.VolatileCoreSound);
        ImGui.SameLine();
        if (ImGui.Button("Play")) GameController.SoundController.PlaySound("attention");
        ImGui.SameLine();
        Gui.ColorPicker("Color##Volatile", val => Settings.VolatileCoreColor = val, Settings.VolatileCoreColor);
        ImGui.Separator();

        Gui.Checkbox("Drowning Orbs", Settings.DrowningOrbs);
        ImGui.SameLine();
        Gui.Checkbox("Snap Line##Drowning", Settings.DrowningOrbsSnapLine);
        ImGui.SameLine();
        Gui.ColorPicker("Color##Drowning", val => Settings.DrowningOrbsColor = val, Settings.DrowningOrbsColor);

        Gui.Checkbox("Exarch Runes", Settings.ExarchRunes);
        ImGui.SameLine();
        Gui.Checkbox("On large map##Exarch", Settings.ExarchRunesOnMap);
        ImGui.SameLine();
        Gui.ColorPicker("Color##Exarch", val => Settings.ExarchRunesColor = val, Settings.ExarchRunesColor);

        Gui.Checkbox("Sawblades", Settings.Sawblades);
        ImGui.SameLine();
        Gui.ColorPicker("Color##Sawblades", val => Settings.SawbladesColor = val, Settings.SawbladesColor);

        Gui.Checkbox("Awakener's disolation", Settings.AwakenerDisolation);
        ImGui.SameLine();
        Gui.ColorPicker("Color##Awakener", val => Settings.AwakenerDisolationColor = val, Settings.AwakenerDisolationColor);
    }

    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
    public class SettingsClass
    {
        public ToggleNode Enabled { get; set; } = new(true);
        public ToggleNode Expanded { get; set; } = new(true);

        public ToggleNode VolatileCore { get; set; } = new(true);
        public ToggleNode VolatileCoreSnapLine { get; set; } = new(true);
        public ToggleNode VolatileCoreSound { get; set; } = new(true);
        public Vector4 VolatileCoreColor { get; set; } = new(1f, 0.2f, 0.2f, 1f);

        public ToggleNode DrowningOrbs { get; set; } = new(true);
        public ToggleNode DrowningOrbsSnapLine { get; set; } = new(true);
        public Vector4 DrowningOrbsColor { get; set; } = new(1f, 0.2f, 0.2f, 1f);

        public ToggleNode AwakenerDisolation { get; set; } = new(true);
        public Vector4 AwakenerDisolationColor { get; set; } = new(1f, 0.2f, 0.2f, 0.4f);

        public ToggleNode Sawblades { get; set; } = new(true);
        public Vector4 SawbladesColor { get; set; } = new(1f, 0.2f, 0.2f, 1f);

        public ToggleNode ExarchRunes { get; set; } = new(true);
        public ToggleNode ExarchRunesOnMap { get; set; } = new(true);
        public Vector4 ExarchRunesColor { get; set; } = new(1f, 0.2f, 0.2f, 1f);
    }
}