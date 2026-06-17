using UnityEditor;

namespace Deucarian.Diagnostics.Editor
{
    public static class DiagnosticsMenu
    {
        private const string MenuRoot = "Tools/Deucarian/Diagnostics/";

        [MenuItem(MenuRoot + "Diagnostics Window")]
        public static void OpenDiagnosticsWindow()
        {
            DiagnosticsWindow.OpenWindow();
        }
    }
}
