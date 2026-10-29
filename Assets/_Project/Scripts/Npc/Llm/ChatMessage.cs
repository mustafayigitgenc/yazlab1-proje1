namespace Gumusdere.Npc.Llm
{
    /// <summary>LLM'e giden tek bir mesaj (system / user / assistant).</summary>
    public readonly struct ChatMessage
    {
        public const string System = "system";
        public const string User = "user";
        public const string Assistant = "assistant";

        public readonly string Role;
        public readonly string Content;

        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }

        public static ChatMessage FromSystem(string content) => new(System, content);
        public static ChatMessage FromUser(string content) => new(User, content);
        public static ChatMessage FromAssistant(string content) => new(Assistant, content);
    }
}
