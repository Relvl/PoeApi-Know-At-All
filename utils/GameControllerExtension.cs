using ExileCore;

namespace Know_At_All.utils;

public static class GameControllerExtension
{
    public static bool IsFunctionsReady(this GameController gameController, bool skipInventoryCheck = false)
    {
        if (gameController.Area?.CurrentArea is null) return false;
        if (gameController.IngameState?.IngameUi?.OpenRightPanel is null) return false;
        if (gameController.IngameState?.IngameUi?.OpenLeftPanel is null) return false;

        if (!gameController.Window.IsForeground()) return false;
        
        // if (gameController.IngameState.IngameUi.)

        if (gameController.IsLoading) return false;
        if (!gameController.InGame) return false;

        if (gameController.Area.CurrentArea.IsHideout) return false;
        if (gameController.Area.CurrentArea.IsTown) return false;

        if (!skipInventoryCheck)
        {
            if (gameController.IngameState.IngameUi.OpenLeftPanel.Address != 0) return false;
            if (gameController.IngameState.IngameUi.OpenRightPanel.Address != 0) return false;
            if (gameController.Game.IngameState.IngameUi.StashElement?.IsVisibleLocal ?? false) return false;
        }

        return true;
    }
}