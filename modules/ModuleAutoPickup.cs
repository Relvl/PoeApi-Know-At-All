using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using Know_At_All.utils;
using SharpDX;
using Map = ExileCore.PoEMemory.Components.Map;
using Vector2 = System.Numerics.Vector2;

namespace Know_At_All.modules;

public class ModuleAutoPickup(Mod mod) : IModule
{
    private readonly Random _random = new();

    private long _lastClickedHash;
    private SettingsClass Settings => mod.Settings.AutoPickup;
    private GameController GameController => mod.GameController;
    private Graphics Graphics => mod.Graphics;
    private IngameUIElements _inGameUi;
    private ServerInventory _inventory;
    private bool[,] _inventoryOccupiedArray;

    public string Name => Settings.Enabled ? "Auto pickup items (enabled)" : "Auto pickup items";
    public ToggleNode Expanded => Settings.Expanded;

    public void Initialise()
    {
        Input.RegisterKey(Settings.PickupTrigger.Value);
        Settings.PickupTrigger.OnValueChanged += () => Input.RegisterKey(Settings.PickupTrigger.Value);
    }

    public void Tick()
    {
        _inGameUi = null;
        _inventory = null;
        if (!Settings.Enabled) return;
        _inGameUi = GameController?.IngameState?.IngameUi;
        _inventory = GameController?.Game?.IngameState?.ServerData?.PlayerInventories[0]?.Inventory;
        _inventoryOccupiedArray = GetInventoryOccupiedArray();
    }

    public void Render()
    {
        if (!Settings.Enabled) return;

        // todo split into tick and render

        if (!NativeInput.IsKeyDown(Settings.PickupTrigger.Value))
        {
            _lastClickedHash = 0;
            return;
        }

        if (!GameController.Window.IsForeground()) return;
        if (GameController.Area.CurrentArea.IsHideout) return;
        if (GameController.Area.CurrentArea.IsTown) return;
        if (GameController.IngameState.IngameUi.OpenLeftPanel.Address != 0) return;
        if (GameController.IngameState.IngameUi.OpenRightPanel.Address != 0) return;

        var windowRect = GameController.Window.GetWindowRectangle();
        windowRect.Inflate(-100, -100);
        var mouseCurPosition = NativeInput.GetCursorPosition();
        var clickedThisFrame = false;

        foreach (var labelOnGround in _inGameUi.ItemsOnGroundLabelsVisible)
        {
            if (!labelOnGround.IsVisible) continue;
            if (!labelOnGround.IsVisibleLocal) continue;
            if (!labelOnGround.CanPickUp) continue;

            if (_lastClickedHash == labelOnGround.Address) continue;

            if (!labelOnGround.ItemOnGround.TryGetComponent<WorldItem>(out var worldItem)) continue;
            if (!IsItemPickable(worldItem)) continue;

            var rect = labelOnGround.Label.GetClientRect();
            var clickRect = rect;
            clickRect.Inflate(-2, -2);

            if (clickRect.Left >= windowRect.Left && clickRect.Right <= windowRect.Right && clickRect.Top >= windowRect.Top && clickRect.Bottom <= windowRect.Bottom)
            {
                const int thickness = 2;
                rect.Inflate(thickness / 2f, thickness / 2f);

                var isMouseOver = clickRect.Contains(mouseCurPosition);
                Graphics.DrawFrame(rect, isMouseOver ? Color.Red : Color.Aqua, thickness);

                if (isMouseOver && !clickedThisFrame)
                    try
                    {
                        NativeInput.blockInput(false);
                        if (Settings.AutoPickupForcePosition)
                        {
                            NativeInput.SetCursorPosAndLeftClick(new Vector2(labelOnGround.Label.Center.X, labelOnGround.Label.Center.Y), 1, new Vector2(_random.NextFloat(-6f, 6f), _random.NextFloat(-6f, 6)));
                            Thread.Sleep(10);
                            NativeInput.SetCursorPos(mouseCurPosition.X, mouseCurPosition.Y);
                        }
                        else
                        {
                            NativeInput.LeftClick();
                        }

                        _lastClickedHash = labelOnGround.Address;
                        clickedThisFrame = true;
                    }
                    finally
                    {
                        NativeInput.blockInput(false);
                    }
            }
        }
    }

    public void DrawSettings()
    {
        ImGui.Text($"Auto pick up: hover over item on the ground with this option enabled and [{Settings.PickupTrigger.Value}] key pressed, it will automatically be clicked.");
        ImGui.Text("It will pickup any item when no panel is open (inventory etc).");
        ImGui.Text("You should hide unwanted items in your Loot Filter, no IFL here.");
        ImGui.Separator();

        Gui.Checkbox("Enabled", Settings.Enabled);
        ImGui.SameLine();
        Gui.Checkbox("Force cursor position", Settings.AutoPickupForcePosition);
        ImGui.SameLine();
        Gui.HelpMarker("This will force the cursor to the center of the item label before clicking.");
        
        Gui.HotkeySelector($"Hotkey: {Settings.PickupTrigger.Value}", Settings.PickupTrigger);
        ImGui.Separator();

        ImGui.Text("Pick up:");
        ImGui.Indent();
        Gui.Checkbox("Stackable (currency, div cards, etc.)", Settings.AutoPickupStackable);
        ImGui.SameLine();
        if (!Settings.AutoPickupStackable) ImGui.BeginDisabled();
        Gui.Checkbox("Skip Gold", Settings.AutoPickupSkipGold);
        if (!Settings.AutoPickupStackable) ImGui.EndDisabled();

        Gui.Checkbox("Any Maps", Settings.AutoPickupMap);

        Gui.Checkbox("Any Uniques", Settings.AutoPickupUnique);
        ImGui.SameLine();
        Gui.Checkbox("Any Rares", Settings.AutoPickupRare);
        ImGui.SameLine();
        if (!Settings.AutoPickupUnique && !Settings.AutoPickupRare) ImGui.BeginDisabled();
        ImGui.SameLine();
        Gui.Checkbox("Skip Identified", Settings.AutoPickupSkipUniqRareIdentified);
        if (!Settings.AutoPickupUnique && !Settings.AutoPickupRare) ImGui.EndDisabled();

        Gui.Checkbox("Any Flasks", Settings.AutoPickupFlasks);

        ImGui.Unindent();
        ImGui.Separator();

        Gui.Checkbox("Check if fits in inventory", Settings.CheckInventoryFits);
    }

