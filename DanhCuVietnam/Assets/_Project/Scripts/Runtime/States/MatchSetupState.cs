namespace DanhCu.Runtime.States
{
    /// <summary>
    /// Top / arena / opponent selection. Lives in the MainMenu scene as a UI panel.
    /// UI edits Session.CurrentMatch via GameManager.UpdateMatchSettings, then ConfirmMatch.
    /// </summary>
    internal sealed class MatchSetupState : GameStateBase
    {
        public MatchSetupState(GameManager manager)
            : base(manager)
        {
        }
    }
}
