using DanhCu.Core.GameFlow;
using DanhCu.Core.Match;

namespace DanhCu.Core.Events
{
    /// <summary>Published after every top-level state change.</summary>
    public readonly struct GameStateChangedEvent
    {
        public GameStateChangedEvent(GameStateId from, GameStateId to)
        {
            From = from;
            To = to;
        }

        public GameStateId From { get; }

        public GameStateId To { get; }
    }

    /// <summary>Published when a match enters the Playing state for the first time.</summary>
    public readonly struct MatchStartedEvent
    {
        public MatchStartedEvent(MatchSettings settings)
        {
            Settings = settings;
        }

        public MatchSettings Settings { get; }
    }

    /// <summary>Published when a match finishes and results are available.</summary>
    public readonly struct MatchEndedEvent
    {
        public MatchEndedEvent(MatchResult result)
        {
            Result = result;
        }

        public MatchResult Result { get; }
    }

    /// <summary>Published when the pause menu opens or closes.</summary>
    public readonly struct PauseChangedEvent
    {
        public PauseChangedEvent(bool isPaused, bool simulationFrozen)
        {
            IsPaused = isPaused;
            SimulationFrozen = simulationFrozen;
        }

        public bool IsPaused { get; }

        /// <summary>False in online matches, where the simulation keeps running under the menu.</summary>
        public bool SimulationFrozen { get; }
    }

    /// <summary>Published while a scene load is in progress (0..1) and once more at 1 when done.</summary>
    public readonly struct LoadingProgressEvent
    {
        public LoadingProgressEvent(float progress)
        {
            Progress = progress;
        }

        public float Progress { get; }
    }
}
