using System;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using SharpDX;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace Know_At_All;

public static class Gui
{
    public static void Checkbox(string label, ToggleNode node)
    {
        var value = node.Value;
        ImGui.Checkbox(label, ref value);
        node.Value = value;
    }

    public static void ColorPicker(string label, Action<Vector4> setter, Vector4 currentValue)
    {
        var color = currentValue;
        if (ImGui.ColorEdit4(label, ref color, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoInputs))
            setter(color);
    }

    public static SharpDX.Color ToSharpDxColor(Vector4 vec)
    {
        return new SharpDX.Color(
            (byte)(vec.X * 255), // Red
            (byte)(vec.Y * 255), // Green
            (byte)(vec.Z * 255), // Blue
            (byte)(vec.W * 255) // Alpha
        );
    }

    public static void HelpMarker(string desc)
    {
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
            ImGui.TextUnformatted(desc);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }

    public static bool IsLocationWithinScreen(Vector2 entityPos, RectangleF screenSize, float allowancePX)
    {
        // Check if the position is within the screen bounds with allowance
        var leftBound = screenSize.Left - allowancePX;
        var rightBound = screenSize.Right + allowancePX;
        var topBound = screenSize.Top - allowancePX;
        var bottomBound = screenSize.Bottom + allowancePX;

        return entityPos.X >= leftBound && entityPos.X <= rightBound && entityPos.Y >= topBound && entityPos.Y <= bottomBound;
    }
}