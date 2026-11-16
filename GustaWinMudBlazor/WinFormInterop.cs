using GustaMudBlazor.Models.Interfaces;
using System.Runtime.InteropServices;

namespace GustaMudBlazor;

[ComVisible(true)]
public class WinFormInterop(IWindowService windowService, Action hideLoadingOverlay)
{
    public void ToggleFullScreenFromJs() => windowService.ToggleFullScreen();

    public void ExitFullScreenFromJs() => windowService.ExitFullScreen();

    public bool IsFullScreen() => windowService.IsFullScreen;

    public void HideLoadingOverlayFromJs() => hideLoadingOverlay();
}
