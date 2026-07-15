using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Deucarian.Diagnostics.Editor
{
    public sealed class DiagnosticsWindow : EditorWindow
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
        private DeucarianEditorWorkbench workbench;
        private Button refreshButton;
        private Button runtimeOverlayButton;
        private Label toolbarSummary;
        private DeucarianEditorWorkbenchFooter footer;

        public static void OpenWindow()
        {
            DiagnosticsWindow window = GetWindow<DiagnosticsWindow>("Diagnostics");
            window.minSize = new Vector2(460f, 360f);
            window.RefreshReport();
            window.Show();
        }

        private void OnEnable()
        {
            RefreshReport();
        }

        private void OnDisable()
        {
            workbench?.Dispose();
            workbench = null;
            refreshButton = null;
            runtimeOverlayButton = null;
            toolbarSummary = null;
            footer = null;
        }

        public void CreateGUI()
        {
            workbench?.Dispose();
            workbench = DeucarianEditorWorkbench.Create(
                rootVisualElement,
                new DeucarianEditorWorkbenchOptions
                {
                    IncludeToolbar = true,
                    IncludeFooter = true,
                    TopSafeFadeName = WallpaperTopSafeFadeName
                });

            BuildToolbar();

            IMGUIContainer content = workbench.AddImGuiContent(DrawWorkbenchContent, ContentName);
            content.style.flexGrow = 1f;
            content.style.minHeight = 0f;

            footer = DeucarianEditorWorkbenchSurfaces.CreateFooter(
                string.Empty,
                string.Empty,
                string.Empty,
                "Copy JSON",
                HandleCopyJsonClicked,
                GetPackageVersionLabel());
            footer.Root.name = FooterName;
            footer.Summary.name = FooterSummaryName;
            footer.Action.name = CopyJsonButtonName;
            footer.Action.tooltip = "Copy the current diagnostics snapshot as JSON.";
            workbench.Footer.Add(footer.Root);

            UpdatePresentation();
        }

        private void OnFocus()
        {
            UpdatePresentation();
        }

        private void OnInspectorUpdate()
        {
            UpdateRuntimeOverlayPresentation();
        }

        private void BuildToolbar()
        {
            runtimeOverlayButton = DeucarianEditorWorkbenchToolbar.CreateToggleButton(
                "Runtime Overlay",
                HandleRuntimeOverlayClicked);
            runtimeOverlayButton.name = RuntimeOverlayButtonName;
            runtimeOverlayButton.tooltip = "Show or hide the runtime diagnostics overlay in the active scene.";
            workbench.Toolbar.Add(runtimeOverlayButton);

            toolbarSummary = DeucarianEditorWorkbenchToolbar.CreateSummary(string.Empty);
            toolbarSummary.name = ToolbarSummaryName;
            workbench.Toolbar.Add(toolbarSummary);
            workbench.Toolbar.Add(DeucarianEditorWorkbenchToolbar.CreateSpacer());

            refreshButton = DeucarianEditorWorkbenchToolbar.CreateActionButton(
                "Refresh",
                HandleRefreshClicked);
            refreshButton.name = RefreshButtonName;
            refreshButton.tooltip = "Build a fresh local diagnostics snapshot.";
            workbench.Toolbar.Add(refreshButton);
        }

        private void DrawWorkbenchContent()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            try
            {
                DrawSummary();
                DrawSections();
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawSummary()
        {
            DeucarianEditorWorkbenchGUI.DrawPanel("Summary", () =>
            {
                DiagnosticSeverity severity = report != null ? report.Severity : DiagnosticSeverity.Info;
                DeucarianEditorWorkbenchGUI.DrawStatusRow(
                    GetSeverityMarker(severity),
                    severity.ToString(),
                    ToEditorStatus(severity));
                DeucarianEditorWorkbenchGUI.DrawSeparator();
                DeucarianEditorWorkbenchGUI.DrawKeyValueRow("Sections", GetSectionCount().ToString());
                DeucarianEditorWorkbenchGUI.DrawKeyValueRow("Generated", GetGeneratedTimeLabel());
            });
        }

        private void DrawSections()
        {
            DeucarianEditorWorkbenchGUI.DrawPanel("Sections", () =>
            {
                if (report == null || report.Sections == null || report.Sections.Count == 0)
                {
                    DeucarianEditorChrome.DrawInlineHelp("No diagnostic providers are currently registered.", MessageType.Info);
                    return;
                }

                for (int i = 0; i < report.Sections.Count; i++)
                {
                    DiagnosticSection section = report.Sections[i];
                    if (section == null)
                    {
                        continue;
                    }

                    DeucarianEditorCards.DrawInlineCard(() =>
                    {
                        EditorGUILayout.BeginHorizontal();
                        try
                        {
                            EditorGUILayout.LabelField(section.Title ?? section.Id ?? string.Empty, EditorStyles.boldLabel);
                            DeucarianEditorStatusBadge.Draw(
                                section.Severity.ToString(),
                                ToEditorStatus(section.Severity),
                                GUILayout.Width(88));
                        }
                        finally
                        {
                            EditorGUILayout.EndHorizontal();
                        }

                        if (section.Items == null)
                        {
                            return;
                        }

                        for (int j = 0; j < section.Items.Count; j++)
                        {
                            DrawItem(section.Items[j]);
                        }
                    });
                }
            });
        }

        private static void DrawItem(DiagnosticItem item)
        {
            if (item == null)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            try
            {
                EditorGUILayout.LabelField(item.Label ?? item.Key, GUILayout.MinWidth(160));
                EditorGUILayout.LabelField(item.Value ?? string.Empty);
                DeucarianEditorStatusBadge.Draw(item.Severity.ToString(), ToEditorStatus(item.Severity), GUILayout.Width(88));
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }

            if (!string.IsNullOrWhiteSpace(item.Message))
            {
                EditorGUILayout.HelpBox(item.Message, ToMessageType(item.Severity));
            }
        }

        private void RefreshReport()
        {
            report = DiagnosticProviderRegistry.BuildReport();
            UpdatePresentation();
            Repaint();
        }

        private void HandleRefreshClicked()
        {
            copyStatus = null;
            RefreshReport();
        }

        private void HandleCopyJsonClicked()
        {
            if (report == null)
            {
                return;
            }

            DiagnosticsJsonExporter.CopyToClipboard(report);
            copyStatus = "JSON copied";
            UpdatePresentation();
        }

        private void HandleRuntimeOverlayClicked()
        {
            SetRuntimeOverlayVisibleInActiveScene(!IsRuntimeOverlayVisibleInActiveScene());
            UpdateRuntimeOverlayPresentation();
        }

        private void UpdatePresentation()
        {
            int sectionCount = GetSectionCount();
            DiagnosticSeverity severity = report != null ? report.Severity : DiagnosticSeverity.Info;

            if (toolbarSummary != null)
            {
                toolbarSummary.text = sectionCount + (sectionCount == 1 ? " section" : " sections")
                    + " · " + GetGeneratedTimeLabel();
                toolbarSummary.tooltip = toolbarSummary.text;
            }

            if (footer != null)
            {
                footer.StatusIcon.text = GetSeverityMarker(severity);
                footer.StatusLabel.text = severity.ToString();
                footer.Summary.text = string.IsNullOrWhiteSpace(copyStatus)
                    ? sectionCount + (sectionCount == 1 ? " diagnostic section" : " diagnostic sections")
                    : copyStatus;
                footer.Summary.tooltip = footer.Summary.text;
                footer.Action.SetEnabled(report != null);
                DeucarianEditorWorkbenchSurfaces.SetFooterStatus(footer, ToEditorStatus(severity));
            }

            UpdateRuntimeOverlayPresentation();
        }

        private void UpdateRuntimeOverlayPresentation()
        {
            if (runtimeOverlayButton == null)
            {
                return;
            }

            bool visible = IsRuntimeOverlayVisibleInActiveScene();
            DeucarianEditorWorkbenchToolbar.SetToggleActive(runtimeOverlayButton, visible);
            runtimeOverlayButton.text = visible ? "Runtime Overlay On" : "Runtime Overlay Off";
        }

        private int GetSectionCount()
        {
            return report != null && report.Sections != null ? report.Sections.Count : 0;
        }

        private string GetGeneratedTimeLabel()
        {
            return report == null
                ? "Not generated"
                : report.GeneratedAtUtc.ToUniversalTime().ToString("HH:mm:ss 'UTC'");
        }

        private static string GetPackageVersionLabel()
        {
            UnityEditor.PackageManager.PackageInfo packageInfo =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(DiagnosticReport).Assembly);
            string version = packageInfo != null ? packageInfo.version : "0.1.3";
            return "Diagnostics " + version;
        }

        private static string GetSeverityMarker(DiagnosticSeverity severity)
        {
            switch (severity)
            {
                case DiagnosticSeverity.Success:
                    return "✓";
                case DiagnosticSeverity.Warning:
                    return "!";
                case DiagnosticSeverity.Error:
                    return "×";
                default:
                    return "i";
            }
        }

        private static bool IsRuntimeOverlayVisibleInActiveScene()
        {
            RuntimeDiagnosticsOverlay[] overlays = FindRuntimeOverlaysInActiveScene();
            for (int i = 0; i < overlays.Length; i++)
            {
                RuntimeDiagnosticsOverlay overlay = overlays[i];
                if (overlay != null && overlay.isActiveAndEnabled)
                {
                    return true;
                }
            }

            return false;
        }

        private static void SetRuntimeOverlayVisibleInActiveScene(bool visible)
        {
            RuntimeDiagnosticsOverlay[] overlays = FindRuntimeOverlaysInActiveScene();

            if (visible)
            {
                RuntimeDiagnosticsOverlay overlay = overlays.Length > 0 ? overlays[0] : CreateRuntimeOverlayInActiveScene();
                if (overlay != null)
                {
                    SetRuntimeOverlayEnabled(overlay, true);
                }

                return;
            }

            for (int i = 0; i < overlays.Length; i++)
            {
                SetRuntimeOverlayEnabled(overlays[i], false);
            }
        }

        private static RuntimeDiagnosticsOverlay CreateRuntimeOverlayInActiveScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                DiagnosticsLog.Editor.Warning("Cannot create RuntimeDiagnosticsOverlay because there is no valid active scene.");
                return null;
            }

            GameObject prefab = FindPackageRuntimeOverlayPrefab();
            GameObject gameObject = null;
            bool persistSceneChanges = ShouldPersistSceneChanges();

            if (prefab != null)
            {
                gameObject = persistSceneChanges
                    ? PrefabUtility.InstantiatePrefab(prefab) as GameObject
                    : UnityEngine.Object.Instantiate(prefab);
                if (gameObject != null)
                {
                    gameObject.name = prefab.name;
                    RegisterCreatedObjectUndo(gameObject);
                    MoveToActiveScene(gameObject, activeScene);
                }
            }

            if (gameObject == null)
            {
                gameObject = new GameObject("Runtime Diagnostics Overlay");
                RegisterCreatedObjectUndo(gameObject);
                MoveToActiveScene(gameObject, activeScene);
                gameObject.AddComponent<RuntimeDiagnosticsOverlay>();
            }

            return gameObject.GetComponentInChildren<RuntimeDiagnosticsOverlay>(true);
        }

        private static void SetRuntimeOverlayEnabled(RuntimeDiagnosticsOverlay overlay, bool enabled)
        {
            if (overlay == null)
            {
                return;
            }

            RecordObjectUndo(overlay.gameObject);
            RecordObjectUndo(overlay);

            if (enabled && !overlay.gameObject.activeSelf)
            {
                overlay.gameObject.SetActive(true);
            }

            overlay.SetVisible(enabled);
            overlay.enabled = enabled;

            if (ShouldPersistSceneChanges())
            {
                EditorUtility.SetDirty(overlay.gameObject);
                EditorUtility.SetDirty(overlay);
                MarkSceneDirty(overlay.gameObject.scene);
            }
        }

        private static RuntimeDiagnosticsOverlay[] FindRuntimeOverlaysInActiveScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                return new RuntimeDiagnosticsOverlay[0];
            }

            RuntimeDiagnosticsOverlay[] allOverlays = Resources.FindObjectsOfTypeAll<RuntimeDiagnosticsOverlay>();
            List<RuntimeDiagnosticsOverlay> sceneOverlays = new List<RuntimeDiagnosticsOverlay>();

            for (int i = 0; i < allOverlays.Length; i++)
            {
                RuntimeDiagnosticsOverlay overlay = allOverlays[i];
                if (overlay == null
                    || EditorUtility.IsPersistent(overlay)
                    || overlay.gameObject.scene != activeScene)
                {
                    continue;
                }

                sceneOverlays.Add(overlay);
            }

            return sceneOverlays.ToArray();
        }

        private static GameObject FindPackageRuntimeOverlayPrefab()
        {
            UnityEditor.PackageManager.PackageInfo packageInfo =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(RuntimeDiagnosticsOverlay).Assembly);
            string packageRoot = packageInfo != null ? packageInfo.assetPath : "Packages/com.deucarian.diagnostics";

            if (string.IsNullOrWhiteSpace(packageRoot) || !AssetDatabase.IsValidFolder(packageRoot))
            {
                return null;
            }

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { packageRoot });
            GameObject firstOverlayPrefab = null;

            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab == null || prefab.GetComponentInChildren<RuntimeDiagnosticsOverlay>(true) == null)
                {
                    continue;
                }

                if (prefab.name == nameof(RuntimeDiagnosticsOverlay))
                {
                    return prefab;
                }

                if (firstOverlayPrefab == null)
                {
                    firstOverlayPrefab = prefab;
                }
            }

            return firstOverlayPrefab;
        }

        private static void MoveToActiveScene(GameObject gameObject, Scene activeScene)
        {
            if (gameObject != null && activeScene.IsValid() && gameObject.scene != activeScene)
            {
                SceneManager.MoveGameObjectToScene(gameObject, activeScene);
            }
        }

        private static void RegisterCreatedObjectUndo(GameObject gameObject)
        {
            if (gameObject != null && ShouldPersistSceneChanges())
            {
                Undo.RegisterCreatedObjectUndo(gameObject, RuntimeOverlayUndoName);
            }
        }

        private static void RecordObjectUndo(UnityEngine.Object target)
        {
            if (target != null && ShouldPersistSceneChanges())
            {
                Undo.RecordObject(target, RuntimeOverlayUndoName);
            }
        }

        private static bool ShouldPersistSceneChanges()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void MarkSceneDirty(Scene scene)
        {
            if (scene.IsValid() && ShouldPersistSceneChanges())
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        private static DeucarianEditorStatus ToEditorStatus(DiagnosticSeverity severity)
        {
            switch (severity)
            {
                case DiagnosticSeverity.Success:
                    return DeucarianEditorStatus.Success;
                case DiagnosticSeverity.Warning:
                    return DeucarianEditorStatus.Warning;
                case DiagnosticSeverity.Error:
                    return DeucarianEditorStatus.Error;
                default:
                    return DeucarianEditorStatus.Info;
            }
        }

        private static MessageType ToMessageType(DiagnosticSeverity severity)
        {
            switch (severity)
            {
                case DiagnosticSeverity.Warning:
                    return MessageType.Warning;
                case DiagnosticSeverity.Error:
                    return MessageType.Error;
                default:
                    return MessageType.Info;
            }
        }
    }
}
