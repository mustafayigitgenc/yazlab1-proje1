namespace Gumusdere.Core
{
    /// <summary>
    /// Bir karakterin kısa durum mesajı göstermesi (konuşma balonu).
    /// Cemal'in mantığı balonun nasıl çizildiğini bilmez; sadece bu arayüzü kullanır.
    /// </summary>
    public interface ISpeaker
    {
        void Say(string text, float seconds = 2.5f);
    }
}
