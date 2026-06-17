using System.Collections.Generic;
using System.Reflection;
using Deucarian.Diagnostics.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deucarian.Diagnostics.Tests
{
    public sealed class DiagnosticsWindowRuntimeOverlayTests
    {
        private static readonly MethodInfo SetRuntimeOverlayVisibleMethod =
            typeof(DiagnosticsWindow).GetMethod(
                "SetRuntimeOverlayVisibleInActiveScene",
                BindingFlags.NonPublic | BindingFlags.Static);

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void SetRuntimeOverlayVisibleInActiveSceneCreatesOnlyOneOverlay()
        {
            SetRuntimeOverlayVisible(true);
            SetRuntimeOverlayVisible(true);

            RuntimeDiagnosticsOverlay[] overlays = FindActiveSceneOverlays();

            Assert.AreEqual(1, overlays.Length);
            Assert.IsTrue(overlays[0].isActiveAndEnabled);
        }

        [Test]
        public void SetRuntimeOverlayVisibleInActiveSceneDisablesOverlayWithoutDeleting()
        {
            SetRuntimeOverlayVisible(true);
            SetRuntimeOverlayVisible(false);

            RuntimeDiagnosticsOverlay[] overlays = FindActiveSceneOverlays();

            Assert.AreEqual(1, overlays.Length);
            Assert.IsFalse(overlays[0].enabled);
            Assert.IsFalse(overlays[0].isActiveAndEnabled);
        }

        private static void SetRuntimeOverlayVisible(bool visible)
        {
            Assert.IsNotNull(SetRuntimeOverlayVisibleMethod);
            SetRuntimeOverlayVisibleMethod.Invoke(null, new object[] { visible });
        }

        private static RuntimeDiagnosticsOverlay[] FindActiveSceneOverlays()
        {
            Scene activeScene = SceneManager.GetActiveScene();
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
    }
}
