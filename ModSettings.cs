using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using Know_At_All.modules;

namespace Know_At_All;

public class ModSettings : ISettings
{
    public ModuleAbyss.SettingsClass Abyss { get; set; } = new();
    public ModuleT17.SettingsClass T17 { get; set; } = new();
    public ModuleAutoPickup.SettingsClass AutoPickup { get; set; } = new();
    public ModuleMercInventory.SettingsClass MercInventory { get; set; } = new();
    public ToggleNode Enable { get; set; } = new(false);
}