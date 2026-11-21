using System.Collections.Generic;
using DanhCu.Core.Diagnostics;
using DanhCu.Core.Events;
using DanhCu.Core.GameFlow;
using DanhCu.Core.Match;
using DanhCu.Core.Services;
using DanhCu.Data;
using DanhCu.Input;
using DanhCu.Runtime.States;
using UnityEngine;

namespace DanhCu.Runtime
{
    /// <summary>
    /// Composition root and owner of the top-level game flow. Lives for the whole
    /// application in DontDestroyOnLoad. The only singleton in the project.
    ///
    /// It wires services together and exposes intent-level commands (StartMatch,
    /// TogglePause, ...) for UI and gameplay; it contains no gameplay rules itself.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        private SceneLoadRequest _pendingLoad;
        private GameStateId _pendingLoadTarget;
        private bool _initialized;

        public static GameManager Instance { get; private set; }

        public GameConfig Config { get; private set; }

        public EventBus Events { get; private set; }

        public ServiceRegistry Services { get; private set; }

        public GameSession Session { get; private set; }

        public GameStateMachine StateMachine { get; private set; }

        public SceneLoader SceneLoader { get; private set; }

        /// <summary>Null when the config has no input asset.</summary>
        public PlayerInputReader Input { get; private set; }

        public GameStateId CurrentState => StateMachine != null && StateMachine.HasStarted ? StateMachine.Current : GameStateId.Boot;

        internal SceneLoadRequest PendingLoad => _pendingLoad;

        internal GameStateId PendingLoadTarget => _pendingLoadTarget;

        /// <summary>Called once by <see cref="GameBootstrapper"/>.</summary>
        internal void Initialize(GameConfig config)
        {
            if (_initialized)
            {
                return;
            }

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _initialized = true;
            Config = config;
            Log.Sink = new UnityLogSink();

            Events = new EventBus();
            Services = new ServiceRegistry();
            Session = new GameSession();
            SceneLoader = new SceneLoader(this, Events);

            if (config.InputActions != null)
            {
                Input = new PlayerInputReader(config.InputActions);
                Input.PausePressed += TogglePause;
            }
            else
            {
                Debug.LogWarning("[DanhCu] GameConfig has no InputActionAsset; player input is disabled.");
            }

            Services.Register(Config);
            Services.Register(Events);
            Services.Register(Session);
            Services.Register(SceneLoader);
            if (Input != null)
            {
                Services.Register(Input);
            }

            StateMachine = new GameStateMachine();
            StateMachine.Register(GameStateId.Boot, new BootState(this));
            StateMachine.Register(GameStateId.MainMenu, new MainMenuState(this));
            StateMachine.Register(GameStateId.MatchSetup, new MatchSetupState(this));
            StateMachine.Register(GameStateId.OnlineLobby, new OnlineLobbyState(this));
            StateMachine.Register(GameStateId.Loading, new LoadingState(this));
            StateMachine.Register(GameStateId.Playing, new PlayingState(this));
            StateMachine.Register(GameStateId.Paused, new PausedState(this));
            StateMachine.Register(GameStateId.MatchResult, new MatchResultState(this));
            StateMachine.StateChanged += OnStateChanged;
            StateMachine.TransitionRejected += OnTransitionRejected;
            StateMachine.Start(GameStateId.Boot);
        }

        // ---------- Commands (called by UI / gameplay) ----------

        /// <summary>From the main menu: begin configuring a match for a mode. Online goes through the lobby.</summary>
        public bool OpenMatchSetup(GameModeType mode)
        {
            GameStateId target = GameModeRules.RequiresNetwork(mode) ? GameStateId.OnlineLobby : GameStateId.MatchSetup;
            if (!StateMachine.CanTransitionTo(target))
            {
                // Never clobber the session of a running match.
                return false;
            }

            Session.SetMatch(Config.CreateDefaultMatchSettings(mode));
            return StateMachine.RequestTransition(target);
        }

        /// <summary>Replaces the settings of the match being configured (in MatchSetup).</summary>
        public void UpdateMatchSettings(MatchSettings settings)
        {
            Session.SetMatch(settings);
        }

