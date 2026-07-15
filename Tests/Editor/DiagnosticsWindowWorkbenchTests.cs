using System;
using System.Linq;
using System.Reflection;
using Deucarian.Diagnostics.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Diagnostics.Tests
{
    public sealed class DiagnosticsWindowWorkbenchTests
    {
        private DiagnosticsWindow window;
        private string previousClipboard;

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
        public void CreateGuiBuildsSharedWorkbenchWithoutLegacyOnGuiHeader()
        {
            CreateWindow();

            VisualElement root = window.rootVisualElement;
            VisualElement shell = root.Q<VisualElement>(className: "deucarian-workbench");
            Button overlay = root.Q<Button>("diagnostics-runtime-overlay-toggle");
            Button refresh = root.Q<Button>("diagnostics-refresh-button");
            Button copy = root.Q<Button>("diagnostics-copy-json-button");

            Assert.NotNull(shell);
            Assert.NotNull(root.Q<VisualElement>("deucarian-workbench-toolbar"));
            Assert.NotNull(root.Q<IMGUIContainer>("diagnostics-workbench-content"));
            Assert.NotNull(root.Q<VisualElement>("diagnostics-workbench-footer"));
            Assert.NotNull(root.Q<Label>("diagnostics-toolbar-summary"));
            Assert.NotNull(overlay);
            Assert.NotNull(refresh);
            Assert.NotNull(copy);
            Assert.IsTrue(overlay.ClassListContains("deucarian-workbench-toolbar__toggle"));
            Assert.IsTrue(refresh.ClassListContains("deucarian-workbench-toolbar__action--standard"));
            Assert.IsTrue(copy.ClassListContains("deucarian-workbench-operation-footer__action"));
            Assert.IsNull(typeof(DiagnosticsWindow).GetMethod("OnGUI", BindingFlags.Instance | BindingFlags.NonPublic));
        }

        [TestCase(899f, "deucarian-responsive--narrow")]
        [TestCase(900f, "deucarian-responsive--compact")]
        [TestCase(1179f, "deucarian-responsive--compact")]
        [TestCase(1180f, "deucarian-responsive--wide")]
        public void WorkbenchUsesExactResponsiveBoundaryClasses(float width, string expectedClass)
        {
            CreateWindow();
            object workbench = GetField<object>(window, "workbench");
            MethodInfo applyResponsiveLayout = workbench.GetType().GetMethod("ApplyResponsiveLayout");
            VisualElement shell = window.rootVisualElement.Q<VisualElement>(className: "deucarian-workbench");

            Assert.NotNull(applyResponsiveLayout);
            applyResponsiveLayout.Invoke(workbench, new object[] { width });

            Assert.IsTrue(shell.ClassListContains(expectedClass));
            Assert.AreEqual(
                1,
                shell.GetClasses().Count(className => className.StartsWith("deucarian-responsive--", StringComparison.Ordinal)));
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
                "0 diagnostic sections",
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
