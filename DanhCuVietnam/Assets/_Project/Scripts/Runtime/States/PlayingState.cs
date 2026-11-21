namespace DanhCu.Runtime.States
{
    /// <summary>
    /// A match is running. The match controller (Phase 2) is spawned by the Gameplay
    /// scene and reports the outcome through GameManager.EndMatch.
    /// MatchStartedEvent is published by GameManager when entering from anything but Paused.
    /// </summary>
    internal sealed class PlayingState : GameStateBase
    {
        public PlayingState(GameManager manager)
            : base(manager)
        {
        }

        public override void Enter()
        {
            Manager.Input?.EnableGameplay();
        }
    }
}
