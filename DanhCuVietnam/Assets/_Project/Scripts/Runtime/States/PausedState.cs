using DanhCu.Core.Events;
using DanhCu.Core.Match;
using UnityEngine;

namespace DanhCu.Runtime.States
{
    /// <summary>
    /// Pause menu. Freezes time only offline: online, the other players keep simulating,
    /// so the menu is an overlay and local input is simply ignored by gameplay.
    /// </summary>
    internal sealed class PausedState : GameStateBase
    {
        private float _previousTimeScale = 1f;
        private bool _froze;

        public PausedState(GameManager manager)
            : base(manager)
        {
        }

        public override void Enter()
        {
            _froze = GameModeRules.CanFreezeSimulationOnPause(Manager.Session.Mode);
            if (_froze)
            {
                _previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                AudioListener.pause = true;
            }

            Manager.Events.Publish(new PauseChangedEvent(true, _froze));
        }

        public override void Exit()
        {
            if (_froze)
            {
                Time.timeScale = _previousTimeScale;
                AudioListener.pause = false;
            }

            Manager.Events.Publish(new PauseChangedEvent(false, _froze));
        }
    }
}
