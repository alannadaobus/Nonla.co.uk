using DanhCu.Core.GameFlow;
using DanhCu.Core.Match;
using DanhCu.Data;
using UnityEngine.SceneManagement;

namespace DanhCu.Runtime.States
{
    /// <summary>
    /// Applies engine settings, then routes to the first real state. The routing decision
    /// waits for the first Tick, i.e. after the startup scene has finished loading.
    /// </summary>
    internal sealed class BootState : GameStateBase
    {
        private bool _routed;

        public BootState(GameManager manager)
            : base(manager)
        {
        }

        public override void Enter()
        {
            EngineSettingsApplier.ApplyPhysics(Manager.Config.PhysicsTuning);
            EngineSettingsApplier.ApplyPlatform(Manager.Config);
        }

        public override void Tick(float deltaTime)
        {
            if (_routed)
            {
                return;
            }

            _routed = true;
            GameConfig config = Manager.Config;
            string startScene = SceneManager.GetActiveScene().name;

            if (startScene == config.MainMenuScene)
            {
                // Editor: Play pressed with the menu open.
                Manager.StateMachine.RequestTransition(GameStateId.MainMenu);
            }
            else if (startScene == config.GameplayScene || config.IsArenaScene(startScene))
            {
                // Editor: Play pressed in a gameplay/arena scene. Start a solo match in place.
                Manager.Session.SetMatch(config.CreateDefaultMatchSettings(GameModeType.SinglePlayer));
                Manager.StateMachine.RequestTransition(GameStateId.Playing);
            }
            else
            {
                Manager.BeginLoad(new SceneLoadRequest(config.MainMenuScene), GameStateId.MainMenu);
            }
        }
    }
}
