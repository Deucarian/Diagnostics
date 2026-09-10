using System;
using System.Linq;
using System.Reflection;
using System.IO;
using Deucarian.Editor;
using Deucarian.Diagnostics.Editor;
using NUnit.Framework;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Diagnostics.Tests
{
    public sealed class DiagnosticsWindowWorkbenchTests
    {
        private DiagnosticsWindow window;
        private string previousClipboard;

        [Test]
        public void PackageExposesDirectCapabilityMenu()
        {
            Assert.AreEqual(
                "Tools/Deucarian/Diagnostics...",
                DiagnosticsMenu.MenuPath);
        }

        [SetUp]
        public void SetUp()
        {
            DiagnosticProviderRegistry.Clear();
            previousClipboard = GUIUtility.systemCopyBuffer;
        }

        [TearDown]
        public void TearDown()
        {
            if (window != null)
            {
                UnityEngine.Object.DestroyImmediate(window);
            }

            GUIUtility.systemCopyBuffer = previousClipboard;
            DiagnosticProviderRegistry.Clear();
        }

        [Test]
        public void CreateGuiBuildsSharedWorkspaceWithoutDuplicatedChrome()
        {
            CreateWindow();
            var root = window.rootVisualElement;
            Assert.NotNull(root.Q("workspace-navigation"));
            Assert.NotNull(root.Q("workspace-collection"));
            Assert.NotNull(root.Q("workspace-details"));
            Assert.NotNull(root.Q<Button>("diagnostics-copy-json-button"));
            Assert.NotNull(root.Q<Button>("diagnostics-refresh-button"));
            Assert.AreEqual("Runtime Overlay Off", root.Q<Button>("diagnostics-runtime-overlay-toggle").text);
            Assert.IsNull(typeof(DiagnosticsWindow).GetMethod("OnGUI", BindingFlags.Instance | BindingFlags.NonPublic));
        }

        [Test]
        public void NormalSectionsAreAvailableWithoutBeingShownAsWarnings()
        {
            DiagnosticProviderRegistry.Register(new WorkbenchProvider());
            CreateWindow();
            var rows = window.rootVisualElement.Q("workspace-collection-rows");
            Assert.AreEqual(0, rows.childCount);
            typeof(DiagnosticsWindow).GetField("showAll", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(window, true);
            Invoke(window, "RenderSections");
            Assert.AreEqual(1, rows.childCount);
        }

        [TestCase(600f, true)]
        [TestCase(1200f, false)]
        public void WorkspaceRespondsToNarrowWidths(float width, bool narrow)
        {
            CreateWindow();
            var collection = GetField<DeucarianEditorCollectionWorkspace>(window, "workspace");
            collection.Workspace.ApplyWidth(width);
            Assert.AreEqual(narrow, collection.Workspace.Root.ClassListContains("dw-narrow"));
        }

        [Test]
        public void RefreshHandlerRebuildsReportAndPresentation()
        {
            DiagnosticProviderRegistry.Register(new WorkbenchProvider());
            CreateWindow();
            Assert.AreEqual(1, GetField<DiagnosticReport>(window, "report").Sections.Count);

            DiagnosticProviderRegistry.Clear();
            Invoke(window, "HandleRefreshClicked");

            Assert.AreEqual(0, GetField<DiagnosticReport>(window, "report").Sections.Count);
            StringAssert.StartsWith(
                "0 sections",
                window.rootVisualElement.Q<Label>("diagnostics-toolbar-summary").text);
            Assert.AreEqual(
                "Local project · Edit Mode",
                window.rootVisualElement.Q<Label>("diagnostics-footer-summary").text);
        }

        [Test]
        public void CopyHandlerExportsCurrentReportAndUpdatesFooter()
        {
            DiagnosticProviderRegistry.Register(new WorkbenchProvider());
            CreateWindow();

            Invoke(window, "HandleCopyJsonClicked");

            StringAssert.Contains("\"id\": \"workbench\"", GUIUtility.systemCopyBuffer);
            Assert.AreEqual("JSON copied", GetField<string>(window, "copyStatus"));
            Assert.AreEqual(
                "JSON copied",
                window.rootVisualElement.Q<Label>("diagnostics-footer-summary").text);
        }

        private void CreateWindow()
        {
            window = ScriptableObject.CreateInstance<DiagnosticsWindow>();
            window.CreateGUI();
        }

        private static T GetField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, fieldName);
            return (T)field.GetValue(target);
        }

        private static void Invoke(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, methodName);
            method.Invoke(target, null);
        }

        private static string ReadPartialClassSource(string primaryPath)
        {
            string directory = Path.GetDirectoryName(primaryPath);
            string stem = Path.GetFileNameWithoutExtension(primaryPath);
            return string.Join(
                Environment.NewLine,
                Directory.GetFiles(directory, stem + "*.cs")
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(File.ReadAllText));
        }

        private sealed class WorkbenchProvider : IDiagnosticProvider
        {
            public string ProviderId => "workbench";
            public string DisplayName => "Workbench";

            public void Collect(DiagnosticReportBuilder builder)
            {
                builder.AddSection(ProviderId, DisplayName)
                    .AddItem("state", "State", "Ready", DiagnosticSeverity.Success);
            }
        }
    }
}
