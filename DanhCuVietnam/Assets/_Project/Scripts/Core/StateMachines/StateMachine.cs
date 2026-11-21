using System;
using System.Collections.Generic;

namespace DanhCu.Core.StateMachines
{
    /// <summary>
    /// Generic, re-entrancy safe finite state machine keyed by an enum.
    ///
    /// Rules:
    /// - Transitions are validated by an optional guard (e.g. a transition table).
    /// - A transition requested while the machine is busy (inside Enter, Exit or Tick)
    ///   is queued and applied as soon as the current call returns, so a state is never
    ///   exited while its own code is still on the stack.
    /// - Queued transitions are validated when they are applied, against the state that
    ///   is current at that moment.
    /// </summary>
    public sealed class StateMachine<TId> where TId : struct, Enum
    {
        private const int MaxTransitionsPerFlush = 32;

        private readonly Dictionary<TId, IState> _states = new Dictionary<TId, IState>();
        private readonly Queue<TId> _pending = new Queue<TId>();
        private readonly Func<TId, TId, bool> _guard;
        private readonly EqualityComparer<TId> _comparer = EqualityComparer<TId>.Default;

        private IState _currentState;
        private TId _currentId;
        private bool _hasStarted;
        private bool _isBusy;

        /// <summary>Raised after a transition completes: (from, to).</summary>
        public event Action<TId, TId> StateChanged;

        /// <summary>Raised when a transition is refused by the guard: (from, to).</summary>
        public event Action<TId, TId> TransitionRejected;

        /// <param name="guard">Returns true if moving from the first id to the second is allowed. Null allows everything except self-transitions.</param>
        public StateMachine(Func<TId, TId, bool> guard = null)
        {
            _guard = guard;
        }

        public bool HasStarted => _hasStarted;

        public bool IsBusy => _isBusy;

        public int PendingTransitionCount => _pending.Count;

        public TId CurrentId
        {
            get
            {
                if (!_hasStarted)
                {
                    throw new InvalidOperationException("State machine has not been started.");
                }

                return _currentId;
            }
        }

        public void Register(TId id, IState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (_states.ContainsKey(id))
            {
                throw new InvalidOperationException($"State '{id}' is already registered.");
            }

            _states.Add(id, state);
        }

        public bool IsRegistered(TId id) => _states.ContainsKey(id);

        public bool TryGetState(TId id, out IState state) => _states.TryGetValue(id, out state);

        /// <summary>Enters the initial state. Must be called exactly once.</summary>
        public void Start(TId initial)
        {
            if (_hasStarted)
            {
                throw new InvalidOperationException("State machine has already been started.");
            }

            IState state = GetRegistered(initial);
            _hasStarted = true;
            _currentId = initial;
            _currentState = state;

            RunBusy(state.Enter);
            FlushPending();
        }

        /// <summary>True if a transition from the current state to <paramref name="to"/> would be accepted right now.</summary>
        public bool CanTransitionTo(TId to)
        {
            return _hasStarted && IsAllowed(_currentId, to);
        }

        /// <summary>
        /// Requests a transition. Returns false if it was rejected immediately.
        /// Returns true if it was applied, or queued because the machine is busy.
        /// </summary>
        public bool RequestTransition(TId to)
        {
            if (!_hasStarted)
            {
                throw new InvalidOperationException("Cannot transition before Start().");
            }

            if (!_states.ContainsKey(to))
            {
                throw new InvalidOperationException($"State '{to}' is not registered.");
            }

            if (_isBusy)
            {
                _pending.Enqueue(to);
                return true;
            }

            bool applied = TryApply(to);
            FlushPending();
            return applied;
        }

        public void Tick(float deltaTime)
        {
            if (!_hasStarted || _isBusy)
            {
                return;
            }

            IState state = _currentState;
            RunBusy(() => state.Tick(deltaTime));
            FlushPending();
        }

        private bool TryApply(TId to)
        {
            TId from = _currentId;
            if (!IsAllowed(from, to))
            {
                TransitionRejected?.Invoke(from, to);
                return false;
            }

            IState next = _states[to];
            IState previous = _currentState;

            RunBusy(previous.Exit);
            _currentId = to;
            _currentState = next;
            RunBusy(next.Enter);

            StateChanged?.Invoke(from, to);
            return true;
        }

        private void FlushPending()
        {
            int guardCount = 0;
            while (_pending.Count > 0 && !_isBusy)
            {
                if (++guardCount > MaxTransitionsPerFlush)
                {
                    _pending.Clear();
                    throw new InvalidOperationException(
                        $"More than {MaxTransitionsPerFlush} chained transitions in one flush; likely a transition loop.");
                }

                TryApply(_pending.Dequeue());
            }
        }

        private bool IsAllowed(TId from, TId to)
        {
            if (_comparer.Equals(from, to))
            {
                return false;
            }

            return _guard == null || _guard(from, to);
        }

        private IState GetRegistered(TId id)
        {
            if (!_states.TryGetValue(id, out IState state))
            {
                throw new InvalidOperationException($"State '{id}' is not registered.");
            }

            return state;
        }

        private void RunBusy(Action action)
        {
            bool wasBusy = _isBusy;
            _isBusy = true;
            try
            {
                action();
            }
            finally
            {
                _isBusy = wasBusy;
            }
        }
    }
}
