using System.Collections.Generic;

namespace DanhCu.Core.Match
{
    /// <summary>
    /// Everything needed to start a match. Content is referenced by stable string ids
    /// (never by asset reference) so settings can be serialized, sent over the network
    /// and validated without the engine.
    /// </summary>
    public sealed class MatchSettings
    {
        public const int MinRoundsToWin = 1;
        public const int MaxRoundsToWin = 5;

        public GameModeType Mode { get; set; } = GameModeType.SinglePlayer;

        public MatchRuleset Ruleset { get; set; } = MatchRuleset.LongestSpin;

        public string ArenaId { get; set; } = string.Empty;

        public string PlayerTopId { get; set; } = string.Empty;

        /// <summary>Empty in solo practice.</summary>
        public string OpponentTopId { get; set; } = string.Empty;

        /// <summary>Only used in <see cref="GameModeType.VersusAI"/>.</summary>
        public string AIProfileId { get; set; } = string.Empty;

        public int RoundsToWin { get; set; } = 1;

        /// <summary>0 means no limit.</summary>
        public float RoundTimeLimitSeconds { get; set; }

        public MatchSettings Clone()
        {
            return (MatchSettings)MemberwiseClone();
        }

        /// <summary>Returns human-readable problems; empty when the settings are playable.</summary>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(ArenaId))
            {
                errors.Add("ArenaId is required.");
            }

            if (string.IsNullOrWhiteSpace(PlayerTopId))
            {
                errors.Add("PlayerTopId is required.");
            }

            if (GameModeRules.RequiresOpponentTop(Mode) && string.IsNullOrWhiteSpace(OpponentTopId))
            {
                errors.Add($"OpponentTopId is required in {Mode}.");
            }

            if (GameModeRules.RequiresAIOpponent(Mode) && string.IsNullOrWhiteSpace(AIProfileId))
            {
                errors.Add($"AIProfileId is required in {Mode}.");
            }

            if (!GameModeRules.SupportsRuleset(Mode, Ruleset))
            {
                errors.Add($"Ruleset {Ruleset} is not supported in {Mode}.");
            }

            if (RoundsToWin < MinRoundsToWin || RoundsToWin > MaxRoundsToWin)
            {
                errors.Add($"RoundsToWin must be between {MinRoundsToWin} and {MaxRoundsToWin}.");
            }

            if (RoundTimeLimitSeconds < 0f)
            {
                errors.Add("RoundTimeLimitSeconds cannot be negative.");
            }

            return errors;
        }

        public override string ToString()
        {
            return $"{Mode}/{Ruleset} arena={ArenaId} player={PlayerTopId} opponent={OpponentTopId} ai={AIProfileId} rounds={RoundsToWin}";
        }
    }
}
