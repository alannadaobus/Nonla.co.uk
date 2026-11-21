namespace DanhCu.Core.Match
{
    /// <summary>Outcome of a finished match.</summary>
    public sealed class MatchResult
    {
        public const int Draw = -1;

        public MatchResult(int winnerSlot, int[] roundsWon, float durationSeconds)
        {
            WinnerSlot = winnerSlot;
            RoundsWon = roundsWon ?? System.Array.Empty<int>();
            DurationSeconds = durationSeconds;
        }

        /// <summary>Index of the winning participant (0 = local player), or <see cref="Draw"/>.</summary>
        public int WinnerSlot { get; }

        /// <summary>Rounds won per participant slot.</summary>
        public int[] RoundsWon { get; }

        public float DurationSeconds { get; }

        public bool IsDraw => WinnerSlot == Draw;
    }
}
