namespace Gumusdere.Npc.Parsing
{
    /// <summary>Model çıktısını oyunun anlayacağı bir yapıya çeviren ayrıştırıcı.</summary>
    public interface IResponseParser<T>
    {
        /// <summary>Başarılıysa true ve sonucu döner; değilse false ve hatanın açıklamasını döner. Asla istisna fırlatmaz.</summary>
        bool TryParse(string raw, out T result, out string error);
    }
}
