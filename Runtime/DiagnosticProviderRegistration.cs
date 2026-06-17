using System;

namespace Deucarian.Diagnostics
{
    public sealed class DiagnosticProviderRegistration : IDisposable
    {
        private readonly IDiagnosticProvider provider;
        private bool disposed;

        internal DiagnosticProviderRegistration(IDiagnosticProvider provider)
        {
            this.provider = provider;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            DiagnosticProviderRegistry.Unregister(provider);
            disposed = true;
        }
    }
}
