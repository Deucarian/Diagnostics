using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Deucarian.Diagnostics.Editor
{
    public sealed partial class DiagnosticsWindow
    {


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

        private static string GetSeverityIconId(DiagnosticSeverity severity)
        {
            switch (severity)
            {
                case DiagnosticSeverity.Success:
                    return DeucarianEditorIconIds.Check;
                case DiagnosticSeverity.Warning:
                case DiagnosticSeverity.Error:
                    return DeucarianEditorIconIds.Warning;
                default:
                    return DeucarianEditorIconIds.Info;
            }
        }

    }
}
