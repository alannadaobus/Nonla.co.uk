using DanhCu.Core.Match;
using NUnit.Framework;

namespace DanhCu.Tests.EditMode.Core
{
    public sealed class MatchSettingsTests
    {
        private static MatchSettings Valid(GameModeType mode)
        {
            return new MatchSettings
            {
                Mode = mode,
                Ruleset = mode == GameModeType.SinglePlayer ? MatchRuleset.LongestSpin : MatchRuleset.Strike,
                ArenaId = "san_dinh",
                PlayerTopId = "cu_go_oi",
                OpponentTopId = mode == GameModeType.SinglePlayer ? string.Empty : "cu_go_sen",
                AIProfileId = mode == GameModeType.VersusAI ? "ai_medium" : string.Empty,
                RoundsToWin = 2,
            };
        }

        [TestCase(GameModeType.SinglePlayer)]
        [TestCase(GameModeType.VersusAI)]
        [TestCase(GameModeType.OnlineMultiplayer)]
        public void ValidSettings_HaveNoErrors(GameModeType mode)
        {
            Assert.That(Valid(mode).Validate(), Is.Empty);
        }

        [Test]
        public void VersusAI_WithoutProfile_IsInvalid()
        {
            MatchSettings settings = Valid(GameModeType.VersusAI);
            settings.AIProfileId = string.Empty;

            Assert.That(settings.Validate(), Has.Some.Contains("AIProfileId"));
        }

        [Test]
        public void Online_WithoutOpponentTop_IsInvalid()
        {
            MatchSettings settings = Valid(GameModeType.OnlineMultiplayer);
            settings.OpponentTopId = string.Empty;

            Assert.That(settings.Validate(), Has.Some.Contains("OpponentTopId"));
        }

        [Test]
        public void SinglePlayer_StrikeRuleset_IsInvalid()
        {
            MatchSettings settings = Valid(GameModeType.SinglePlayer);
            settings.Ruleset = MatchRuleset.Strike;

            Assert.That(settings.Validate(), Has.Some.Contains("Ruleset"));
        }

        [TestCase(0)]
        [TestCase(6)]
        public void RoundsOutOfRange_IsInvalid(int rounds)
        {
            MatchSettings settings = Valid(GameModeType.VersusAI);
            settings.RoundsToWin = rounds;

            Assert.That(settings.Validate(), Has.Some.Contains("RoundsToWin"));
        }

        [Test]
        public void Clone_IsIndependent()
        {
            MatchSettings original = Valid(GameModeType.VersusAI);
            MatchSettings copy = original.Clone();
            copy.ArenaId = "other";

            Assert.That(original.ArenaId, Is.EqualTo("san_dinh"));
        }

        [Test]
        public void Session_StoresCopy()
        {
            var session = new GameSession();
            MatchSettings settings = Valid(GameModeType.VersusAI);
            session.SetMatch(settings);
            settings.ArenaId = "mutated";

            Assert.That(session.CurrentMatch.ArenaId, Is.EqualTo("san_dinh"));
            Assert.That(session.Mode, Is.EqualTo(GameModeType.VersusAI));
        }

        [Test]
        public void ModeRules_OnlineCannotFreezeOnPause()
        {
            Assert.That(GameModeRules.CanFreezeSimulationOnPause(GameModeType.OnlineMultiplayer), Is.False);
            Assert.That(GameModeRules.CanFreezeSimulationOnPause(GameModeType.VersusAI), Is.True);
            Assert.That(GameModeRules.CanFreezeSimulationOnPause(GameModeType.SinglePlayer), Is.True);
        }
    }
}
