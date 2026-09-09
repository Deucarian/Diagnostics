using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Deucarian.Diagnostics.Editor
{
    public sealed partial class DiagnosticsWindow : EditorWindow
    {
        private const string RuntimeOverlayUndoName = "Show Runtime Overlay";
        private const string RefreshButtonName = "diagnostics-refresh-button";
        private const string RuntimeOverlayButtonName = "diagnostics-runtime-overlay-toggle";
        private const string ToolbarSummaryName = "diagnostics-toolbar-summary";
        private const string ContentName = "diagnostics-workbench-content";
        private const string FooterName = "diagnostics-workbench-footer";
        private const string CopyJsonButtonName = "diagnostics-copy-json-button";
        private const string FooterSummaryName = "diagnostics-footer-summary";
        private const string WallpaperTopSafeFadeName = "diagnostics-wallpaper-top-safe-fade";

        private DiagnosticReport report;
        private Vector2 scrollPosition;
        private string copyStatus;
        private DeucarianEditorCollectionWorkspace workspace;
        private string selectedSection;
        private bool showAll;
        private string search = string.Empty;
        private Button refreshButton;
        private Button runtimeOverlayButton;
        private Label toolbarSummary;
        private Button copyButton;
    }
}
