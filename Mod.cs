using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ImGuiNET;
using Know_At_All.modules;
using Know_At_All.utils;
using SharpDX;
using Vector2 = System.Numerics.Vector2;

namespace Know_At_All;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class Mod : BaseSettingsPlugin<ModSettings>
{
    // Graphics.DrawText($"Plugin {GetType().Name} is working.", new Vector2(100, 100), Color.Red);

    private readonly List<IModule> _modules = [];
    public readonly ModDictionary ModDictionary;

    public Mod()
    {
        ModDictionary = new ModDictionary(this);

        // -------------- add your modules here --------------
        _modules.Add(new ModuleAbyss(this));
        _modules.Add(new ModuleT17(this));
        _modules.Add(new ModuleAutoPickup(this));
        _modules.Add(new ModuleMercInventory(this));
        _modules.Add(new ModuleMapMods(this));
        _modules.Add(new ModuleAreaMods(this));
    }
    
    public string PlayerName => GameController.Player is null || !GameController.Player.TryGetComponent<Player>(out var player) ? "Unknown" : player.PlayerName;

    public override bool Initialise()
    {
        foreach (var module in _modules)
            try
            {
                module.Initialise();
            }
            catch (Exception e)
            {
                DebugWindow.LogError(e.StackTrace);
            }

        return true;
    }

    public override void AreaChange(AreaInstance area)
    {
        foreach (var module in _modules)
            try
            {
                module.AreaChange(area);
            }
            catch (Exception e)
            {
                DebugWindow.LogError(e.StackTrace);
            }
    }

    public override Job Tick()
    {
        return new Job("Know-At-All_MainJob", () =>
        {
            foreach (var module in _modules)
                try
                {
                    module.Tick();
                }
                catch (Exception e)
                {
                    DebugWindow.LogError(e.StackTrace);
                }
        });
    }

    /// <summary>
    /// This is called after Tick
    /// </summary>
    public override void Render()
    {
        foreach (var module in _modules)
            try
            {
                module.Render();
            }
            catch (Exception e)
            {
                DebugWindow.LogError(e.StackTrace);
            }
    }

    public override void DrawSettings()
    {
        ImGui.Text("Helps you to know what's going on. Check all the tabs, there is a lot of information.");
        ImGui.Text("You can enable it differently for each module.");
        ImGui.Separator();

        foreach (var module in _modules)
            try
            {
                ImGui.SetNextItemOpen(module.Expanded, ImGuiCond.Once);
                var opened = ImGui.TreeNodeEx($"{module.Name}##{module.GetHashCode()}", ImGuiTreeNodeFlags.CollapsingHeader);
                if (ImGui.IsItemToggledOpen())
                    module.Expanded.Value = opened;

                if (opened)
                {
                    ImGui.Indent();
                    module.DrawSettings();
                    ImGui.Unindent();
                    // don't need TreePop here! ImGuiTreeNodeFlags.CollapsingHeader deals with it!
                }

                ImGui.Spacing();
            }
            catch (Exception e)
            {
                DebugWindow.LogError(e.StackTrace);
            }
    }
}