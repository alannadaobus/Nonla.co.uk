using System;
using System.Collections;
using DanhCu.Core.GameFlow;
using DanhCu.Core.Match;
using DanhCu.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DanhCu.Tests.PlayMode
{
    /// <summary>
    /// End-to-end smoke test of the top-level flow with real scenes.
    /// Requires the project setup to have run (scenes + data + build settings).
    /// </summary>
    public sealed class GameFlowSmokeTests
    {
        private const float TimeoutSeconds = 15f;

        [UnityTest]
        public IEnumerator Boot_ReachesMainMenu_ThenOfflineMatchFlowWorks()
        {
            yield return WaitFor(() => GameManager.Instance != null, "GameManager was not bootstrapped");
            GameManager gm = GameManager.Instance;

            yield return WaitFor(() => gm.CurrentState == GameStateId.MainMenu, "Did not reach MainMenu");

            MatchSettings settings = gm.Config.CreateDefaultMatchSettings(GameModeType.VersusAI);
            Assert.That(gm.StartMatch(settings), Is.True, "StartMatch was refused");

            yield return WaitFor(() => gm.CurrentState == GameStateId.Playing, "Did not reach Playing");

            gm.TogglePause();
            Assert.That(gm.CurrentState, Is.EqualTo(GameStateId.Paused));
            Assert.That(Time.timeScale, Is.EqualTo(0f));

            gm.TogglePause();
            Assert.That(gm.CurrentState, Is.EqualTo(GameStateId.Playing));
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            gm.EndMatch(new MatchResult(0, new[] { 2, 0 }, 30f));
            Assert.That(gm.CurrentState, Is.EqualTo(GameStateId.MatchResult));
            Assert.That(gm.Session.LastResult.WinnerSlot, Is.EqualTo(0));

            Assert.That(gm.ReturnToMainMenu(), Is.True);
            yield return WaitFor(() => gm.CurrentState == GameStateId.MainMenu, "Did not return to MainMenu");
        }

        [UnityTest]
        public IEnumerator Physics_MaxAngularSpeed_IsRaisedAtBoot()
        {
            yield return WaitFor(() => GameManager.Instance != null, "GameManager was not bootstrapped");
            Assert.That(Physics.defaultMaxAngularSpeed, Is.GreaterThanOrEqualTo(300f));
        }

        private static IEnumerator WaitFor(Func<bool> condition, string failure)
        {
            float start = Time.realtimeSinceStartup;
            while (!condition())
            {
                if (Time.realtimeSinceStartup - start > TimeoutSeconds)
                {
                    Assert.Fail(failure);
                }

                yield return null;
            }
        }
    }
}
