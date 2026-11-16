using System.Runtime.InteropServices;

namespace GustaMudBlazor;

[ComVisible(true)]
public class WinFormInterop(Action hideLoadingOverlay)
{
    public void HideLoadingOverlayFromJs() => hideLoadingOverlay();
}
