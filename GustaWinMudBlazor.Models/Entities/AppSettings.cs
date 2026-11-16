namespace GustaMudBlazor.Models.Entities;

public class AppSettings
{
    public bool IsDarkMode { get; set; } = true;
    public AppWindowState WindowState { get; set; } = AppWindowState.Normal;
}

public enum AppWindowState
{
    Normal,
    Minimized,
    Maximized
}
