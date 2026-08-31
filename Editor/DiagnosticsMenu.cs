using UnityEditor;

namespace Deucarian.Diagnostics.Editor
{
    public static class DiagnosticsMenu
    {
        public const string MenuPath = "Tools/Deucarian/Diagnostics...";

        [MenuItem(MenuPath)]
        public static void OpenDiagnosticsWindow()
        {
            DiagnosticsWindow.OpenWindow();
        }
    }
}
