using System;
using System.Collections.Generic;
using DanhCu.Core.StateMachines;
using NUnit.Framework;

namespace DanhCu.Tests.EditMode.Core
{
    public sealed class StateMachineTests
    {
        private enum S
        {
            A,
            B,
            C,
        }

        private sealed class RecordingState : IState
        {
            private readonly string _name;
            private readonly List<string> _log;

            public RecordingState(string name, List<string> log)
            {
                _name = name;
                _log = log;
            }

            public Action OnEnter { get; set; }

            public Action OnTick { get; set; }

            public void Enter()
            {
                _log.Add(_name + ".Enter");
                OnEnter?.Invoke();
            }

            public void Tick(float deltaTime)
            {
                _log.Add(_name + ".Tick");
                OnTick?.Invoke();
            }

            public void Exit() => _log.Add(_name + ".Exit");
        }

        private List<string> _log;
        private StateMachine<S> _fsm;
        private RecordingState _a;
        private RecordingState _b;
        private RecordingState _c;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _fsm = new StateMachine<S>((from, to) => !(from == S.A && to == S.C));
            _a = new RecordingState("A", _log);
            _b = new RecordingState("B", _log);
            _c = new RecordingState("C", _log);
            _fsm.Register(S.A, _a);
            _fsm.Register(S.B, _b);
            _fsm.Register(S.C, _c);
        }

        [Test]
        public void Start_EntersInitialState()
        {
            _fsm.Start(S.A);

            Assert.That(_fsm.CurrentId, Is.EqualTo(S.A));
            Assert.That(_log, Is.EqualTo(new[] { "A.Enter" }));
        }

        [Test]
        public void CurrentId_BeforeStart_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _ = _fsm.CurrentId);
        }

        [Test]
        public void Start_Twice_Throws()
        {
            _fsm.Start(S.A);
            Assert.Throws<InvalidOperationException>(() => _fsm.Start(S.B));
        }

        [Test]
        public void Register_Duplicate_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _fsm.Register(S.A, _a));
        }

        [Test]
        public void Transition_CallsExitThenEnter_AndRaisesEvent()
        {
            _fsm.Start(S.A);
            S? from = null;
            S? to = null;
            _fsm.StateChanged += (f, t) =>
            {
                from = f;
                to = t;
            };

            bool applied = _fsm.RequestTransition(S.B);

            Assert.That(applied, Is.True);
            Assert.That(_fsm.CurrentId, Is.EqualTo(S.B));
            Assert.That(_log, Is.EqualTo(new[] { "A.Enter", "A.Exit", "B.Enter" }));
            Assert.That(from, Is.EqualTo(S.A));
            Assert.That(to, Is.EqualTo(S.B));
        }

        [Test]
        public void Transition_RejectedByGuard_StaysAndRaisesRejected()
        {
            _fsm.Start(S.A);
            bool rejected = false;
            _fsm.TransitionRejected += (f, t) => rejected = true;

            bool applied = _fsm.RequestTransition(S.C);

            Assert.That(applied, Is.False);
            Assert.That(rejected, Is.True);
            Assert.That(_fsm.CurrentId, Is.EqualTo(S.A));
        }

        [Test]
        public void SelfTransition_IsRejected()
        {
            _fsm.Start(S.A);
            Assert.That(_fsm.RequestTransition(S.A), Is.False);
            Assert.That(_log, Is.EqualTo(new[] { "A.Enter" }));
        }

        [Test]
        public void TransitionRequestedDuringEnter_IsAppliedAfterEnterCompletes()
        {
            _b.OnEnter = () => _fsm.RequestTransition(S.C);
            _fsm.Start(S.A);

            _fsm.RequestTransition(S.B);

            Assert.That(_fsm.CurrentId, Is.EqualTo(S.C));
            Assert.That(_log, Is.EqualTo(new[] { "A.Enter", "A.Exit", "B.Enter", "B.Exit", "C.Enter" }));
        }

        [Test]
        public void TransitionRequestedDuringTick_IsAppliedAfterTickReturns()
        {
            _a.OnTick = () =>
            {
                _fsm.RequestTransition(S.B);
                _log.Add("A.TickEnd");
            };
            _fsm.Start(S.A);

            _fsm.Tick(0.016f);

            Assert.That(_fsm.CurrentId, Is.EqualTo(S.B));
            Assert.That(_log, Is.EqualTo(new[] { "A.Enter", "A.Tick", "A.TickEnd", "A.Exit", "B.Enter" }));
        }

        [Test]
        public void TransitionRequestedDuringStartEnter_IsApplied()
        {
            _a.OnEnter = () => _fsm.RequestTransition(S.B);

            _fsm.Start(S.A);

            Assert.That(_fsm.CurrentId, Is.EqualTo(S.B));
        }

        [Test]
        public void QueuedTransition_IsValidatedAgainstStateAtApplyTime()
        {
            // While entering B, request C then A. B->C is legal; C->A is legal; both apply in order.
            _b.OnEnter = () =>
            {
                _fsm.RequestTransition(S.C);
                _fsm.RequestTransition(S.A);
            };
            _fsm.Start(S.A);

            _fsm.RequestTransition(S.B);

            Assert.That(_fsm.CurrentId, Is.EqualTo(S.A));
            Assert.That(_fsm.PendingTransitionCount, Is.EqualTo(0));
        }

        [Test]
        public void InfiniteTransitionLoop_IsDetected()
        {
            _a.OnEnter = () => _fsm.RequestTransition(S.B);
            _b.OnEnter = () => _fsm.RequestTransition(S.A);

            Assert.Throws<InvalidOperationException>(() => _fsm.Start(S.A));
        }

        [Test]
        public void Tick_OnlyTicksCurrentState()
        {
            _fsm.Start(S.A);
            _fsm.RequestTransition(S.B);
            _log.Clear();

            _fsm.Tick(0.016f);

            Assert.That(_log, Is.EqualTo(new[] { "B.Tick" }));
        }

        [Test]
        public void RequestTransition_ToUnregisteredState_Throws()
        {
            var fsm = new StateMachine<S>();
            fsm.Register(S.A, _a);
            fsm.Start(S.A);

            Assert.Throws<InvalidOperationException>(() => fsm.RequestTransition(S.B));
        }
    }
}
