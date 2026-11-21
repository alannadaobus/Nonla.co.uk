using DanhCu.Core.StateMachines;

namespace DanhCu.Runtime.States
{
    /// <summary>Base for top-level states. States orchestrate; they hold no gameplay rules.</summary>
    internal abstract class GameStateBase : IState
    {
        protected GameStateBase(GameManager manager)
        {
            Manager = manager;
        }

        protected GameManager Manager { get; }

        public virtual void Enter()
        {
        }

        public virtual void Tick(float deltaTime)
        {
        }

        public virtual void Exit()
        {
        }
    }
}
