namespace Hype.GameServer.InGame.Fsm.Detail
{
    public interface IFsmBehaviour
    {
        void Begin();
        void Update(long delta);
        void End();
    }
}
