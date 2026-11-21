namespace DanhCu.Core.Match
{
    /// <summary>
    /// Mutable runtime state that survives scene loads: the match being set up or played
    /// and the result of the last one. Owned by the GameManager; never stored in assets.
    /// </summary>
    public sealed class GameSession
    {
        /// <summary>Settings of the match being configured or currently played.</summary>
        public MatchSettings CurrentMatch { get; private set; }

        public MatchResult LastResult { get; private set; }

        public bool HasMatch => CurrentMatch != null;

        public GameModeType Mode => CurrentMatch?.Mode ?? GameModeType.SinglePlayer;

        /// <summary>Stores a copy so later edits by the caller cannot leak into a running match.</summary>
        public void SetMatch(MatchSettings settings)
        {
            CurrentMatch = settings?.Clone();
        }

        public void SetResult(MatchResult result)
        {
            LastResult = result;
        }

        public void Clear()
        {
            CurrentMatch = null;
            LastResult = null;
        }
    }
}
