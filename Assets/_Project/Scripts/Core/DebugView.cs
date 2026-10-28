using System;

namespace Gumusdere.Core
{
    /// <summary>
    /// F1 ile açılıp kapanan hata ayıklama görünümü.
    /// Açıkken Cemal'in NavMesh yolu ve durum bilgileri ekranda çizilir.
    /// Sunumda "hareket gerçekten yol bulmayla yapılıyor" demek için kullanılır.
    /// </summary>
    public static class DebugView
    {
        public static bool Enabled { get; private set; }

        public static event Action<bool> Changed;

        public static void Toggle()
        {
            Enabled = !Enabled;
            Changed?.Invoke(Enabled);
        }
    }
}
