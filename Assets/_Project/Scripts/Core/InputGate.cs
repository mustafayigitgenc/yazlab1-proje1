using UnityEngine;

namespace Gumusdere.Core
{
    /// <summary>
    /// Aynı tuşun aynı karede birden fazla sistem tarafından kullanılmasını önler.
    /// Örnek: balık tutarken Esc'ye basılınca olta toplanır, aynı karede oyun duraklatılmaz.
    /// </summary>
    public static class InputGate
    {
        private static int escapeConsumedFrame = -1;

        public static void ConsumeEscape() => escapeConsumedFrame = Time.frameCount;

        public static bool IsEscapeConsumed => escapeConsumedFrame == Time.frameCount;
    }
}
