using Assets.Scripts.Player;

namespace Assets.Scripts.StateMachine
{
    interface IState
    {
        void Enter(PlayerController p);
        IState Execute(PlayerController p);
        void Exit(PlayerController p);
    }
}