using System;
using Deucarian.Diagnostics;
using UnityEngine;

namespace Deucarian.Diagnostics.Samples.DiagnosticsDemo
{
    public sealed class DiagnosticsDemoBootstrap : MonoBehaviour
    {
        [SerializeField] private bool addOverlay = true;

        private IDisposable registration;
        private RuntimeDiagnosticsOverlay overlay;

        private void OnEnable()
        {
            registration = DiagnosticProviderRegistry.Register(new ExampleDiagnosticProvider());

            if (addOverlay && overlay == null)
            {
                overlay = gameObject.AddComponent<RuntimeDiagnosticsOverlay>();
            }
        }

        private void OnDisable()
        {
            registration?.Dispose();
            registration = null;
        }
    }
}
