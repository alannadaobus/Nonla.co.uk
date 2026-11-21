using DanhCu.Core.Diagnostics;
using DanhCu.Core.GameFlow;

namespace DanhCu.Runtime.States
{
    /// <summary>
    /// Loads the scenes requested via GameManager.BeginLoad, then enters the target state.
    /// If loading fails it falls back to the main menu; if the main menu itself fails,
    /// it stays here and logs an error (the build is broken).
    /// </summary>
    internal sealed class LoadingState : GameStateBase
    {
        private GameStateId _target;
        private bool _isFallback;

        public LoadingState(GameManager manager)
            : base(manager)
        {
        }

        public override void Enter()
        {
            Manager.Input?.DisableGameplay();
            _isFallback = false;
            _target = Manager.PendingLoadTarget;
            Manager.SceneLoader.Load(Manager.PendingLoad, OnLoaded);
        }

        private void OnLoaded(bool success)
        {
            if (success)
            {
                Manager.StateMachine.RequestTransition(_target);
                return;
            }

            if (_isFallback || _target == GameStateId.MainMenu)
            {
                Log.Error("Main menu scene failed to load. Check Build Settings.");
                return;
            }

            _isFallback = true;
            _target = GameStateId.MainMenu;
            Manager.SceneLoader.Load(new SceneLoadRequest(Manager.Config.MainMenuScene), OnLoaded);
        }
    }
}
