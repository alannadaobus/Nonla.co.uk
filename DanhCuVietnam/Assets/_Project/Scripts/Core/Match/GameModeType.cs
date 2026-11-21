namespace DanhCu.Core.Match
{
    /// <summary>How a match is played. Persisted in save data: never renumber.</summary>
    public enum GameModeType
    {
        /// <summary>Solo play / practice (Luyện tập). No opponent required.</summary>
        SinglePlayer = 0,

        /// <summary>Local match against an AI opponent (Đấu với máy).</summary>
        VersusAI = 1,

        /// <summary>Networked match against other players (Trực tuyến).</summary>
        OnlineMultiplayer = 2,
    }

    /// <summary>Win condition of a round. Persisted in save data: never renumber.</summary>
    public enum MatchRuleset
    {
        /// <summary>Quay lâu: the top that spins longest wins.</summary>
        LongestSpin = 0,

        /// <summary>Bổ cù: strike the opponent's spinning top with a thrown top to stop it or knock it over.</summary>
        Strike = 1,

        /// <summary>Đẩy vòng: knock the opponent's top out of the drawn ring.</summary>
        RingOut = 2,
    }

    /// <summary>Rules that depend only on the mode. Keep gameplay code asking these instead of comparing enums.</summary>
    public static class GameModeRules
    {
        public static bool RequiresNetwork(GameModeType mode) => mode == GameModeType.OnlineMultiplayer;

        public static bool RequiresAIOpponent(GameModeType mode) => mode == GameModeType.VersusAI;

        public static bool RequiresOpponentTop(GameModeType mode) => mode != GameModeType.SinglePlayer;

        /// <summary>Online matches cannot freeze time: other players keep simulating.</summary>
        public static bool CanFreezeSimulationOnPause(GameModeType mode) => mode != GameModeType.OnlineMultiplayer;

        /// <summary>Solo practice only supports the longest-spin ruleset (there is nothing to strike).</summary>
        public static bool SupportsRuleset(GameModeType mode, MatchRuleset ruleset)
        {
            return mode != GameModeType.SinglePlayer || ruleset == MatchRuleset.LongestSpin;
        }
    }
}
