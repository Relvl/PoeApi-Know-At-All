using ExileCore;
using ExileCore.Shared.Nodes;

namespace Know_At_All;

public interface IModule
{
    string Name { get; }
    ToggleNode Expanded { get; }

    void Initialise()
    {
    }

    void AreaChange(AreaInstance area)
    {
    }

    void Tick()
    {
    }

    void Render()
    {
    }

    void DrawSettings();
}