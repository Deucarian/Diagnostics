using Deucarian.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Diagnostics.Editor
{
    public sealed class DiagnosticsWindow : EditorWindow
    {
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

            DeucarianEditorChrome.DrawPackageHeader(
                "diagnostics",
                "Deucarian Diagnostics",
                "Build local diagnostic snapshots from explicitly registered providers.");

            DrawToolbar();
            DrawSummary();
            DrawSections();

            DeucarianEditorChrome.DrawFooterVersion("com.deucarian.diagnostics", "0.1.0");
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            DeucarianEditorChrome.DrawSectionHeader("Snapshot");
            DeucarianEditorChrome.BeginSection();
            EditorGUILayout.BeginHorizontal();
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
            EditorGUILayout.EndHorizontal();
            DeucarianEditorChrome.EndSection();
        }

        private void DrawSummary()
        {
            DeucarianEditorChrome.DrawSectionHeader("Summary");
            DeucarianEditorChrome.BeginSection();
            DiagnosticSeverity severity = report != null ? report.Severity : DiagnosticSeverity.Info;
            DeucarianEditorStatusBadge.Draw(severity.ToString(), ToEditorStatus(severity), GUILayout.Width(100));
            EditorGUILayout.LabelField("Sections", report != null && report.Sections != null ? report.Sections.Count.ToString() : "0");
            DeucarianEditorChrome.EndSection();
        }

        private void DrawSections()
        {
            DeucarianEditorChrome.DrawSectionHeader("Sections");
            DeucarianEditorChrome.BeginSection();

            if (report == null || report.Sections == null || report.Sections.Count == 0)
            {
                DeucarianEditorChrome.DrawInlineHelp("No diagnostic providers are currently registered.", MessageType.Info);
                DeucarianEditorChrome.EndSection();
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
                EditorGUILayout.LabelField(section.Title, EditorStyles.boldLabel);
                DeucarianEditorStatusBadge.Draw(section.Severity.ToString(), ToEditorStatus(section.Severity), GUILayout.Width(88));
                EditorGUILayout.EndHorizontal();

                if (section.Items == null)
                {
                    continue;
                }

                for (int j = 0; j < section.Items.Count; j++)
                {
                    DrawItem(section.Items[j]);
                }
            }

            DeucarianEditorChrome.EndSection();
        }

        private static void DrawItem(DiagnosticItem item)
        {
            if (item == null)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(item.Label ?? item.Key, GUILayout.MinWidth(160));
            EditorGUILayout.LabelField(item.Value ?? string.Empty);
            DeucarianEditorStatusBadge.Draw(item.Severity.ToString(), ToEditorStatus(item.Severity), GUILayout.Width(88));
            EditorGUILayout.EndHorizontal();

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
