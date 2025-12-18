// SDET_CodingInterview_Practice.cs
// Drop into a .NET Console App as Program.cs
// Goal: Implement TODOs. Each section has tiny "tests" in Main.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SdetInterviewPractice
{
    internal static class Program
    {
        static async Task Main()
        {
            Console.OutputEncoding = Encoding.UTF8;

            // ====== 0) Sanity ======
            Console.WriteLine("SDET Interview Practice - C#");
            Console.WriteLine("Implement TODOs and re-run until all assertions pass.\n");

            // ====== 1) Log parsing & aggregation ======
            // Scenario: You ingest test runner logs and want useful summaries.
            var logLines = new[]
            {
                "2025-12-17T10:01:00Z INFO  Suite=Checkout Test=AddToCart DurationMs=120 Status=PASS",
                "2025-12-17T10:01:01Z INFO  Suite=Checkout Test=ApplyCoupon DurationMs=450 Status=FAIL Error=Assertion",
                "2025-12-17T10:01:02Z WARN  Suite=Checkout Test=ApplyCoupon DurationMs=470 Status=FAIL Error=Timeout",
                "2025-12-17T10:01:03Z INFO  Suite=Search   Test=BasicSearch DurationMs=80  Status=PASS",
                "2025-12-17T10:01:04Z INFO  Suite=Search   Test=BasicSearch DurationMs=90  Status=PASS",
            };

            var parsed = LogAnalyzer.Parse(logLines).ToList();
            Assert.Equal(5, parsed.Count, "Parse should return 5 entries");

            var summary = LogAnalyzer.SummarizeByTest(parsed);
            Assert.Equal(2, summary["Checkout|ApplyCoupon"].FailCount, "ApplyCoupon fail count");
            Assert.Equal(1, summary["Checkout|AddToCart"].PassCount, "AddToCart pass count");

            // ====== 2) Stable retry policy (flaky tests) ======
            // Scenario: Retry only on "transient" errors.
            var outcomes = new[] { RunOutcome.Fail("Timeout"), RunOutcome.Fail("Timeout"), RunOutcome.Pass() };
            int attempt = 0;

            var result = await RetryPolicy.ExecuteAsync(
                action: () =>
                {
                    // simulates a flaky run
                    var o = outcomes[attempt++];
                    return Task.FromResult(o);
                },
                isTransient: e => e.Reason is "Timeout" or "Network",
                maxAttempts: 3,
                backoffMs: attemptNo => 10 * attemptNo);

            Assert.True(result.IsPass, "Should pass after retries");

            // ====== 3) Dedupe failures (triage) ======
            // Scenario: group failures by "signature" to reduce noise.
            var fails = new[]
            {
                new Failure("Checkout", "ApplyCoupon", "Timeout", "stackA"),
                new Failure("Checkout", "ApplyCoupon", "Timeout", "stackA"),
                new Failure("Checkout", "ApplyCoupon", "Assertion", "stackB"),
                new Failure("Search", "BasicSearch", "Timeout", "stackC"),
            };

            var groups = FailureTriage.GroupBySignature(fails);
            Assert.Equal(3, groups.Count, "Should produce 3 unique signatures");
            Assert.Equal(2, groups.Max(g => g.Count), "Largest group should have 2 items");

            // ====== 4) JSON-ish config validation ======
            // Scenario: validate pipeline/test config quickly and clearly.
            // We'll use a simple dictionary instead of real JSON for the exercise.
            var cfg = new Dictionary<string, string?>
            {
                ["baseUrl"] = "https://example.test",
                ["timeoutMs"] = "5000",
                ["retries"] = "2",
                ["env"] = "staging",
            };

            var errors = ConfigValidator.Validate(cfg);
            Assert.Equal(0, errors.Count, "Valid config should have 0 errors");

            cfg["timeoutMs"] = "-1"; // invalid
            errors = ConfigValidator.Validate(cfg);
            Assert.True(errors.Count > 0, "Invalid timeout should produce errors");

            // ====== 5) Diff two result sets (regression detection) ======
            // Scenario: compare yesterday vs today to detect new failures and fixed tests.
            var yesterday = new[]
            {
                new TestResult("Checkout", "ApplyCoupon", "FAIL"),
                new TestResult("Checkout", "AddToCart", "PASS"),
                new TestResult("Search", "BasicSearch", "PASS"),
            };

            var today = new[]
            {
                new TestResult("Checkout", "ApplyCoupon", "PASS"),
                new TestResult("Checkout", "AddToCart", "FAIL"),
                new TestResult("Search", "BasicSearch", "PASS"),
                new TestResult("Search", "AdvancedSearch", "FAIL"),
            };

            var diff = ResultDiff.Compute(yesterday, today);
            Assert.Equal(2, diff.NewFailures.Count, "NewFailures should include AddToCart + AdvancedSearch");
            Assert.Equal(1, diff.Fixed.Count, "Fixed should include ApplyCoupon");

            // ====== 6) Async timeout wrapper (common in E2E) ======
            // Scenario: enforce timeouts around async work without hanging.
            var fast = await AsyncUtil.WithTimeout(Task.Delay(20), timeoutMs: 200);
            Assert.True(fast.Completed, "Fast task should complete");

            bool timedOut = false;
            try
            {
                await AsyncUtil.WithTimeout(Task.Delay(200), timeoutMs: 20);
            }
            catch (TimeoutException) { timedOut = true; }

            Assert.True(timedOut, "Slow task should timeout");

            Console.WriteLine("\n✅ Done. If you see no exceptions, all current tests passed.");
            Console.WriteLine("Next: add more edge cases (nulls, empty inputs, malformed lines).");
        }
    }

    // ============================================================
    // SECTION 1: Log parsing
    // ============================================================
    public sealed record LogEntry(
        DateTime TimestampUtc,
        string Level,
        string Suite,
        string Test,
        int DurationMs,
        string Status,
        string? Error);

    public sealed record TestSummary(int PassCount, int FailCount, double AvgDurationMs);

    public static class LogAnalyzer
    {
        // TODO:
        // Parse lines like:
        // "2025-12-17T10:01:00Z INFO  Suite=Checkout Test=AddToCart DurationMs=120 Status=PASS"
        // Requirements:
        // - Ignore extra spaces
        // - Error is optional (Error=...)
        // - Throw FormatException for invalid lines
        public static IEnumerable<LogEntry> Parse(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                // Minimal robust approach: split by spaces, but key=val parts matter
                // You may choose: regex, manual scan, etc.
                // Keep it readable and defensive.
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 6) throw new FormatException($"Invalid log line: {line}");

                if (!DateTime.TryParse(parts[0], out var ts)) throw new FormatException($"Bad timestamp: {line}");
                var level = parts[1];

                // Parse key=value tokens starting at index 2
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 2; i < parts.Length; i++)
                {
                    var token = parts[i];
                    var eq = token.IndexOf('=');
                    if (eq <= 0) continue; // allow stray tokens
                    var key = token[..eq];
                    var val = token[(eq + 1)..];
                    dict[key] = val;
                }

                string suite = GetRequired(dict, "Suite", line);
                string test = GetRequired(dict, "Test", line);
                string status = GetRequired(dict, "Status", line);
                int duration = int.TryParse(GetRequired(dict, "DurationMs", line), out var d)
                    ? d : throw new FormatException($"Bad DurationMs: {line}");

                dict.TryGetValue("Error", out var err);

                yield return new LogEntry(
                    TimestampUtc: ts.ToUniversalTime(),
                    Level: level,
                    Suite: suite,
                    Test: test,
                    DurationMs: duration,
                    Status: status,
                    Error: err);
            }

            static string GetRequired(Dictionary<string, string> dict, string key, string line)
                => dict.TryGetValue(key, out var v) ? v : throw new FormatException($"Missing {key}: {line}");
        }

        // TODO:
        // Return dictionary keyed by "Suite|Test"
        // PassCount, FailCount, AvgDurationMs (over all runs)
        public static Dictionary<string, TestSummary> SummarizeByTest(IEnumerable<LogEntry> entries)
        {
            return entries
                .GroupBy(e => $"{e.Suite}|{e.Test}")
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        int pass = g.Count(x => x.Status.Equals("PASS", StringComparison.OrdinalIgnoreCase));
                        int fail = g.Count(x => x.Status.Equals("FAIL", StringComparison.OrdinalIgnoreCase));
                        double avg = g.Average(x => x.DurationMs);
                        return new TestSummary(pass, fail, avg);
                    });
        }
    }

    // ============================================================
    // SECTION 2: Retry policy
    // ============================================================
    public sealed record RunOutcome(bool IsPass, string? Reason)
    {
        public static RunOutcome Pass() => new(true, null);
        public static RunOutcome Fail(string reason) => new(false, reason);
    }

    public static class RetryPolicy
    {
        // TODO:
        // Execute action up to maxAttempts.
        // If it fails AND isTransient(outcome) is true, retry with backoff.
        // Otherwise return immediately.
        public static async Task<RunOutcome> ExecuteAsync(
            Func<Task<RunOutcome>> action,
            Func<RunOutcome, bool> isTransient,
            int maxAttempts,
            Func<int, int> backoffMs,
            CancellationToken ct = default)
        {
            if (maxAttempts <= 0) throw new ArgumentOutOfRangeException(nameof(maxAttempts));

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                var outcome = await action().ConfigureAwait(false);

                if (outcome.IsPass) return outcome;

                bool canRetry = attempt < maxAttempts && isTransient(outcome);
                if (!canRetry) return outcome;

                int delay = Math.Max(0, backoffMs(attempt));
                if (delay > 0) await Task.Delay(delay, ct).ConfigureAwait(false);
            }

            // Unreachable, but compiler-friendly
            return RunOutcome.Fail("Unknown");
        }
    }

    // ============================================================
    // SECTION 3: Failure triage
    // ============================================================
    public sealed record Failure(string Suite, string Test, string Reason, string Stack);

    public static class FailureTriage
    {
        // TODO:
        // A "signature" groups identical failures by (Suite, Test, Reason, Stack)
        // Return list of groups (each group is list of failures), sorted by group size desc.
        public static List<List<Failure>> GroupBySignature(IEnumerable<Failure> failures)
        {
            return failures
                .GroupBy(f => (f.Suite, f.Test, f.Reason, f.Stack))
                .Select(g => g.ToList())
                .OrderByDescending(g => g.Count)
                .ToList();
        }
    }

    // ============================================================
    // SECTION 4: Config validation
    // ============================================================
    public static class ConfigValidator
    {
        // TODO:
        // Rules:
        // - baseUrl required, must be absolute URI
        // - timeoutMs required, integer in [1..120000]
        // - retries required, integer in [0..5]
        // - env optional but if present must be one of: dev, staging, prod
        // Return list of human-readable errors.
        public static List<string> Validate(Dictionary<string, string?> cfg)
        {
            var errors = new List<string>();

            var baseUrl = Get(cfg, "baseUrl");
            if (string.IsNullOrWhiteSpace(baseUrl))
                errors.Add("baseUrl is required");
            else if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
                errors.Add("baseUrl must be an absolute URI");

            if (!TryGetInt(cfg, "timeoutMs", out var timeout))
                errors.Add("timeoutMs is required and must be an integer");
            else if (timeout < 1 || timeout > 120000)
                errors.Add("timeoutMs must be between 1 and 120000");

            if (!TryGetInt(cfg, "retries", out var retries))
                errors.Add("retries is required and must be an integer");
            else if (retries < 0 || retries > 5)
                errors.Add("retries must be between 0 and 5");

            var env = Get(cfg, "env");
            if (!string.IsNullOrWhiteSpace(env))
            {
                var ok = env is "dev" or "staging" or "prod";
                if (!ok) errors.Add("env must be one of: dev, staging, prod");
            }

            return errors;

            static string? Get(Dictionary<string, string?> d, string k)
                => d.TryGetValue(k, out var v) ? v : null;

            static bool TryGetInt(Dictionary<string, string?> d, string k, out int value)
            {
                value = 0;
                if (!d.TryGetValue(k, out var v) || string.IsNullOrWhiteSpace(v)) return false;
                return int.TryParse(v, out value);
            }
        }
    }

    // ============================================================
    // SECTION 5: Result diff
    // ============================================================
    public sealed record TestResult(string Suite, string Test, string Status)
    {
        public string Key => $"{Suite}|{Test}";
    }

    public sealed record DiffResult(
        List<TestResult> NewFailures,
        List<TestResult> Fixed,
        List<TestResult> StillFailing);

    public static class ResultDiff
    {
        // TODO:
        // Compare yesterday vs today keyed by Suite|Test
        // - NewFailures: today FAIL but yesterday missing or PASS
        // - Fixed: today PASS but yesterday FAIL
        // - StillFailing: today FAIL and yesterday FAIL
        public static DiffResult Compute(IEnumerable<TestResult> yesterday, IEnumerable<TestResult> today)
        {
            var y = yesterday.ToDictionary(r => r.Key, r => r.Status, StringComparer.OrdinalIgnoreCase);
            var newFailures = new List<TestResult>();
            var fixedOnes = new List<TestResult>();
            var stillFailing = new List<TestResult>();

            foreach (var t in today)
            {
                var todayFail = t.Status.Equals("FAIL", StringComparison.OrdinalIgnoreCase);
                var todayPass = t.Status.Equals("PASS", StringComparison.OrdinalIgnoreCase);

                y.TryGetValue(t.Key, out var yStatus);
                var yFail = string.Equals(yStatus, "FAIL", StringComparison.OrdinalIgnoreCase);
                var yPass = string.Equals(yStatus, "PASS", StringComparison.OrdinalIgnoreCase);

                if (todayFail && (!y.ContainsKey(t.Key) || yPass)) newFailures.Add(t);
                else if (todayPass && yFail) fixedOnes.Add(t);
                else if (todayFail && yFail) stillFailing.Add(t);
            }

            return new DiffResult(newFailures, fixedOnes, stillFailing);
        }
    }

    // ============================================================
    // SECTION 6: Async timeout wrapper
    // ============================================================
    public sealed record TimeoutResult(bool Completed);

    public static class AsyncUtil
    {
        // TODO:
        // Await task but throw TimeoutException if it doesn't complete within timeoutMs.
        // Return TimeoutResult(Completed=true) if it completes.
        public static async Task<TimeoutResult> WithTimeout(Task task, int timeoutMs, CancellationToken ct = default)
        {
            if (timeoutMs <= 0) throw new ArgumentOutOfRangeException(nameof(timeoutMs));

            var timeoutTask = Task.Delay(timeoutMs, ct);
            var completed = await Task.WhenAny(task, timeoutTask).ConfigureAwait(false);

            if (completed == timeoutTask) throw new TimeoutException($"Timed out after {timeoutMs}ms");

            await task.ConfigureAwait(false);
            return new TimeoutResult(true);
        }
    }

    // ============================================================
    // Minimal assertions (so you can practice without xUnit/NUnit)
    // ============================================================
    public static class Assert
    {
        public static void True(bool condition, string message)
        {
            if (!condition) throw new Exception("ASSERT TRUE FAILED: " + message);
        }

        public static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception($"ASSERT EQUAL FAILED: {message}. Expected={expected} Actual={actual}");
        }
    }
}
