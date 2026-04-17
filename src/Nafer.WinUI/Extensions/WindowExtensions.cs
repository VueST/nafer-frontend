using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;
using WinRT.Interop;

namespace Nafer.WinUI.Extensions;

public static class WindowExtensions
{
    internal static HWND GetWindowHandle(this Window window)
    {
        var handle = WindowNative.GetWindowHandle(window);
        return (HWND)handle;
    }

    public static unsafe bool UseImmersiveDarkModeEx(this Window window, bool enabled, bool activate = true)
    {
        var hwnd = window.GetWindowHandle();
        
        // This is the core Lively pattern: Set the attribute then force a refresh
        BOOL useDarkMode = enabled;
        var hr = PInvoke.DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, &useDarkMode, (uint)sizeof(BOOL));

        if (hr.Succeeded && activate)
        {
            // Lively's Magic Refresh: Briefly hide/show to force DWM to re-evaluate the frame
            PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_HIDE);
            PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_SHOW);
            return true;
        }
        return hr.Succeeded;
    }

    public static unsafe void SuppressNativeBorders(this Window window)
    {
        var hwnd = window.GetWindowHandle();
        
        // Kill the native white border (Win 11)
        uint borderColor = 0xFFFFFFFE; // DWMWA_COLOR_NONE
        PInvoke.DwmSetWindowAttribute(hwnd, (DWMWINDOWATTRIBUTE)34, &borderColor, (uint)sizeof(uint));

        // Caption Color Sync
        uint captionColor = 0x00000000; // Black
        PInvoke.DwmSetWindowAttribute(hwnd, (DWMWINDOWATTRIBUTE)35, &captionColor, (uint)sizeof(uint));
    }

    public static void SetDragRegionForCustomTitleBar(this Window window, 
        Grid appTitleBar,
        ColumnDefinition brandingCol,
        ColumnDefinition dragCol)
    {
        if (window.AppWindow.TitleBar is not { } titleBar) return;

        if (titleBar.ExtendsContentIntoTitleBar)
        {
            double scale = GetScaleAdjustment(window);
            
            var dragRects = new List<Windows.Graphics.RectInt32>();

            Windows.Graphics.RectInt32 dragRect;
            dragRect.X = (int)(brandingCol.ActualWidth * scale);
            dragRect.Y = 0;
            dragRect.Height = (int)(appTitleBar.ActualHeight * scale);
            dragRect.Width = (int)(dragCol.ActualWidth * scale);
            
            dragRects.Add(dragRect);
            titleBar.SetDragRectangles(dragRects.ToArray());
        }
    }

    private static double GetScaleAdjustment(Window window)
    {
        var hwnd = window.GetWindowHandle();
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        
        // Simplified DPI check matching Lively's logic
        uint dpi = PInvoke.GetDpiForWindow(hwnd);
        return dpi / 96.0;
    }
}
