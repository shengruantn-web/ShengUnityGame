using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using VillageClash.Core;

namespace VillageClash
{
    /// <summary>Opt-in player verification, configured before the normal save is loaded.</summary>
    public sealed class SmokeSession : MonoBehaviour
    {
        public static SmokeSession Current { get; private set; }
        public FixedClock Clock { get; } = new FixedClock();
        public string SavePath { get; private set; }
        public string ShotsDirectory { get; private set; }
        public bool Finished { get; private set; }
        public bool Interactive { get; private set; }
        int failures;
        System.Threading.Timer watchdog;

        public static SmokeSession StartFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            bool interactive = Array.IndexOf(args, "-qa-session") >= 0;
            if (!interactive && Array.IndexOf(args, "-smoke-test") < 0 && Array.IndexOf(args, "-shots") < 0) return null;
            var session = new GameObject("SmokeSession").AddComponent<SmokeSession>();
            Current = session;
            session.Interactive = interactive;
            if (interactive) session.Clock.NowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            int timeoutSeconds = 180;
            // A locked desktop can stall Unity before Update or a coroutine advances.
            // Keep the deadline outside the frame loop and use no Unity API on this thread.
            if (!interactive) session.watchdog = new System.Threading.Timer(_ =>
            {
                Console.Error.WriteLine($"[Smoke] FAIL runner timed out after {timeoutSeconds} seconds");
                Console.Error.Flush();
                // Managed shutdown can itself wait on a stalled Unity thread.
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }, null, 180000, System.Threading.Timeout.Infinite);
            Application.logMessageReceived += session.OnLog;
            try
            {
                int timeout = Array.IndexOf(args, "-smoke-timeout");
                if (timeout >= 0)
                {
                    if (timeout + 1 >= args.Length || !int.TryParse(args[timeout + 1], out timeoutSeconds)
                        || timeoutSeconds < 1 || timeoutSeconds > 600)
                        throw new ArgumentException("-smoke-timeout requires a whole number of seconds from 1 to 600.");
                    session.watchdog?.Change(timeoutSeconds * 1000, System.Threading.Timeout.Infinite);
                }
                // Never accept a save path from the caller or reuse a previous run's village.
                var runDir = Path.Combine(Path.GetTempPath(), "VillageClashSmoke", Guid.NewGuid().ToString("N"));
                session.SavePath = Path.Combine(runDir, "village.json");
                Directory.CreateDirectory(runDir);
                File.WriteAllText(session.SavePath, SaveSystem.Serialize(SaveSystem.NewGame(11, session.Clock.NowUnix)));
                Debug.Log("[Smoke] isolated save: " + session.SavePath);

                int shots = Array.IndexOf(args, "-shots");
                if (shots >= 0)
                {
                    if (shots + 1 >= args.Length || args[shots + 1].StartsWith("-"))
                        throw new ArgumentException("-shots requires an output directory.");
                    if (Application.isBatchMode || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                        throw new ArgumentException("Screenshots require an interactive graphics session. Use -smoke-test for headless checks.");
                    session.ShotsDirectory = Path.GetFullPath(args[shots + 1]);
                    Directory.CreateDirectory(session.ShotsDirectory);
                }
            }
            catch (Exception e)
            {
                session.Check(false, "setup: " + e.Message);
                session.Complete();
            }
            return session;
        }

        public void Check(bool ok, string what)
        {
            if (!ok) failures++;
            Debug.Log("[Smoke] " + (ok ? "OK   " : "FAIL ") + what);
        }

        public void Complete()
        {
            if (Finished) return;
            Finished = true;
            Debug.Log("[Smoke] " + (failures == 0 ? "ALL CHECKS PASSED" : failures + " CHECK(S) FAILED"));
            Application.Quit(failures == 0 ? 0 : 1);
        }

        void OnLog(string message, string stackTrace, LogType type)
        {
            if (Finished || (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
            Check(false, "Unity reported " + type + ": " + message);
            Complete();
        }

        void OnDestroy()
        {
            watchdog?.Dispose();
            Application.logMessageReceived -= OnLog;
            if (Current == this) Current = null;
        }
    }
}
