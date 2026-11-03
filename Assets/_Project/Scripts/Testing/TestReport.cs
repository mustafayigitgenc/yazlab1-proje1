using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Gumusdere.Testing
{
    /// <summary>
    /// Test sonuçlarını proje kökündeki TestResults/ klasörüne yazar:
    ///   *.csv  → ham veri (Excel ile açılır)
    ///   *.md   → README'ye doğrudan yapıştırılabilecek özet tablo
    /// Editörde klasör proje kökündedir; Windows build'de oyunun exe'sinin yanındadır.
    /// </summary>
    public static class TestReport
    {
        public static string ResultsFolder =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "TestResults"));

        public static string Stamp() => DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

        /// <summary>CSV yazar (ayraç ';' — Türkçe Excel virgülü ondalık sayar). Tam dosya yolunu döner; hata olursa null.</summary>
        public static string WriteCsv(string fileName, IReadOnlyList<string> header, IEnumerable<IReadOnlyList<string>> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(";", Escape(header)));
            foreach (IReadOnlyList<string> row in rows) sb.AppendLine(string.Join(";", Escape(row)));
            return WriteText(fileName, sb.ToString(), true);
        }

        public static string WriteText(string fileName, string content, bool withBom = false)
        {
            try
            {
                Directory.CreateDirectory(ResultsFolder);
                string path = Path.Combine(ResultsFolder, fileName);
                File.WriteAllText(path, content, new UTF8Encoding(withBom)); // BOM: Excel Türkçe harfleri doğru göstersin
                Debug.Log($"[Test] Yazıldı: {path}");
                return path;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Test] Sonuç dosyası yazılamadı ({fileName}): {e.Message}");
                return null;
            }
        }

        public static string F(float value, int decimals = 1) =>
            value.ToString("F" + decimals, CultureInfo.InvariantCulture);

        public static string Percent(int part, int total) =>
            total == 0 ? "-" : (100f * part / total).ToString("F0", CultureInfo.InvariantCulture) + "%";

        private static IEnumerable<string> Escape(IEnumerable<string> cells)
        {
            foreach (string cell in cells)
            {
                string c = cell ?? "";
                bool quote = c.IndexOfAny(new[] { ';', '"', '\n', '\r' }) >= 0;
                yield return quote ? "\"" + c.Replace("\"", "\"\"") + "\"" : c;
            }
        }

        /// <summary>Markdown tablosu içine güvenle konacak metin.</summary>
        public static string Md(string text) =>
            (text ?? "").Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    }
}
