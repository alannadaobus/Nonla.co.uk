namespace DanhCu.Runtime.States
{
    /// <summary>Main menu. UI (Phase 2) calls GameManager.OpenMatchSetup / QuitGame.</summary>
    internal sealed class MainMenuState : GameStateBase
    {
        public MainMenuState(GameManager manager)
            : base(manager)
        {
        }

        public override void Enter()
        {
            Manager.Input?.DisableGameplay();
        }
    }
}
