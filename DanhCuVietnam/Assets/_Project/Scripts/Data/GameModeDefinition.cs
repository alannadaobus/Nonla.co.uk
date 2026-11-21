using System.Collections.Generic;
using DanhCu.Core.Match;
using UnityEngine;

namespace DanhCu.Data
{
    /// <summary>Menu entry and default rules for a <see cref="GameModeType"/>.</summary>
    [CreateAssetMenu(menuName = "Danh Cu/Game Mode", fileName = "GameMode_")]
    public sealed class GameModeDefinition : DefinitionAsset
    {
        [SerializeField] private GameModeType modeType = GameModeType.SinglePlayer;
        [SerializeField] private LocalizedText description;

        [Header("Default rules")]
        [SerializeField] private MatchRuleset defaultRuleset = MatchRuleset.LongestSpin;
        [Range(MatchSettings.MinRoundsToWin, MatchSettings.MaxRoundsToWin)] [SerializeField] private int roundsToWin = 1;

        [Tooltip("Seconds per round; 0 = no limit.")]
        [Min(0f)] [SerializeField] private float roundTimeLimitSeconds;

        [Tooltip("Hidden from menus until the feature ships.")]
        [SerializeField] private bool isAvailable = true;

        public GameModeType ModeType => modeType;

        public LocalizedText Description => description;

        public MatchRuleset DefaultRuleset => defaultRuleset;

        public int RoundsToWin => roundsToWin;

        public float RoundTimeLimitSeconds => roundTimeLimitSeconds;

        public bool IsAvailable => isAvailable;

        public override void Validate(List<string> errors)
        {
            base.Validate(errors);
            Require(GameModeRules.SupportsRuleset(modeType, defaultRuleset), errors, $"ruleset {defaultRuleset} is not supported by {modeType}.");
        }

        internal void Configure(GameModeType newModeType, MatchRuleset newRuleset, int newRoundsToWin, bool available, LocalizedText newDescription)
        {
            modeType = newModeType;
            defaultRuleset = newRuleset;
            roundsToWin = newRoundsToWin;
            isAvailable = available;
            description = newDescription;
        }
    }
}
