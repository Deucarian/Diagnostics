using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deucarian.Diagnostics.Editor
{
    public sealed class DiagnosticsWindow : EditorWindow
    {
        private const string RuntimeOverlayUndoName = "Show Runtime Overlay";

        private DiagnosticReport report;
        private Vector2 scrollPosition;
        private string copyStatus;

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

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            try
            {
                DeucarianEditorChrome.DrawPackageHeader(
                    "diagnostics",
                    "Deucarian Diagnostics",
                    "Build local diagnostic snapshots from explicitly registered providers.");

                DrawToolbar();
                DrawRuntimeOverlayControls();
                DrawSummary();
                DrawSections();

                DeucarianEditorChrome.DrawFooterVersion("com.deucarian.diagnostics", "0.1.2");
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawToolbar()
        {
            DeucarianEditorChrome.DrawSectionHeader("Snapshot");
            DeucarianEditorChrome.BeginSection();
            try
            {
                EditorGUILayout.BeginHorizontal();
                try
                {
                    if (GUILayout.Button("Refresh", GUILayout.Width(96)))
                    {
                        RefreshReport();
                        copyStatus = null;
                    }

                    using (new EditorGUI.DisabledScope(report == null))
                    {
                        if (GUILayout.Button("Copy JSON", GUILayout.Width(96)))
                        {
                            DiagnosticsJsonExporter.CopyToClipboard(report);
                            copyStatus = "Copied";
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(copyStatus))
                    {
                        EditorGUILayout.LabelField(copyStatus, GUILayout.Width(72));
                    }

                    GUILayout.FlexibleSpace();
                }
                finally
                {
                    EditorGUILayout.EndHorizontal();
                }
            }
            finally
            {
                DeucarianEditorChrome.EndSection();
            }
        }

        private void DrawRuntimeOverlayControls()
        {
            DeucarianEditorChrome.DrawSectionHeader("Runtime Overlay");
            DeucarianEditorChrome.BeginSection();
            try
            {
                bool isVisible = IsRuntimeOverlayVisibleInActiveScene();

                EditorGUI.BeginChangeCheck();
                bool shouldShow = EditorGUILayout.ToggleLeft("Show Runtime Overlay", isVisible);
                if (EditorGUI.EndChangeCheck())
                {
                    SetRuntimeOverlayVisibleInActiveScene(shouldShow);
                }
            }
            finally
            {
                DeucarianEditorChrome.EndSection();
            }
        }

        private void DrawSummary()
        {
            DeucarianEditorChrome.DrawSectionHeader("Summary");
            DeucarianEditorChrome.BeginSection();
            try
            {
                DiagnosticSeverity severity = report != null ? report.Severity : DiagnosticSeverity.Info;
                DeucarianEditorStatusBadge.Draw(severity.ToString(), ToEditorStatus(severity), GUILayout.Width(100));
                EditorGUILayout.LabelField("Sections", report != null && report.Sections != null ? report.Sections.Count.ToString() : "0");
            }
            finally
            {
                DeucarianEditorChrome.EndSection();
            }
        }

        private void DrawSections()
        {
            DeucarianEditorChrome.DrawSectionHeader("Sections");
            DeucarianEditorChrome.BeginSection();
            try
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

                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal();
                    try
                    {
                        EditorGUILayout.LabelField(section.Title, EditorStyles.boldLabel);
                        DeucarianEditorStatusBadge.Draw(section.Severity.ToString(), ToEditorStatus(section.Severity), GUILayout.Width(88));
                    }
                    finally
                    {
                        EditorGUILayout.EndHorizontal();
                    }

                    if (section.Items == null)
                    {
                        continue;
                    }

                    for (int j = 0; j < section.Items.Count; j++)
                    {
                        DrawItem(section.Items[j]);
                    }
                }
            }
            finally
            {
                DeucarianEditorChrome.EndSection();
            }
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
            Repaint();
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
