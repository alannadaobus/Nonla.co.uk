using System;
using System.Collections.Generic;
using DanhCu.Core.GameFlow;
using DanhCu.Core.StateMachines;
using NUnit.Framework;

namespace DanhCu.Tests.EditMode.Core
{
    public sealed class GameStateMachineTests
    {
        private sealed class NullState : IState
        {
            public void Enter()
            {
            }

            public void Tick(float deltaTime)
            {
            }

            public void Exit()
            {
            }
        }

        private static GameStateMachine CreateStarted()
        {
            var machine = new GameStateMachine();
            foreach (GameStateId id in (GameStateId[])Enum.GetValues(typeof(GameStateId)))
            {
                machine.Register(id, new NullState());
            }

            machine.Start();
            return machine;
        }

        [Test]
        public void Start_WithMissingState_Throws()
        {
            var machine = new GameStateMachine();
            machine.Register(GameStateId.Boot, new NullState());

            Assert.Throws<InvalidOperationException>(() => machine.Start());
        }

        [Test]
        public void Start_BeginsInBoot()
        {
            Assert.That(CreateStarted().Current, Is.EqualTo(GameStateId.Boot));
        }

        [Test]
        public void HappyPath_OfflineMatch_FollowsTable()
        {
            GameStateMachine machine = CreateStarted();
            var path = new[]
            {
                GameStateId.Loading,
                GameStateId.MainMenu,
                GameStateId.MatchSetup,
                GameStateId.Loading,
                GameStateId.Playing,
                GameStateId.Paused,
                GameStateId.Playing,
                GameStateId.MatchResult,
                GameStateId.Loading,
                GameStateId.Playing,
            };

            foreach (GameStateId step in path)
            {
                Assert.That(machine.RequestTransition(step), Is.True, $"Expected {machine.Current} -> {step} to be allowed");
            }
        }

        [Test]
        public void OnlinePath_GoesThroughLobby()
        {
            GameStateMachine machine = CreateStarted();
            machine.RequestTransition(GameStateId.MainMenu);

            Assert.That(machine.RequestTransition(GameStateId.OnlineLobby), Is.True);
            Assert.That(machine.RequestTransition(GameStateId.MatchSetup), Is.True);
        }

        [TestCase(GameStateId.MainMenu, GameStateId.Playing)]
        [TestCase(GameStateId.MainMenu, GameStateId.Paused)]
        [TestCase(GameStateId.Paused, GameStateId.MatchResult)]
        [TestCase(GameStateId.MatchResult, GameStateId.Playing)]
        [TestCase(GameStateId.Loading, GameStateId.Paused)]
        [TestCase(GameStateId.OnlineLobby, GameStateId.Playing)]
        public void IllegalTransitions_AreRejected(GameStateId from, GameStateId to)
        {
            Assert.That(GameStateTransitions.IsAllowed(from, to), Is.False);
        }

        [Test]
        public void EveryState_HasAtLeastOneExit()
        {
            foreach (GameStateId id in (GameStateId[])Enum.GetValues(typeof(GameStateId)))
            {
                Assert.That(GameStateTransitions.GetTargets(id), Is.Not.Empty, $"{id} is a dead end");
            }
        }

        [Test]
        public void EveryState_IsReachableFromBoot()
        {
            var visited = new HashSet<GameStateId> { GameStateId.Boot };
            var frontier = new Queue<GameStateId>();
            frontier.Enqueue(GameStateId.Boot);
            while (frontier.Count > 0)
            {
                foreach (GameStateId next in GameStateTransitions.GetTargets(frontier.Dequeue()))
                {
                    if (visited.Add(next))
                    {
                        frontier.Enqueue(next);
                    }
                }
            }

            foreach (GameStateId id in (GameStateId[])Enum.GetValues(typeof(GameStateId)))
            {
                Assert.That(visited, Does.Contain(id), $"{id} is unreachable");
            }
        }

        [Test]
        public void StateChanged_IsRaised()
        {
            GameStateMachine machine = CreateStarted();
            var changes = new List<(GameStateId, GameStateId)>();
            machine.StateChanged += (f, t) => changes.Add((f, t));

            machine.RequestTransition(GameStateId.Loading);

            Assert.That(changes, Is.EqualTo(new[] { (GameStateId.Boot, GameStateId.Loading) }));
        }
    }
}
