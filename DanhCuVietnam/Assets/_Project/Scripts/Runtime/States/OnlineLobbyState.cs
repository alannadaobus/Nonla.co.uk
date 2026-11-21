using DanhCu.Core.Diagnostics;

namespace DanhCu.Runtime.States
{
    /// <summary>
    /// Matchmaking / lobby for online play. Placeholder until the networking layer
    /// (Phase 5, TECHNICAL_DESIGN.md §8) is implemented.
    /// </summary>
    internal sealed class OnlineLobbyState : GameStateBase
    {
        public OnlineLobbyState(GameManager manager)
            : base(manager)
        {
        }

        public override void Enter()
        {
            Log.Warning("Online multiplayer is not implemented yet (Phase 5). Use GameManager.ReturnToMainMenu().");
        }
    }
}
