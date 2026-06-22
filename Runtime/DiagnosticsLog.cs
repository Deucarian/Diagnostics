using Deucarian.Logging;

namespace Deucarian.Diagnostics
{
    /// <summary>
    /// Package-level log categories for Diagnostics.
    /// </summary>
    public static class DiagnosticsLog
    {
        public static readonly DLog General = DLog.For("Diagnostics");
        public static readonly DLog Editor = DLog.For("Diagnostics.Editor");
    }
}
