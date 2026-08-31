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
        public void CreateGuiBuildsSharedCommandBarAndFooterWithoutPackageHeader()
        {
            CreateWindow();

            VisualElement root = window.rootVisualElement;
            VisualElement shell = root.Q<VisualElement>(className: "deucarian-workbench");
            VisualElement header = root.Q<VisualElement>(className: DeucarianEditorPackageHeader.RootClass);
            Button overlay = root.Q<Button>("diagnostics-runtime-overlay-toggle");
            Button refresh = root.Q<Button>("diagnostics-refresh-button");
            Button copy = root.Q<Button>("diagnostics-copy-json-button");

            Assert.NotNull(shell);
            Assert.IsNull(header);
            VisualElement toolbar = root.Q<VisualElement>("deucarian-workbench-toolbar");
            Assert.NotNull(toolbar);
            Assert.IsTrue(toolbar.ClassListContains(DeucarianEditorCommandBar.RootClass));
            VisualElement leadingLane = toolbar.Q<VisualElement>(
                className: DeucarianEditorCommandBar.LeadingLaneClass);
            Label summaryLane = toolbar.Q<Label>(
                className: DeucarianEditorCommandBar.SummaryLaneClass);
            VisualElement trailingLane = toolbar.Q<VisualElement>(
                className: DeucarianEditorCommandBar.TrailingLaneClass);
            Assert.NotNull(leadingLane);
            Assert.NotNull(summaryLane);
            Assert.NotNull(trailingLane);
            Assert.IsTrue(toolbar.ClassListContains(
                DeucarianEditorWorkbenchToolbar.CompactSingleLineClass));
            Assert.NotNull(root.Q<IMGUIContainer>("diagnostics-workbench-content"));
            Assert.NotNull(root.Q<VisualElement>("diagnostics-workbench-footer"));
            Assert.NotNull(root.Q<Label>("diagnostics-toolbar-summary"));
            Assert.NotNull(overlay);
            Assert.NotNull(refresh);
            Assert.NotNull(copy);
            Assert.AreSame(leadingLane, overlay.parent);
            Assert.AreSame(summaryLane, root.Q<Label>("diagnostics-toolbar-summary"));
            Assert.AreSame(trailingLane, refresh.parent);
            Assert.IsTrue(overlay.ClassListContains("deucarian-workbench-toolbar__toggle"));
            Assert.IsTrue(refresh.ClassListContains("deucarian-workbench-toolbar__action--standard"));
            Assert.IsTrue(copy.ClassListContains("deucarian-workbench-operation-footer__action"));
            Assert.IsTrue(overlay.ClassListContains(DeucarianEditorIconTextButton.RootClass));
            Assert.IsTrue(refresh.ClassListContains(DeucarianEditorIconTextButton.RootClass));
            Assert.IsTrue(copy.ClassListContains(DeucarianEditorIconTextButton.RootClass));
            Assert.IsTrue(overlay.ClassListContains(DeucarianEditorCommandBar.ToggleClass));
            Assert.IsTrue(refresh.ClassListContains(DeucarianEditorCommandBar.ActionClass));
            Assert.NotNull(overlay.Q<Image>(className: DeucarianEditorIconTextButton.IconClass));
            Assert.NotNull(refresh.Q<Image>(className: DeucarianEditorIconTextButton.IconClass));
            Assert.NotNull(copy.Q<Image>(className: DeucarianEditorIconTextButton.IconClass));
            Assert.NotNull(overlay.Q<VisualElement>(
                className: DeucarianEditorIconTextButton.GapClass));
            Assert.NotNull(refresh.Q<VisualElement>(
                className: DeucarianEditorIconTextButton.GapClass));
            Assert.NotNull(copy.Q<VisualElement>(
                className: DeucarianEditorIconTextButton.GapClass));
            Assert.AreEqual(
                "Runtime Overlay Off",
                overlay.Q<Label>(className: DeucarianEditorIconTextButton.LabelClass).text);
            Assert.AreEqual(8f, overlay.style.paddingLeft.value.value);
            Assert.AreEqual(8f, overlay.style.paddingRight.value.value);
            Assert.AreEqual(160f, overlay.style.minWidth.value.value);
            Assert.AreEqual(0f, overlay.style.flexShrink.value);
            Assert.AreEqual(
                DeucarianEditorLayoutMetrics.CommandControlHeight,
                overlay.style.height.value.value);
            Assert.AreEqual(
                DeucarianEditorLayoutMetrics.TextLineHeight,
                overlay.Q<Label>(className: DeucarianEditorIconTextButton.LabelClass)
                    .style.height.value.value);
            Assert.AreEqual(
                DeucarianEditorLayoutMetrics.TextLineHeight,
                toolbar.Q<Label>(className: DeucarianEditorCommandBar.SummaryLaneClass)
                    .style.height.value.value);
            Assert.AreEqual(
                PickingMode.Ignore,
                overlay.Q<VisualElement>(
                    className: DeucarianEditorIconTextButton.ContentClass).pickingMode);
            Assert.AreEqual(
                8f,
                copy.Q<VisualElement>(
                    className: DeucarianEditorIconTextButton.GapClass).style.width.value.value);
            Assert.AreEqual(new Vector2(420f, 280f), window.minSize);
            Assert.IsNull(typeof(DiagnosticsWindow).GetMethod("OnGUI", BindingFlags.Instance | BindingFlags.NonPublic));
        }

        [Test]
        public void EmptyAndSectionStatesAvoidNestedHelpAndCardSurfaces()
        {
            const string assetPath = "Packages/com.deucarian.diagnostics/Editor/DiagnosticsWindow.cs";
            PackageInfo package = PackageInfo.FindForAssetPath(assetPath);
            string absolutePath = package == null
                ? Path.GetFullPath("Editor/DiagnosticsWindow.cs")
                : Path.Combine(package.resolvedPath, "Editor/DiagnosticsWindow.cs");
            string source = ReadPartialClassSource(absolutePath);

            StringAssert.Contains("DrawEmptySectionsState", source);
            StringAssert.Contains("DeucarianEditorCommandBar", source);
            StringAssert.Contains("BeginEmbeddedPage", source);
            StringAssert.DoesNotContain("BeginSettingsPage", source);
            StringAssert.Contains("// IncludeHeader = true", source);
            StringAssert.Contains("DeucarianEditorIconIds.Info", source);
            StringAssert.Contains("DeucarianEditorLayoutMetrics.IconSize", source);
            StringAssert.Contains("DeucarianEditorLayoutMetrics.IconTextGap", source);
            StringAssert.Contains("DeucarianEditorWorkbenchGUI.BoldLabelStyle", source);
            StringAssert.Contains("DeucarianEditorWorkbenchGUI.WordWrappedMiniLabelStyle", source);
            StringAssert.DoesNotContain("DeucarianEditorChrome.DrawInlineHelp", source);
            StringAssert.DoesNotContain("DeucarianEditorCards.DrawInlineCard", source);
            StringAssert.DoesNotContain("EditorStyles.boldLabel", source);
            StringAssert.DoesNotContain("EditorStyles.wordWrappedMiniLabel", source);
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
