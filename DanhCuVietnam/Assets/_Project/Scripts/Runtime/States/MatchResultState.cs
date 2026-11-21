namespace DanhCu.Runtime.States
{
    /// <summary>Results screen. UI calls GameManager.Rematch or ReturnToMainMenu.</summary>
    internal sealed class MatchResultState : GameStateBase
    {
        public MatchResultState(GameManager manager)
            : base(manager)
        {
        }

        public override void Enter()
        {
            Manager.Input?.DisableGameplay();
        }
    }
}
