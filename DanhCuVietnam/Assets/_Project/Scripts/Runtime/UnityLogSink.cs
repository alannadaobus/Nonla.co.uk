using System;
using DanhCu.Core.Diagnostics;
using UnityEngine;

namespace DanhCu.Runtime
{
    /// <summary>Routes <see cref="Log"/> from engine-agnostic assemblies to the Unity console.</summary>
    internal sealed class UnityLogSink : ILogSink
    {
        public void Info(string message) => Debug.Log("[DanhCu] " + message);

        public void Warning(string message) => Debug.LogWarning("[DanhCu] " + message);

        public void Error(string message) => Debug.LogError("[DanhCu] " + message);

        public void Exception(Exception exception) => Debug.LogException(exception);
    }
}
