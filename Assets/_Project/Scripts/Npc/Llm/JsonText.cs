using System.Globalization;
using System.Text;

namespace Gumusdere.Npc.Llm
{
    /// <summary>
    /// İstek JSON'unu elle kurarken metinleri güvenle kaçışlamak için yardımcı.
    /// Sayılar her zaman InvariantCulture ile yazılır (Türkçe Windows'ta 0,7 yerine 0.7 olsun diye).
    /// </summary>
    public static class JsonText
    {
        public static string Quote(string value)
        {
            if (value == null) return "null";

            var sb = new StringBuilder(value.Length + 8);
            sb.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        public static string Number(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        public static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
