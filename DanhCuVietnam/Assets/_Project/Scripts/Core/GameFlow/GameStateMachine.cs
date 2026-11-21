using System;
using DanhCu.Core.StateMachines;

namespace DanhCu.Core.GameFlow
{
    /// <summary>
    /// Top-level game flow: a <see cref="StateMachine{TId}"/> guarded by <see cref="GameStateTransitions"/>.
    /// Engine-agnostic so it can be unit-tested and reused by a headless server.
    /// </summary>
    public sealed class GameStateMachine
    {
        private readonly StateMachine<GameStateId> _machine;

        public GameStateMachine()
        {
            _machine = new StateMachine<GameStateId>(GameStateTransitions.IsAllowed);
            _machine.StateChanged += (from, to) => StateChanged?.Invoke(from, to);
            _machine.TransitionRejected += (from, to) => TransitionRejected?.Invoke(from, to);
        }

        public event Action<GameStateId, GameStateId> StateChanged;

        public event Action<GameStateId, GameStateId> TransitionRejected;

        public bool HasStarted => _machine.HasStarted;

        public GameStateId Current => _machine.CurrentId;

        public void Register(GameStateId id, IState state) => _machine.Register(id, state);

        public void Start() => Start(GameStateId.Boot);

        public void Start(GameStateId initial)
        {
            foreach (GameStateId id in (GameStateId[])Enum.GetValues(typeof(GameStateId)))
            {
                if (!_machine.IsRegistered(id))
                {
                    throw new InvalidOperationException($"Game state '{id}' has no registered implementation.");
                }
            }

            _machine.Start(initial);
        }

        public bool CanTransitionTo(GameStateId to) => _machine.CanTransitionTo(to);

        public bool RequestTransition(GameStateId to) => _machine.RequestTransition(to);

        public void Tick(float deltaTime) => _machine.Tick(deltaTime);
    }
}
