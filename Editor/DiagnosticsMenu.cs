using UnityEditor;

namespace Deucarian.Diagnostics.Editor
{
    public static class DiagnosticsMenu
    {
        private const string MenuPath = "Tools/Deucarian/Tools and Quality/Diagnostics";

        [MenuItem(MenuPath)]
        public static void OpenDiagnosticsWindow()
        {
            DiagnosticsWindow.OpenWindow();
        }
    }
}
