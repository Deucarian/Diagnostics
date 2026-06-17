using UnityEngine;

namespace Deucarian.Diagnostics
{
    public sealed class RuntimeDiagnosticsOverlay : MonoBehaviour
    {
        private const float PanelMargin = 24f;
        private const float PanelWidth = 520f;
        private const float PanelHeight = 420f;

        [SerializeField] private bool visible = true;
        [SerializeField] private float refreshIntervalSeconds = 1f;

        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle mutedStyle;
        private GUIStyle sectionStyle;
        private DiagnosticReport report;
        private float nextRefreshAt;
        private Vector2 scroll;
        private string copyStatus;

        public void Refresh()
        {
            report = DiagnosticProviderRegistry.BuildReport();
            nextRefreshAt = Time.unscaledTime + Mathf.Max(0.1f, refreshIntervalSeconds);
        }

        public void SetVisible(bool isVisible)
        {
            visible = isVisible;
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            EnsureStyles();

            if (report == null || Time.unscaledTime >= nextRefreshAt)
            {
                Refresh();
            }

            float width = Mathf.Min(PanelWidth, Mathf.Max(300f, Screen.width - (PanelMargin * 2f)));
            float height = Mathf.Min(PanelHeight, Mathf.Max(260f, Screen.height - (PanelMargin * 2f)));
            Rect panel = new Rect(Screen.width - width - PanelMargin, PanelMargin, width, height);

            Color previous = GUI.color;
            GUI.color = GetPanelColor(report != null ? report.Severity : DiagnosticSeverity.Info);
            GUI.Box(panel, GUIContent.none, panelStyle);
            GUI.color = previous;

            GUILayout.BeginArea(new Rect(panel.x + 16f, panel.y + 14f, panel.width - 32f, panel.height - 28f));
            DrawHeader();
            DrawToolbar();
            GUILayout.Space(6f);
            scroll = GUILayout.BeginScrollView(scroll);
            DrawReport();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawHeader()
        {
            DiagnosticSeverity severity = report != null ? report.Severity : DiagnosticSeverity.Info;
            GUILayout.Label("Deucarian Diagnostics", titleStyle);
            GUILayout.Space(4f);
            GUILayout.Label(
                "Status: " + severity
                + "    Sections: " + CountSections()
                + "    Warnings: " + CountItems(DiagnosticSeverity.Warning)
                + "    Errors: " + CountItems(DiagnosticSeverity.Error),
                mutedStyle);
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", GUILayout.Width(92f), GUILayout.Height(24f)))
            {
                Refresh();
                copyStatus = null;
            }

            if (GUILayout.Button("Copy JSON", GUILayout.Width(100f), GUILayout.Height(24f)))
            {
                DiagnosticsJsonExporter.CopyToClipboard(report);
                copyStatus = "Copied";
            }

            if (!string.IsNullOrWhiteSpace(copyStatus))
            {
                GUILayout.Label(copyStatus, mutedStyle, GUILayout.Width(72f));
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawReport()
        {
            if (report == null || report.Sections == null || report.Sections.Count == 0)
            {
                GUILayout.Label("No diagnostic providers registered.", mutedStyle);
                return;
            }

            for (int i = 0; i < report.Sections.Count; i++)
            {
                DiagnosticSection section = report.Sections[i];
                if (section == null)
                {
                    continue;
                }

                GUILayout.Space(8f);
                GUILayout.Label(FormatValue(section.Title) + " [" + section.Severity + "]", sectionStyle);

                if (section.Items == null || section.Items.Count == 0)
                {
                    GUILayout.Label("No items.", mutedStyle);
                    continue;
                }

                for (int j = 0; j < section.Items.Count; j++)
                {
                    DrawItem(section.Items[j]);
                }
            }
        }

        private void DrawItem(DiagnosticItem item)
        {
            if (item == null)
            {
                return;
            }

            GUILayout.Label(
                FormatValue(item.Label ?? item.Key)
                + ": " + FormatValue(item.Value)
                + " (" + item.Severity + ")",
                labelStyle);

            if (!string.IsNullOrWhiteSpace(item.Message))
            {
                GUILayout.Label("  " + item.Message, mutedStyle);
            }
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = Texture2D.whiteTexture }
            };

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = Color.white },
                wordWrap = true
            };

            mutedStyle = new GUIStyle(labelStyle)
            {
                normal = { textColor = new Color(0.78f, 0.88f, 0.82f, 1f) }
            };

            sectionStyle = new GUIStyle(labelStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        private int CountSections()
        {
            return report != null && report.Sections != null ? report.Sections.Count : 0;
        }

        private int CountItems(DiagnosticSeverity severity)
        {
            if (report == null || report.Sections == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < report.Sections.Count; i++)
            {
                DiagnosticSection section = report.Sections[i];
                if (section == null || section.Items == null)
                {
                    continue;
                }

                for (int j = 0; j < section.Items.Count; j++)
                {
                    DiagnosticItem item = section.Items[j];
                    if (item != null && item.Severity == severity)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static Color GetPanelColor(DiagnosticSeverity severity)
        {
            switch (severity)
            {
                case DiagnosticSeverity.Error:
                    return new Color(0.20f, 0.06f, 0.06f, 0.92f);
                case DiagnosticSeverity.Warning:
                    return new Color(0.22f, 0.15f, 0.05f, 0.92f);
                case DiagnosticSeverity.Success:
                    return new Color(0.04f, 0.11f, 0.08f, 0.92f);
                default:
                    return new Color(0.04f, 0.10f, 0.13f, 0.92f);
            }
        }

        private static string FormatValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "none" : value;
        }
    }
}
