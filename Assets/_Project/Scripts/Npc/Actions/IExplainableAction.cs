namespace Gumusdere.Npc.Actions
{
    /// <summary>
    /// Kural reddettiğinde NPC'nin oyuncuya söyleyeceği tutarlı açıklamayı üretebilen eylem.
    /// Model "kapıyı açtım" deyip kural engellerse, model düzeltemezse bu cümle kullanılır.
    /// </summary>
    public interface IExplainableAction : IGameAction
    {
        string ExplainRefusal(int trust);
    }
}
