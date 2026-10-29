using UnityEngine;

namespace Gumusdere.Core
{
    /// <summary>
    /// Ekranın sol altındaki mesaj kutusu. Karakterlerin sözleri ve oyun olayları buraya düşer;
    /// böylece kamera dışında söylenen şeyler de kaçırılmaz.
    /// </summary>
    public interface IMessageLog
    {
        void Add(string speaker, string text, Color color);
    }
}
