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
            DiagnosticsWindow window = DeucarianEditorWindowPages.GetStandalone<DiagnosticsWindow>("Diagnostics");
            window.navigation?.Navigate(DeucarianToolIds.Diagnostics);
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
            navigation?.Dispose();
            navigation = null;
            workspace?.Dispose();
            workspace = null;
        }
        public void CreateGUI()
        {
            navigation?.Dispose();
            navigation = new DeucarianEditorPageSession(this, DeucarianToolIds.Diagnostics, BuildPage);
        }

        internal static IDeucarianEditorPage CreatePage() =>
            DeucarianEditorWindowPages.Create<DiagnosticsWindow>(
                (window, root) => window.BuildPage(root), activate: (window, route) => window.UpdatePresentation(), update: window => window.UpdateRuntimeOverlayPresentation());

        private DeucarianEditorPageSession navigation;
        private VisualElement pageRoot;
        private VisualElement PageRoot => pageRoot ?? rootVisualElement;

        private void BuildPage(VisualElement root)
        {
            pageRoot = root;
            workspace?.Dispose();
            PageRoot.Clear();
            workspace = new DeucarianEditorCollectionWorkspace(PageRoot, Application.productName,
                "Diagnostics", "See what needs attention.",
                DeucarianToolIds.Diagnostics, "Find a tool…");
            workspace.UsePanels();
            workspace.Collection.AddToClassList("dw-balanced-panels");
            var shell = workspace.Workspace;
            shell.SetScopeBeforeTabs();
            refreshButton = DeucarianEditorWorkspaceControls.IconButton("Refresh", DeucarianEditorIconIds.Refresh, HandleRefreshClicked);
            refreshButton.name = RefreshButtonName;
            copyButton = DeucarianEditorWorkspaceControls.Button("Copy JSON", HandleCopyJsonClicked);
            copyButton.name = CopyJsonButtonName;
            var filter = new PopupField<string>(new List<string> { "All providers", "Needs attention" }, showAll ? 0 : 1);
            filter.RegisterValueChangedCallback(_ => { showAll = filter.index == 0; selectedSection = null; RenderSections(); });
            shell.Scope.AddToClassList("dw-filter-scope");
            var filters = DeucarianEditorWorkspaceControls.Region("diagnostics-filters", "dw-filter-toolbar");
            filters.Add(DeucarianEditorWorkspaceControls.Field("Scope", filter));
            var providerSearch = DeucarianEditorSearchField.Create("Find a provider…", value => { search = value ?? ""; RenderSections(); }, search);
            providerSearch.name = "diagnostics-provider-search";
            filters.Add(providerSearch);
            var refreshIcon = DeucarianEditorWorkspaceControls.IconButton(string.Empty, DeucarianEditorIconIds.Refresh, HandleRefreshClicked);
            refreshIcon.tooltip = "Refresh snapshot";
            filters.Add(refreshIcon);
            shell.Scope.Add(filters);
            health = new DeucarianEditorStatusSummary("diagnostics-health");
            health.Root.AddToClassList("dw-status-compact");
            shell.Content.Insert(0, health.Root);
            detailActions = DeucarianEditorWorkspaceControls.EndActions(refreshButton,
                DeucarianEditorWorkspaceControls.IconButton("Export snapshot", DeucarianEditorIconIds.Download, ExportJson, DeucarianEditorButtonRole.Primary));
            detailActions.AddToClassList("dw-collection-footer");
            exportStatus = DeucarianEditorWorkspaceControls.Label(string.Empty, "dw-muted");
            var advanced = new Foldout { text = "More options", value = false };
            advanced.AddToClassList("dw-foldout");
            detailOptions = advanced;
            runtimeOverlayButton = DeucarianEditorWorkspaceControls.Button("Runtime overlay", HandleRuntimeOverlayClicked);
            runtimeOverlayButton.name = RuntimeOverlayButtonName;
            runtimeOverlayButton.tooltip = "Show or hide the runtime overlay in the active scene. In Edit Mode this changes the scene and supports Undo.";
            advanced.Add(DeucarianEditorWorkspaceControls.Actions(runtimeOverlayButton, copyButton));
            toolbarSummary = DeucarianEditorWorkspaceControls.Label(string.Empty, "dw-muted");
            toolbarSummary.name = ToolbarSummaryName;
            advanced.Add(toolbarSummary);
            shell.FooterLeading.name = FooterSummaryName;
            shell.Footer.name = FooterName;
            DeucarianEditorWorkspaceControls.Show(shell.Footer, false);
            DeucarianEditorWorkspaceNavigation.Populate(shell, DeucarianToolIds.Diagnostics);
            UpdatePresentation();
        }

        private void OnFocus() => UpdatePresentation();
        private void OnInspectorUpdate() => UpdateRuntimeOverlayPresentation();

        private void RenderSections()
        {
            if (workspace == null) return;
            var rows = new List<DeucarianEditorCollectionItem>();
            DiagnosticSection selected = null;
            string firstId = null;
            DiagnosticSection first = null;
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
                    if (first == null) { first = section; firstId = id; }
                    if (selectedSection == id) selected = section;
                    rows.Add(new DeucarianEditorCollectionItem(id, title, string.Empty,
                        attention ? section.Severity.ToString() : "No issues",
                        () => { selectedSection = id; RenderSections(); }, iconId: SectionIcon(section)));
                }
            }
            if (selected == null) { selected = first; selectedSection = firstId; }
            workspace.SetItems(rows, selected != null ? selectedSection : null,
                GetSectionCount() == 0 ? "No diagnostic providers are registered." :
                search.Length > 0 ? "No sections match your search." :
                showAll ? "No sections in this snapshot." : "No warnings or errors in this snapshot. All sections contains the captured details.");
            workspace.Details.Clear();
            var form = new DeucarianEditorWorkspaceForm(workspace.Details);
            if (selected == null)
            {
                form.Section("Snapshot details").Note(() => "Select a section to inspect its captured values. Refresh is explicit; this is not live telemetry.");
                AppendDetailActions();
                return;
            }
            var detailTitle = DeucarianEditorWorkspaceControls.Label(selected.Title ?? selected.Id ?? "Section", "dw-feature-title");
            var header = DeucarianEditorWorkspaceControls.IconPanel("diagnostics-detail-heading", SectionIcon(selected), detailTitle);
            header.AddToClassList("dw-panel-flush");
            workspace.Details.Add(header);
            workspace.Details.Add(DeucarianEditorWorkspaceControls.Divider());
            var propertyList = DeucarianEditorWorkspaceControls.Region(null, "dw-property-list");
            workspace.Details.Add(propertyList);
            var values = new DeucarianEditorWorkspaceForm(propertyList);
            if (selected.Items != null)
                foreach (var item in selected.Items)
                {
                    if (item == null) continue;
                    var target = item;
                    values.ReadOnly("diagnostic-value-" + item.Key, item.Label ?? item.Key, () => target.Value);
                    if (!string.IsNullOrWhiteSpace(item.Message)) values.Note(() => target.Message);
                }
            AppendDetailActions();
        }

        private void AppendDetailActions()
        {
            workspace.Details.hierarchy.Add(detailActions);
            workspace.Details.Add(exportStatus);
            workspace.Details.Add(detailOptions);
        }

        private static string SectionIcon(DiagnosticSection section) =>
            (section.Id ?? "").IndexOf("notification", System.StringComparison.OrdinalIgnoreCase) >= 0
                ? DeucarianEditorIconIds.Notifications : DeucarianEditorIconIds.Activity;

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

        private void ExportJson()
        {
            if (report == null) return;
            string path = EditorUtility.SaveFilePanel("Export diagnostics", "", "diagnostics.json", "json");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                System.IO.File.WriteAllText(path, DiagnosticsJsonExporter.ToJson(report));
                copyStatus = "Snapshot exported";
            }
            catch (System.Exception error) { copyStatus = "Export failed: " + error.Message; }
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
            exportStatus.text = copyStatus ?? string.Empty;
            DeucarianEditorWorkspaceControls.Show(exportStatus, !string.IsNullOrEmpty(copyStatus));
            bool available = GetSectionCount() > 0;
            bool attention = report != null && report.Severity >= DiagnosticSeverity.Warning;
            health.Set(!available ? "No providers yet" : attention ? "Needs your attention" : "No issues reported",
                !available ? "Installed providers will appear here when they register."
                : string.Empty,
                available ? ToEditorStatus(report.Severity) : DeucarianEditorStatus.Info);
            health.Root.tooltip = GetSectionCount() + " providers captured · " + GetGeneratedTimeLabel();
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
