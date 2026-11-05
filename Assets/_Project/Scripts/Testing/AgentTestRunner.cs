using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gumusdere.Companion;
using Gumusdere.Navigation;
using Gumusdere.World;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace Gumusdere.Testing
{
    /// <summary>
    /// Otonom ajan testi (F9). Cemal'i güney kıyısında rastgele seçilen başlangıç noktalarına ışınlar
    /// ve sırayla üç farklı hedefe gönderir:
    ///   - Kuyu başı       : aynı kıyı, kısa yol
    ///   - Vinç            : nehrin karşısı; köprü kalkıkken tek yol sığ geçit (oyuncunun geçemediği engel)
    ///   - Göl iskelesi    : en uzak hedef, geçit + orman kenarı
    /// Görev, oyundaki gerçek akışla verilir (TryAssignTask → Hedefe git → Kullan); yani ölçülen şey
    /// Cemal'in gerçek durum makinesi, NavMesh yol bulması ve ışın sensörüdür.
    /// Her deneme için başarı, süre, yeniden yol arama sayısı ve geçitten geçip geçmediği kaydedilir.
    /// Sonuç: TestResults/agent_test_*.csv ve agent_test_*.md
    /// </summary>
    public sealed class AgentTestRunner : MonoBehaviour
    {
        public const int TrialCount = 30;
        private const int Seed = 2026;            // aynı tohum → aynı başlangıç noktaları (tekrarlanabilir test)
        private const float TrialTimeout = 90f;   // oyun saniyesi
        private const float SpeedUp = 3f;         // test 3 kat hızlı oynatılır; süreler oyun saniyesidir
        private const float MinStartDistance = 8f;
        private static readonly Rect StartArea = new(-36f, -24f, 72f, 40f); // x -36..36, z -24..16 (nehrin güneyi)

        private readonly List<AgentTrialResult> results = new();
        private readonly List<TestTarget> targets = new();

        private CompanionController companion;
        private Transform player;
        private Drawbridge bridge;
        private Action<bool> setControls;
        private Func<bool> canStart;

        private bool cancelRequested;
        private bool trialActive;
        private AgentTrialResult current;
        private float trialStartedAt;
        private string status;
        private float statusUntil;

        public bool IsRunning { get; private set; }

        public void Initialize(CompanionController cemal, Transform playerTransform, Drawbridge drawbridge,
            Action<bool> controlsSetter, Func<bool> startAllowed)
        {
            companion = cemal;
            player = playerTransform;
            bridge = drawbridge;
            setControls = controlsSetter;
            canStart = startAllowed;

            Transform root = new GameObject("AjanTestHedefleri").transform;
            root.SetParent(transform, false);
            targets.Add(TestTarget.Create(root, "Kuyu başı", new Vector3(-4f, 0f, -12.5f), "aynı kıyı, kısa yol"));
            targets.Add(TestTarget.Create(root, "Vinç", new Vector3(6f, 0f, 28.2f), "nehrin karşısı (sığ geçit ya da köprü)"));
            targets.Add(TestTarget.Create(root, "Göl iskelesi", new Vector3(-9.6f, 0f, 48f), "en uzak hedef"));
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null || !kb.f9Key.wasPressedThisFrame) return;

            if (IsRunning)
            {
                // Yanlışlıkla ikinci kez basınca test bozulmasın: iptal sadece Shift+F9 ile.
                if (kb.shiftKey.isPressed)
                {
                    cancelRequested = true;
                    status = "İptal ediliyor...";
                }
                return;
            }

            if (canStart != null && !canStart())
            {
                ShowStatus("Ajan testi şu an başlatılamaz (oyun oynanıyor olmalı, diyalog/balık kapalı olmalı).", 4f);
                return;
            }
            if (companion.IsBusy)
            {
                ShowStatus("Cemal şu an bir görevde. G ile geri çağırıp tekrar F9'a bas.", 4f);
                return;
            }

            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            IsRunning = true;
            cancelRequested = false;
            results.Clear();
            setControls?.Invoke(false);
            float previousScale = Time.timeScale;
            Time.timeScale = SpeedUp;

            // Test, Cemal'in yol bulmasını ölçer: can yeleği varmış gibi (sığ geçit açık) yapılır, sonda eski hâline döner.
            bool hadLifeVest = companion.HasLifeVest;
            companion.SetLifeVest(true);

            companion.TaskSucceeded += OnTaskSucceeded;
            companion.TaskFailed += OnTaskFailed;
            var rng = new System.Random(Seed);
            Debug.Log($"[AjanTesti] Başladı: {TrialCount} deneme, tohum {Seed}, köprü {(bridge != null && bridge.IsLowered ? "inik" : "kalkık")}");

            for (int i = 0; i < TrialCount && !cancelRequested; i++)
            {
                TestTarget target = targets[i % targets.Count];
                current = new AgentTrialResult { Index = i + 1, Target = target.DisplayName };

                if (!TryPickStart(rng, target.InteractionPoint, out Vector3 start, out float pathLength))
                {
                    current.Reason = "Uygun başlangıç noktası bulunamadı";
                    results.Add(current);
                    continue;
                }

                current.Start = start;
                current.PathLength = pathLength;
                current.StraightDistance = Flat(target.InteractionPoint - start);

                // Cemal'i hazırla: boşta bekleme durumuna al, başlangıca ışınla, görevi ver.
                companion.Brain.ChangeState(companion.IdleState);
                companion.Agent.Warp(start);
                companion.Agent.ResetPath();
                yield return null;

                trialActive = true;
                if (!companion.TryAssignTask(target))
                {
                    trialActive = false;
                    current.Reason = "Görev verilemedi";
                    results.Add(current);
                    continue;
                }

                float startedAt = Time.time;
                trialStartedAt = startedAt;
                while (trialActive && !cancelRequested && Time.time - startedAt < TrialTimeout)
                {
                    if (!current.CrossedFord && companion.IsInShallowWater()) current.CrossedFord = true;
                    if (companion.GoToTargetState.IsCareful) current.Careful = true;
                    status = $"Deneme {i + 1}/{TrialCount}  •  Hedef: {target.DisplayName}\n" +
                             $"Süre: {Time.time - startedAt:0.0} sn  •  Durum: {companion.CurrentStateName}\n" +
                             $"Başarılı: {results.Count(r => r.Success)}/{results.Count}   (Shift+F9: iptal)";
                    yield return null;
                }

                if (trialActive)
                {
                    // Süre doldu (ya da iptal): görevi geri çağır; olay işleyicisi sonucu "başarısız" yazar.
                    string reason = cancelRequested ? "İptal" : $"Zaman aşımı ({TrialTimeout:0} sn)";
                    float elapsed = Time.time - startedAt;
                    companion.Recall();
                    if (trialActive) Finish(false, elapsed, reason);
                    current.Seconds = elapsed;
                    current.Reason = reason;
                }

                results.Add(current);
                Debug.Log($"[AjanTesti] #{current.Index} {current.Target}: {(current.Success ? "BAŞARILI" : "BAŞARISIZ")} " +
                          $"{current.Seconds:0.0} sn, yol {current.PathLength:0} m, yeniden yol {current.Repaths}, geçit {current.CrossedFord} {current.Reason}");
            }

            companion.SetLifeVest(hadLifeVest);
            companion.TaskSucceeded -= OnTaskSucceeded;
            companion.TaskFailed -= OnTaskFailed;
            Time.timeScale = previousScale <= 0f ? 1f : previousScale;

            // Cemal'i oyuncunun yanına geri getir.
            companion.Brain.ChangeState(companion.IdleState);
            if (NavMesh.SamplePosition(player.position + player.right * 1.5f, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                companion.Agent.Warp(hit.position);
            }
            companion.Brain.ChangeState(companion.FollowState);
            setControls?.Invoke(true);
            IsRunning = false;

            string summary = WriteReports();
            ShowStatus(summary, 20f);
        }

        // ------------------------------------------------------------------ Olaylar

        private void OnTaskSucceeded(string taskName, float seconds)
        {
            if (trialActive) Finish(true, seconds, null);
        }

        private void OnTaskFailed(string taskName, string reason)
        {
            if (trialActive) Finish(false, Time.time - trialStartedAt, reason);
        }

        private void Finish(bool success, float seconds, string reason)
        {
            trialActive = false;
            current.Success = success;
            current.Seconds = seconds;
            current.Repaths = companion.GoToTargetState.RepathCount;
            current.Reason = reason;
        }

        // ------------------------------------------------------------------ Başlangıç noktası seçimi

        /// <summary>
        /// Güney kıyısında NavMesh üzerinde, yürünebilir ("Walkable") ve hedefe gerçekten bağlı bir nokta seçer.
        /// Ev çatısı gibi kopuk NavMesh adacıkları bu kontrolle elenir.
        /// </summary>
        private static bool TryPickStart(System.Random rng, Vector3 targetPoint, out Vector3 start, out float pathLength)
        {
            start = default;
            pathLength = 0f;
            int walkable = NavAreas.MaskOf(NavAreas.Walkable);
            var path = new NavMeshPath();

            if (!NavMesh.SamplePosition(targetPoint, out NavMeshHit targetHit, 2f, NavMesh.AllAreas)) return false;

            for (int attempt = 0; attempt < 200; attempt++)
            {
                float x = StartArea.xMin + (float)rng.NextDouble() * StartArea.width;
                float z = StartArea.yMin + (float)rng.NextDouble() * StartArea.height;
                if (!NavMesh.SamplePosition(new Vector3(x, 0f, z), out NavMeshHit hit, 1.5f, walkable)) continue;
                if (Flat(targetHit.position - hit.position) < MinStartDistance) continue;
                if (!NavMesh.CalculatePath(hit.position, targetHit.position, NavMesh.AllAreas, path)) continue;
                if (path.status != NavMeshPathStatus.PathComplete) continue;

                start = hit.position;
                pathLength = Length(path);
                return true;
            }
            return false;
        }

        private static float Length(NavMeshPath path)
        {
            float total = 0f;
            Vector3[] corners = path.corners;
            for (int i = 1; i < corners.Length; i++) total += Vector3.Distance(corners[i - 1], corners[i]);
            return total;
        }

        private static float Flat(Vector3 v)
        {
            v.y = 0f;
            return v.magnitude;
        }

        // ------------------------------------------------------------------ Rapor

        private string WriteReports()
        {
            string stamp = TestReport.Stamp();
            string bridgeState = bridge != null && bridge.IsLowered ? "inik" : "kalkık";

            string[] header =
            {
                "deneme", "hedef", "baslangic_x", "baslangic_z", "kus_ucusu_m", "navmesh_yol_m",
                "basarili", "sure_sn", "yeniden_yol", "gecitten_gecti", "yavasladi", "sebep"
            };
            var rows = results.Select(r => (IReadOnlyList<string>)new[]
            {
                r.Index.ToString(), r.Target, TestReport.F(r.Start.x), TestReport.F(r.Start.z),
                TestReport.F(r.StraightDistance), TestReport.F(r.PathLength),
                r.Success ? "evet" : "hayir", TestReport.F(r.Seconds, 2), r.Repaths.ToString(),
                r.CrossedFord ? "evet" : "hayir", r.Careful ? "evet" : "hayir", r.Reason ?? ""
            });
            string csv = TestReport.WriteCsv($"agent_test_{stamp}.csv", header, rows);

            var md = new StringBuilder();
            md.AppendLine($"### Ajan testi ({DateTime.Now:dd.MM.yyyy HH:mm})");
            md.AppendLine();
            md.AppendLine($"{results.Count} deneme, rastgele tohum {Seed}, köprü {bridgeState}, zaman aşımı {TrialTimeout:0} sn. " +
                          "Süreler oyun saniyesidir (görev verildiği andan hedefi kullandığı ana kadar).");
            md.AppendLine();
            md.AppendLine("| Hedef | Deneme | Başarılı | Başarı oranı | Ort. süre (sn) | En kısa | En uzun | Ort. NavMesh yolu (m) | Sığ geçitten geçen | Ort. yeniden yol |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
            foreach (IGrouping<string, AgentTrialResult> group in results.GroupBy(r => r.Target))
            {
                md.AppendLine(SummaryRow(group.Key, group.ToList()));
            }
            md.AppendLine(SummaryRow("**Toplam**", results));

            List<AgentTrialResult> failures = results.Where(r => !r.Success).ToList();
            md.AppendLine();
            if (failures.Count == 0)
            {
                md.AppendLine("Başarısız deneme yok.");
            }
            else
            {
                md.AppendLine("Başarısız denemeler:");
                foreach (AgentTrialResult f in failures)
                    md.AppendLine($"- #{f.Index} {f.Target} ({TestReport.F(f.Start.x)}, {TestReport.F(f.Start.z)}): {TestReport.Md(f.Reason)}");
            }
            string mdPath = TestReport.WriteText($"agent_test_{stamp}.md", md.ToString());

            int ok = results.Count(r => r.Success);
            List<float> times = results.Where(r => r.Success).Select(r => r.Seconds).ToList();
            return $"Bitti: {ok}/{results.Count} başarılı ({TestReport.Percent(ok, results.Count)}), " +
                   $"ort. süre {(times.Count > 0 ? times.Average() : 0f):0.0} sn\n" +
                   $"Dosyalar: {(csv != null ? System.IO.Path.GetFileName(csv) : "yazılamadı")}, " +
                   $"{(mdPath != null ? System.IO.Path.GetFileName(mdPath) : "yazılamadı")}\n" +
                   $"Klasör: {TestReport.ResultsFolder}";
        }

        private static string SummaryRow(string name, List<AgentTrialResult> list)
        {
            List<AgentTrialResult> ok = list.Where(r => r.Success).ToList();
            string avg = ok.Count > 0 ? TestReport.F(ok.Average(r => r.Seconds)) : "-";
            string min = ok.Count > 0 ? TestReport.F(ok.Min(r => r.Seconds)) : "-";
            string max = ok.Count > 0 ? TestReport.F(ok.Max(r => r.Seconds)) : "-";
            string path = list.Count > 0 ? TestReport.F(list.Average(r => r.PathLength)) : "-";
            string repath = list.Count > 0 ? TestReport.F(list.Average(r => (float)r.Repaths), 2) : "-";
            int ford = list.Count(r => r.CrossedFord);
            return $"| {name} | {list.Count} | {ok.Count} | {TestReport.Percent(ok.Count, list.Count)} | {avg} | {min} | {max} | {path} | {ford} | {repath} |";
        }

        // ------------------------------------------------------------------ Ekran

        private void ShowStatus(string text, float seconds)
        {
            status = text;
            statusUntil = Time.unscaledTime + seconds;
        }

        private void OnGUI()
        {
            if (!IsRunning && Time.unscaledTime > statusUntil) return;
            TestPanel.Draw("Ajan testi (F9)", status);
        }
    }
}
