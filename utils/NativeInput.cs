using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using SharpDX;
using Vector2 = System.Numerics.Vector2;

namespace Know_At_All.utils;

public static class NativeInput
{
    private const int MouseeventfLeftdown = 0x02;
    private const int MouseeventfLeftup = 0x04;

    public const int MouseeventfMiddown = 0x0020;
    public const int MouseeventfMidup = 0x0040;

    private const int MouseeventfRightdown = 0x0008;
    private const int MouseeventfRightup = 0x0010;
    private const int MouseEventWheel = 0x800;

    // 
    private const int MovementDelay = 10;

    private const int ClickDelay = 1;

    private const int KeyeventfExtendedkey = 0x0001;
    private const int KeyeventfKeyup = 0x0002;
    private const int KeyPressed = 0x8000;
    private const int KeyToggled = 0x0001;

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern bool BlockInput(bool block);

    [DllImport("user32.dll")]
    private static extern uint keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);


    public static void KeyDown(Keys key)
    {
        keybd_event((byte)key, 0, KeyeventfExtendedkey | 0, 0);
    }

    public static void KeyUp(Keys key)
    {
        keybd_event((byte)key, 0, KeyeventfExtendedkey | KeyeventfKeyup, 0);
    }

    public static void KeyPress(Keys key)
    {
        KeyDown(key);
        Thread.Sleep(50);
        KeyUp(key);
    }

    public static void KeyPress(Keys key, int delay)
    {
        KeyDown(key);
        Thread.Sleep(delay);
        KeyUp(key);
    }

    public static bool IsKeyDown(Keys key)
    {
        return GetKeyState((int)key) < 0;
    }

    public static bool IsKeyPressed(Keys key)
    {
        return Convert.ToBoolean(GetKeyState((int)key) & KeyPressed);
    }


    public static bool IsKeyToggled(Keys key)
    {
        return Convert.ToBoolean(GetKeyState((int)key) & KeyToggled);
    }

    public static void blockInput(bool block)
    {
        BlockInput(block);
    }


    /// <summary>
    ///     Sets the cursor position relative to the game window.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="gameWindow"></param>
    /// <returns></returns>
    public static bool SetCursorPos(int x, int y, RectangleF gameWindow)
    {
        return SetCursorPos(x + (int)gameWindow.X, y + (int)gameWindow.Y);
    }

    /// <summary>
    ///     Sets the cursor position to the center of a given rectangle relative to the game window
    /// </summary>
    /// <param name="position"></param>
    /// <param name="gameWindow"></param>
    /// <returns></returns>
    public static bool SetCurosPosToCenterOfRec(RectangleF position, RectangleF gameWindow)
    {
        return SetCursorPos((int)(gameWindow.X + position.Center.X),
            (int)(gameWindow.Y + position.Center.Y));
    }

    /// <summary>
    ///     Retrieves the cursor's position, in screen coordinates.
    /// </summary>
    /// <see>See MSDN documentation for further information.</see>
    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT lpPoint);

    public static Point GetCursorPosition()
    {
        GetCursorPos(out var lpPoint);
        return lpPoint;
    }

    public static void LeftMouseDown()
    {
        mouse_event(MouseeventfLeftdown, 0, 0, 0, 0);
    }

    public static void LeftMouseUp()
    {
        mouse_event(MouseeventfLeftup, 0, 0, 0, 0);
    }

    public static void RightMouseDown()
    {
        mouse_event(MouseeventfRightdown, 0, 0, 0, 0);
    }

    public static void RightMouseUp()
    {
        mouse_event(MouseeventfRightup, 0, 0, 0, 0);
    }

    public static void SetCursorPosAndLeftClick(Vector2 pos, int extraDelay, Vector2 offset)
    {
        var posX = (int)(pos.X + offset.X);
        var posY = (int)(pos.Y + offset.Y);
        SetCursorPos(posX, posY);
        Thread.Sleep(MovementDelay + extraDelay);
        LeftClick();
    }

    public static void SetCursorPosAndRightClick(Vector2 pos, int extraDelay, Vector2 offset)
    {
        var posX = (int)(pos.X + offset.X);
        var posY = (int)(pos.Y + offset.Y);
        SetCursorPos(posX, posY);
        Thread.Sleep(MovementDelay + extraDelay);
        RightClick();
    }

    public static void VerticalScroll(bool forward, int clicks)
    {
        if (forward)
            mouse_event(MouseEventWheel, 0, 0, clicks * 120, 0);
        else
            mouse_event(MouseEventWheel, 0, 0, -(clicks * 120), 0);
    }

    public static void LeftClick()
    {
        LeftMouseDown();
        Thread.Sleep(ClickDelay);
        LeftMouseUp();
    }

    public static void RightClick()
    {
        RightMouseDown();
        Thread.Sleep(ClickDelay);
        RightMouseUp();
    }
    ////////////////////////////////////////////////////////////


    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public static implicit operator Point(POINT point)
        {
            return new Point(point.X, point.Y);
        }
    }
}