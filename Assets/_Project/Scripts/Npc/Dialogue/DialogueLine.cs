namespace Gumusdere.Npc.Dialogue
{
    public enum DialogueSpeaker
    {
        Player,
        Npc,
        System
    }

    /// <summary>Diyalog penceresindeki tek bir satır.</summary>
    public sealed class DialogueLine
    {
        public DialogueLine(DialogueSpeaker speaker, string text)
        {
            Speaker = speaker;
            Text = text;
        }

        public DialogueSpeaker Speaker { get; }
        public string Text { get; }
    }
}
