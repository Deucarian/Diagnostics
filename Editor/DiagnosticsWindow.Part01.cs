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
            DeucarianEditorWorkspace.ConfigureWindow(window);
            window.RefreshReport();
            window.Show();
        }

        private void OnEnable()
        {
            if (!Application.isBatchMode)
            {
                DeucarianEditorWorkspace.ConfigureWindow(this);
            }

            RefreshReport();
        }

        private void OnDisable()
        {
            workspace?.Dispose();
            workspace = null;
        }

        public void CreateGUI()
        {
            workspace?.Dispose();
            rootVisualElement.Clear();
            workspace = new DeucarianEditorCollectionWorkspace(rootVisualElement, Application.productName,
                "Diagnostics", "A local snapshot. Start with what needs attention.",
                DeucarianToolIds.Diagnostics, "Search diagnostic sections…");
            var shell = workspace.Workspace;
            refreshButton = DeucarianEditorWorkspaceControls.Button("Refresh snapshot", HandleRefreshClicked);
            refreshButton.name = RefreshButtonName;
            shell.PageActions.Add(refreshButton);
            copyButton = DeucarianEditorWorkspaceControls.Button("Copy JSON", HandleCopyJsonClicked);
            copyButton.name = CopyJsonButtonName;
            shell.PageActions.Add(copyButton);
            var tabs = new DeucarianEditorChoiceBar(new[] { "Needs attention", "All sections" }, showAll ? 1 : 0, true);
            tabs.Changed += value => { showAll = value == 1; RenderSections(); };
            shell.Tabs.Add(tabs);
            runtimeOverlayButton = DeucarianEditorWorkspaceControls.Button("Runtime overlay", HandleRuntimeOverlayClicked);
            runtimeOverlayButton.name = RuntimeOverlayButtonName;
            runtimeOverlayButton.tooltip = "Show or hide the runtime overlay in the active scene. In Edit Mode this changes the scene and supports Undo.";
            shell.Scope.Add(runtimeOverlayButton);
            toolbarSummary = DeucarianEditorWorkspaceControls.Label(string.Empty, "dw-muted");
            toolbarSummary.name = ToolbarSummaryName;
            shell.Scope.Add(toolbarSummary);
            shell.FooterLeading.name = FooterSummaryName;
            shell.Footer.name = FooterName;
            shell.FooterTrailing.text = GetPackageVersionLabel();
            shell.SearchField.RegisterValueChangedCallback(evt => { search = evt.newValue ?? ""; RenderSections(); });
            UpdatePresentation();
        }

        private void OnFocus() => UpdatePresentation();
        private void OnInspectorUpdate() => UpdateRuntimeOverlayPresentation();

        private void RenderSections()
        {
            if (workspace == null) return;
            var rows = new List<DeucarianEditorCollectionItem>();
            DiagnosticSection selected = null;
            if (report?.Sections != null)
            {
                for (int i = 0; i < report.Sections.Count; i++)
                {
                    var section = report.Sections[i];
                    if (section == null) continue;
                    string title = section.Title ?? section.Id ?? "Unnamed section";
                    bool attention = section.Severity >= DiagnosticSeverity.Warning;
                    if (!showAll && !attention) continue;
                    if (title.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) < 0 &&
                        (section.Id ?? "").IndexOf(search, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                    string id = i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    if (selectedSection == id) selected = section;
                    rows.Add(new DeucarianEditorCollectionItem(id, title,
                        (section.Items?.Count ?? 0) + " captured values",
                        attention ? section.Severity.ToString() : "Captured",
                        () => { selectedSection = id; RenderSections(); }));
                }
            }
            workspace.SetItems(rows, selected != null ? selectedSection : null,
                GetSectionCount() == 0 ? "No diagnostic providers are registered." :
                search.Length > 0 ? "No sections match your search." :
                showAll ? "No sections in this snapshot." : "No warnings or errors in this snapshot. All sections contains the captured details.");
            workspace.Details.Clear();
            var form = new DeucarianEditorWorkspaceForm(workspace.Details);
            if (selected == null)
            {
                form.Section("Snapshot details").Note(() => "Select a section to inspect its captured values. Refresh is explicit; this is not live telemetry.");
                return;
            }
            var values = form.Section(selected.Title ?? selected.Id ?? "Section");
            if (selected.Items != null)
                foreach (var item in selected.Items)
                {
                    if (item == null) continue;
                    var target = item;
                    values.ReadOnly("diagnostic-value-" + item.Key, item.Label ?? item.Key, () => target.Value);
                    if (!string.IsNullOrWhiteSpace(item.Message)) values.Note(() => target.Message);
                }
        }

        private void RefreshReport()
        {
            report = DiagnosticProviderRegistry.BuildReport();
            DiagnosticsControlCenterStatus.Publish(report);
            UpdatePresentation();
            Repaint();
        }

        private void HandleRefreshClicked() { copyStatus = null; RefreshReport(); }

        private void HandleCopyJsonClicked()
        {
            if (report == null) return;
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
            if (workspace == null) return;
            toolbarSummary.text = GetSectionCount() + " sections · Captured " + GetGeneratedTimeLabel();
            workspace.Workspace.FooterLeading.text = string.IsNullOrWhiteSpace(copyStatus)
                ? "Local project · " + (EditorApplication.isPlaying ? "Play Mode" : "Edit Mode") : copyStatus;
            copyButton.SetEnabled(report != null);
            RenderSections();
            UpdateRuntimeOverlayPresentation();
        }

        private void UpdateRuntimeOverlayPresentation()
        {
            if (runtimeOverlayButton == null) return;
            bool visible = IsRuntimeOverlayVisibleInActiveScene();
            runtimeOverlayButton.text = visible ? "Runtime Overlay On" : "Runtime Overlay Off";
            runtimeOverlayButton.EnableInClassList("dw-selected", visible);
        }

        private int GetSectionCount() => report?.Sections?.Count ?? 0;
        private string GetGeneratedTimeLabel() => report == null ? "Not generated" : report.GeneratedAtUtc.ToUniversalTime().ToString("HH:mm:ss 'UTC'");
        private static string GetPackageVersionLabel()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(DiagnosticReport).Assembly);
            return "Diagnostics " + (info != null ? info.version : "development");
        }

        private static bool IsRuntimeOverlayVisibleInActiveScene()
        {
            foreach (var overlay in FindRuntimeOverlaysInActiveScene())
                if (overlay != null && overlay.isActiveAndEnabled) return true;
            return false;
        }
    }
}
