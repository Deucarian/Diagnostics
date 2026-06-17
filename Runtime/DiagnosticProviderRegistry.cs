using System;
using System.Collections.Generic;

namespace Deucarian.Diagnostics
{
    public static class DiagnosticProviderRegistry
    {
        private static readonly object SyncRoot = new object();
        private static readonly List<IDiagnosticProvider> Providers = new List<IDiagnosticProvider>();

        public static DiagnosticProviderRegistration Register(IDiagnosticProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            lock (SyncRoot)
            {
                if (!Providers.Contains(provider))
                {
                    Providers.Add(provider);
                }
            }

            return new DiagnosticProviderRegistration(provider);
        }

        public static void Unregister(IDiagnosticProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            lock (SyncRoot)
            {
                Providers.Remove(provider);
            }
        }

        public static void Clear()
        {
            lock (SyncRoot)
            {
                Providers.Clear();
            }
        }

        public static IReadOnlyList<IDiagnosticProvider> SnapshotProviders()
        {
            lock (SyncRoot)
            {
                return Providers.ToArray();
            }
        }

        public static DiagnosticReport BuildReport()
        {
            return DiagnosticReportBuilder.BuildFrom(SnapshotProviders());
        }
    }
}
