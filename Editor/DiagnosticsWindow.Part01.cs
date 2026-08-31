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


        public static void OpenWindow()
        {
            DiagnosticsWindow window = GetWindow<DiagnosticsWindow>("Diagnostics");
            window.minSize = new Vector2(420f, 280f);
            ApplyPreferredSizeOnce(window);
            window.RefreshReport();
            window.Show();
        }

        private void OnEnable()
        {
            minSize = new Vector2(420f, 280f);
            if (!Application.isBatchMode)
            {
                ApplyPreferredSizeOnce(this);
            }

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
                    // Package headers are intentionally disabled for now. Keep the
                    // shared header implementation available for a future UI pass.
                    // IncludeHeader = true,
                    IncludeToolbar = true,
                    IncludeFooter = true,
                    // HeaderPackageKey = "diagnostics",
                    // HeaderTitle = "Deucarian Diagnostics",
                    // HeaderSubtitle = "Inspect local runtime health and export a diagnostic snapshot.",
                    ToolbarLayout = DeucarianEditorWorkbenchToolbarLayout.CompactSingleLine,
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
            DeucarianEditorCommandBar.ConfigureAction(
                footer.Action,
                DeucarianEditorIconIds.Copy,
                "Copy JSON",
                footer.Action.tooltip);
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
            DeucarianEditorCommandBarLanes lanes =
                DeucarianEditorCommandBar.CreateLanes(workbench.Toolbar);

            runtimeOverlayButton = DeucarianEditorCommandBar.CreateToggle(
                "Runtime Overlay",
                HandleRuntimeOverlayClicked,
                false,
                DeucarianEditorIconIds.Monitor,
                "Show or hide the runtime diagnostics overlay in the active scene.");
            runtimeOverlayButton.name = RuntimeOverlayButtonName;
            runtimeOverlayButton.tooltip = "Show or hide the runtime diagnostics overlay in the active scene.";
            DeucarianEditorCommandBar.SetMinimumWidth(runtimeOverlayButton, 160f);
            lanes.Leading.Add(runtimeOverlayButton);

            toolbarSummary = lanes.Summary;
            toolbarSummary.name = ToolbarSummaryName;

            refreshButton = DeucarianEditorCommandBar.CreateAction(
                DeucarianEditorIconIds.Refresh,
                "Refresh",
                HandleRefreshClicked,
                false,
                "Build a fresh local diagnostics snapshot.");
            refreshButton.name = RefreshButtonName;
            refreshButton.tooltip = "Build a fresh local diagnostics snapshot.";
            lanes.Trailing.Add(refreshButton);
        }

        private void DrawWorkbenchContent()
        {
            using (DeucarianEditorWorkbenchGUI.BeginEmbeddedPage(GUILayout.ExpandHeight(true)))
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
                    DrawEmptySectionsState();
                    return;
                }

                for (int i = 0; i < report.Sections.Count; i++)
                {
                    DiagnosticSection section = report.Sections[i];
                    if (section == null)
                    {
                        continue;
                    }

                    EditorGUILayout.BeginHorizontal();
                    try
                    {
                        EditorGUILayout.LabelField(
                            section.Title ?? section.Id ?? string.Empty,
                            DeucarianEditorWorkbenchGUI.BoldLabelStyle);
                        DeucarianEditorStatusBadge.Draw(
                            section.Severity.ToString(),
                            ToEditorStatus(section.Severity),
                            GUILayout.Width(88));
                    }
                    finally
                    {
                        EditorGUILayout.EndHorizontal();
                    }

                    if (section.Items != null)
                    {
                        for (int j = 0; j < section.Items.Count; j++)
                        {
                            DrawItem(section.Items[j]);
                        }
                    }

                    if (i < report.Sections.Count - 1)
                    {
                        DeucarianEditorWorkbenchGUI.DrawSeparator();
                        GUILayout.Space(4f);
                    }
                }
            });
        }

        private static void DrawEmptySectionsState()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                float iconSize = DeucarianEditorLayoutMetrics.IconSize;
                Rect iconRect = GUILayoutUtility.GetRect(
                    iconSize,
                    iconSize,
                    GUILayout.Width(iconSize));
                DeucarianEditorIcons.DrawIcon(
                    iconRect,
                    DeucarianEditorIcons.GetIcon(DeucarianEditorIconIds.Info),
                    DeucarianEditorTheme.MutedText);
                GUILayout.Space(DeucarianEditorLayoutMetrics.IconTextGap);
                EditorGUILayout.LabelField(
                    "No diagnostic providers are currently registered.",
                    DeucarianEditorWorkbenchGUI.WordWrappedMiniLabelStyle);
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
                EditorGUILayout.LabelField(
                    item.Label ?? item.Key,
                    DeucarianEditorWorkbenchGUI.LabelStyle,
                    GUILayout.MinWidth(160));
                EditorGUILayout.LabelField(
                    item.Value ?? string.Empty,
                    DeucarianEditorWorkbenchGUI.LabelStyle);
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
            DiagnosticsControlCenterStatus.Publish(report);
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
                footer.StatusLabel.text = severity.ToString();
                footer.Summary.text = string.IsNullOrWhiteSpace(copyStatus)
                    ? sectionCount + (sectionCount == 1 ? " diagnostic section" : " diagnostic sections")
                    : copyStatus;
                footer.Summary.tooltip = footer.Summary.text;
                footer.Action.SetEnabled(report != null);
                DeucarianEditorWorkbenchSurfaces.SetFooterIcon(
                    footer,
                    GetSeverityIconId(severity));
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
            DeucarianEditorCommandBar.SetActive(runtimeOverlayButton, visible);
            DeucarianEditorCommandBar.SetText(
                runtimeOverlayButton,
                visible ? "Runtime Overlay On" : "Runtime Overlay Off");
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
    }
}