    private bool IsItemPickable(WorldItem worldItem)
    {
        if (Settings.AutoPickupStackable && worldItem.ItemEntity.TryGetComponent<Stack>(out var stack) && IsStackFits(worldItem))
            return true;

        if (!IsFitsInInventory(worldItem))
            return false;

        if (Settings.AutoPickupMap && worldItem.ItemEntity.TryGetComponent<Map>(out var map))
            return true;

        if (Settings.AutoPickupFlasks && worldItem.ItemEntity.TryGetComponent<Flask>(out var flask))
            return true;

        if ((Settings.AutoPickupUnique || Settings.AutoPickupRare) && worldItem.ItemEntity.TryGetComponent<Mods>(out var mods))
            if (!Settings.AutoPickupSkipUniqRareIdentified || !mods.Identified)
            {
                if (Settings.AutoPickupUnique && mods.ItemRarity == ItemRarity.Unique) return true;
                if (Settings.AutoPickupRare && mods.ItemRarity == ItemRarity.Rare) return true;
            }

        return false;
    }

    private bool IsStackFits(WorldItem worldItem)
    {
        if (worldItem.ItemEntity.Metadata == "Metadata/Items/Currency/GoldCoin")
            return !Settings.AutoPickupSkipGold;

        if (!Settings.CheckInventoryFits)
            return true;

        if (_inventory is null) return false;

        // check exists
        foreach (var slotItem in _inventory.InventorySlotItems.ToList())
        {
            if (slotItem.Item.Path != worldItem.ItemEntity.Path) continue;
            if (!slotItem.Item.TryGetComponent<Stack>(out var stackStored)) continue;
            if (stackStored.FullStack) continue;
            // we don't check the count here, if stack is not full, it will be picked up anyway
            return true;
        }

        return IsFitsInInventory(worldItem);
    }

    private bool[,] GetInventoryOccupiedArray()
    {
        var inventory = _inventory;
        if (inventory is null)
            return new bool[0, 0];
        var occupied = new bool[inventory.Rows, inventory.Columns];
        foreach (var inventorySlotItem in inventory.InventorySlotItems.ToList())
            for (var y = 0; y < inventorySlotItem.SizeY; ++y)
            for (var x = 0; x < inventorySlotItem.SizeX; ++x)
            {
                var slotY = inventorySlotItem.PosY + y;
                var slotX = inventorySlotItem.PosX + x;
                if (slotY < inventory.Rows && slotX < inventory.Columns)
                {
                    occupied[slotY, slotX] = true;
                }
            }

        return occupied;
    }

    private bool IsFitsInInventory(WorldItem worldItem)
    {
        if (!Settings.CheckInventoryFits)
            return true;
        var inventory = _inventory;
        if (inventory is null)
            return true;
        if (!worldItem.ItemEntity.TryGetComponent<Base>(out var baseComponent)) return false;

        // Try to find free space
        for (var y = 0; y <= inventory.Rows - baseComponent.ItemCellsSizeY; ++y)
        for (var x = 0; x <= inventory.Columns - baseComponent.ItemCellsSizeX; ++x)
        {
            var fits = true;
            for (var dy = 0; dy < baseComponent.ItemCellsSizeY && fits; ++dy)
            for (var dx = 0; dx < baseComponent.ItemCellsSizeX && fits; ++dx)
                if (_inventoryOccupiedArray[y + dy, x + dx])
                    fits = false; // slot is occupied
            if (fits)
                return true;
        }

        return false;
    }

    public class SettingsClass
    {
        public ToggleNode Enabled { get; set; } = new(true);
        public ToggleNode Expanded { get; set; } = new(true);
        public ToggleNode AutoPickupForcePosition { get; set; } = new(false);
        public ToggleNode AutoPickupStackable { get; set; } = new(true);
        public ToggleNode AutoPickupSkipGold { get; set; } = new(true);
        public ToggleNode AutoPickupMap { get; set; } = new(true);
        public ToggleNode AutoPickupUnique { get; set; } = new(false);
        public ToggleNode AutoPickupRare { get; set; } = new(false);
        public ToggleNode AutoPickupSkipUniqRareIdentified { get; set; } = new(true);
        public ToggleNode AutoPickupFlasks { get; set; } = new(false);
        public ToggleNode CheckInventoryFits { get; set; } = new(true);
        public HotkeyNode PickupTrigger { get; set; } = new(Keys.LMenu);
    }
}