        /// <summary>From MatchSetup: validate and load the match.</summary>
        public bool ConfirmMatch()
        {
            if (CurrentState != GameStateId.MatchSetup || !Session.HasMatch)
            {
                return false;
            }

            List<string> errors = Config.ValidateMatch(Session.CurrentMatch);
            if (errors.Count > 0)
            {
                Debug.LogError("[DanhCu] Cannot start match: " + string.Join(" ", errors));
                return false;
            }

            return BeginLoad(BuildMatchLoadRequest(Session.CurrentMatch), GameStateId.Playing);
        }

        /// <summary>Convenience for tests and quick-play: MainMenu -> MatchSetup -> Loading -> Playing.</summary>
        public bool StartMatch(MatchSettings settings)
        {
            if (CurrentState == GameStateId.MainMenu && !OpenMatchSetup(settings.Mode))
            {
                return false;
            }

            Session.SetMatch(settings);
            return ConfirmMatch();
        }

        public void TogglePause()
        {
            if (CurrentState == GameStateId.Playing)
            {
                StateMachine.RequestTransition(GameStateId.Paused);
            }
            else if (CurrentState == GameStateId.Paused)
            {
                StateMachine.RequestTransition(GameStateId.Playing);
            }
        }

        /// <summary>Called by the match rules when a winner is decided.</summary>
        public void EndMatch(MatchResult result)
        {
            if (CurrentState != GameStateId.Playing)
            {
                return;
            }

            Session.SetResult(result);
            if (StateMachine.RequestTransition(GameStateId.MatchResult))
            {
                Events.Publish(new MatchEndedEvent(result));
            }
        }

        public bool Rematch()
        {
            return CurrentState == GameStateId.MatchResult
                && Session.HasMatch
                && BeginLoad(BuildMatchLoadRequest(Session.CurrentMatch), GameStateId.Playing);
        }

        public bool ReturnToMainMenu()
        {
            switch (CurrentState)
            {
                case GameStateId.MatchSetup:
                case GameStateId.OnlineLobby:
                    return StateMachine.RequestTransition(GameStateId.MainMenu);
                case GameStateId.Playing:
                case GameStateId.Paused:
                case GameStateId.MatchResult:
                    return BeginLoad(new SceneLoadRequest(Config.MainMenuScene), GameStateId.MainMenu);
                default:
                    return false;
            }
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------- Internal API for states ----------

        internal bool BeginLoad(SceneLoadRequest request, GameStateId target)
        {
            if (!StateMachine.CanTransitionTo(GameStateId.Loading))
            {
                Log.Warning($"Cannot load '{request}' from state {CurrentState}.");
                return false;
            }

            _pendingLoad = request;
            _pendingLoadTarget = target;
            return StateMachine.RequestTransition(GameStateId.Loading);
        }

        internal SceneLoadRequest BuildMatchLoadRequest(MatchSettings settings)
        {
            ArenaDefinition arena = Config.FindArena(settings.ArenaId);
            return new SceneLoadRequest(Config.GameplayScene, arena != null ? arena.SceneName : null);
        }

        // ---------- Unity lifecycle ----------

        private void Update()
        {
            if (_initialized)
            {
                StateMachine.Tick(Time.unscaledDeltaTime);
            }
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            if (Input != null)
            {
                Input.PausePressed -= TogglePause;
                Input.Dispose();
            }

            Events?.Clear();
            Services?.Clear();
            Instance = null;
        }

        /// <summary>Supports "Enter Play Mode Options" with domain reload disabled.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void OnStateChanged(GameStateId from, GameStateId to)
        {
            Log.Info($"State {from} -> {to}");
            Events.Publish(new GameStateChangedEvent(from, to));

            if (to == GameStateId.Playing && from != GameStateId.Paused && Session.HasMatch)
            {
                Events.Publish(new MatchStartedEvent(Session.CurrentMatch));
            }
        }

        private static void OnTransitionRejected(GameStateId from, GameStateId to)
        {
            Log.Warning($"Rejected state transition {from} -> {to}.");
        }
    }
}
