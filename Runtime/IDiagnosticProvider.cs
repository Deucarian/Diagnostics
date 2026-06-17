namespace Deucarian.Diagnostics
{
    public interface IDiagnosticProvider
    {
        string ProviderId { get; }
        string DisplayName { get; }
        void Collect(DiagnosticReportBuilder builder);
    }
}
