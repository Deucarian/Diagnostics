using System;
using Deucarian.Logging;

namespace Deucarian.Diagnostics
{
    public sealed class DeucarianLoggingDiagnosticsInstaller : IDisposable
    {
        private readonly DiagnosticProviderRegistration providerRegistration;
        private bool disposed;

        private DeucarianLoggingDiagnosticsInstaller(RingBufferLogSink ringBuffer,
                                                     DeucarianLoggingDiagnosticProvider provider,
                                                     DiagnosticProviderRegistration providerRegistration)
        {
            RingBuffer = ringBuffer;
            Provider = provider;
            this.providerRegistration = providerRegistration;
        }

        public RingBufferLogSink RingBuffer { get; }

        public DeucarianLoggingDiagnosticProvider Provider { get; }

        public static DeucarianLoggingDiagnosticsInstaller Install(int capacity = RingBufferLogSink.DefaultCapacity,
                                                                   int maxRecentEntries = 20)
        {
            RingBufferLogSink ringBuffer = new RingBufferLogSink(capacity);
            DeucarianLoggingDiagnosticProvider provider =
                new DeucarianLoggingDiagnosticProvider(ringBuffer, maxRecentEntries);

            DeucarianLog.RegisterSink(ringBuffer);
            DiagnosticProviderRegistration registration = DiagnosticProviderRegistry.Register(provider);
            return new DeucarianLoggingDiagnosticsInstaller(ringBuffer, provider, registration);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            providerRegistration.Dispose();
            DeucarianLog.UnregisterSink(RingBuffer);
            disposed = true;
        }
    }
}
