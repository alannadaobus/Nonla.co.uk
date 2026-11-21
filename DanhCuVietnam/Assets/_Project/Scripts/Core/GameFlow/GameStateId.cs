namespace DanhCu.Core.GameFlow
{
    /// <summary>Top-level application states. See Docs/TECHNICAL_DESIGN.md §5.</summary>
    public enum GameStateId
    {
        /// <summary>Services are created and engine settings applied.</summary>
        Boot = 0,

        /// <summary>Title / main menu.</summary>
        MainMenu = 1,

        /// <summary>Choosing top, arena, opponent and rules for the next match.</summary>
        MatchSetup = 2,

        /// <summary>Online lobby / matchmaking (online mode only).</summary>
        OnlineLobby = 3,

        /// <summary>Asynchronous scene loading between major states.</summary>
        Loading = 4,

        /// <summary>A match is running.</summary>
        Playing = 5,

        /// <summary>Pause menu shown. Simulation is frozen only in offline modes.</summary>
        Paused = 6,

        /// <summary>Results of the finished match.</summary>
        MatchResult = 7,
    }
}
