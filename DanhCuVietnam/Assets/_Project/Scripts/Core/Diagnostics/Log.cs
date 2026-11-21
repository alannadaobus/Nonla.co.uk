using System;

namespace DanhCu.Core.Diagnostics
{
    /// <summary>Destination for engine-agnostic log output.</summary>
    public interface ILogSink
    {
        void Info(string message);

        void Warning(string message);

        void Error(string message);

        void Exception(Exception exception);
    }

    /// <summary>
    /// Logging facade for assemblies that cannot reference UnityEngine.
    /// The runtime installs a Unity sink at boot; tests may install a capturing sink.
    /// </summary>
    public static class Log
    {
        private static ILogSink _sink = new ConsoleLogSink();

        public static ILogSink Sink
        {
            get => _sink;
            set => _sink = value ?? new ConsoleLogSink();
        }

        public static void Info(string message) => _sink.Info(message);

        public static void Warning(string message) => _sink.Warning(message);

        public static void Error(string message) => _sink.Error(message);

        public static void Exception(Exception exception) => _sink.Exception(exception);

        private sealed class ConsoleLogSink : ILogSink
        {
            public void Info(string message) => Console.WriteLine("[DanhCu] " + message);

            public void Warning(string message) => Console.WriteLine("[DanhCu][Warning] " + message);

            public void Error(string message) => Console.Error.WriteLine("[DanhCu][Error] " + message);

            public void Exception(Exception exception) => Console.Error.WriteLine("[DanhCu][Exception] " + exception);
        }
    }
}
