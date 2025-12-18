// CSharp_Proficiency_Practice_Questions.cs
// Paste into a .NET Console App (Program.cs). Run it.
// This file is intentionally "questions in code" (comments + TODO stubs).
// Your job: implement TODOs and/or write short answers in comments where asked.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

internal static class Program
{
    static async Task Main()
    {
        Console.WriteLine("C# Proficiency Practice (SDET-flavored) - Questions in Code");
        Console.WriteLine("Open this file and work through TODOs.\n");

        // Uncomment sections as you implement them.
        // Q01_ValueVsReference.Run();
        // Q02_Equality.Run();
        // Q03_ParsingAndValidation.Run();
        // Q04_LinqAggregation.Run();
        // await Q05_AsyncTimeout.Run();
        // await Q06_ConcurrencyLimit.Run();
        // Q07_DedupeFailures.Run();
        // Q08_DiffResults.Run();

        Console.WriteLine("Done. Implement + uncomment one section at a time.");
    }
}

// ======================================================
// Q01) class vs struct vs record (Value vs Reference)
// ======================================================
internal static class Q01_ValueVsReference
{
    /*
      Questions:
      1) In C#, what’s the difference between class, struct, and record?
      2) When would you choose each in test automation code?
      3) What is copying behavior for structs vs classes?
      4) What are the tradeoffs for mutable structs?
    */

    public static void Run()
    {
        // TODO: Create one class, one struct, and one record each representing TestCase.
        // TODO: Demonstrate copy behavior:
        // - Assign one variable to another
        // - Mutate one instance
        // - Print results (what changed, what didn’t)
        //
        // Bonus: show "with" expression for records.
    }
}

// ======================================================
// Q02) Equality: == vs Equals vs ReferenceEquals
// ======================================================
internal static class Q02_Equality
{
    /*
      Questions:
      1) Explain == vs Equals vs ReferenceEquals.
      2) How do records change equality behavior?
      3) How should you implement equality for a complex type used as a dictionary key?
    */

    public static void Run()
    {
        // TODO: Implement a TestId type that can be used as Dictionary key.
        // Requirements:
        // - Two TestId instances are equal if (Suite, Name) match case-insensitively.
        // - Hash code must align with Equals.
        // - Demonstrate usage in Dictionary<TestId, int>.
        //
        // Hint: consider IEquatable<TestId>.
    }
}

// ======================================================
// Q03) Parsing & Validation (common SDET work)
// ======================================================
internal static class Q03_ParsingAndValidation
{
    /*
      Scenario:
      You receive lines like:
      "Suite=Checkout Test=ApplyCoupon DurationMs=450 Status=FAIL Error=Timeout"

      Questions:
      1) Write a robust parser that handles extra spaces and optional Error.
      2) Decide: throw exceptions vs return (bool ok, value, errorMessage).
      3) What edge cases do you handle?
    */

    public sealed record LogEntry(string Suite, string Test, int DurationMs, string Status, string? Error);

    public static void Run()
    {
        var lines = new[]
        {
            "Suite=Checkout Test=ApplyCoupon DurationMs=450 Status=FAIL Error=Timeout",
            "Suite=Search   Test=BasicSearch DurationMs=80 Status=PASS",
        };

        foreach (var line in lines)
        {
            // TODO: Replace with your parser call.
            // var entry = Parse(line);

            // Console.WriteLine(entry);
        }
    }

    // TODO: Implement. Choose your approach and keep it defensive.
    public static LogEntry Parse(string line)
    {
        throw new NotImplementedException();
    }
}

// ======================================================
// Q04) LINQ aggregation: failure rate & ordering
// ======================================================
internal static class Q04_LinqAggregation
{
    /*
      Questions:
      1) Explain deferred execution in LINQ and one bug it can cause in tests.
      2) Implement summary: group by suite, count pass/fail, compute failure rate, order desc.

      Output example:
      Suite=Checkout Pass=10 Fail=2 FailureRate=0.1667
    */

    public sealed record TestResult(string Suite, string Test, string Status);

    public sealed record SuiteSummary(string Suite, int Pass, int Fail, double FailureRate);

    public static void Run()
    {
        var results = new List<TestResult>
        {
            new("Checkout", "AddToCart", "PASS"),
            new("Checkout", "ApplyCoupon", "FAIL"),
            new("Checkout", "ApplyCoupon", "FAIL"),
            new("Search", "BasicSearch", "PASS"),
            new("Search", "BasicSearch", "PASS"),
        };

        // TODO: Implement SummarizeBySuite and print summaries.
        // var summaries = SummarizeBySuite(results);
        // foreach (var s in summaries) Console.WriteLine(s);
    }

    // TODO: Implement with LINQ. Be careful about division by zero.
    public static List<SuiteSummary> SummarizeBySuite(IEnumerable<TestResult> results)
    {
        throw new NotImplementedException();
    }
}

// ======================================================
// Q05) Async: implement timeout correctly
// ======================================================
internal static class Q05_AsyncTimeout
{
    /*
      Questions:
      1) Why can .Result/.Wait deadlock?
      2) Task.WhenAll vs Task.WhenAny: when to use?
      3) Implement WithTimeout: throw TimeoutException if not completed.
    */

    public static async Task Run()
    {
        // TODO: once implemented, these should behave as described.
        // await WithTimeout(Task.Delay(20), 200);   // should complete
        // await WithTimeout(Task.Delay(200), 20);   // should throw TimeoutException
    }

    // TODO: Implement (no busy waiting). Respect CancellationToken if you add one.
    public static async Task WithTimeout(Task task, int timeoutMs)
    {
        throw new NotImplementedException();
    }
}

// ======================================================
// Q06) Concurrency: limit N tasks at once (CI-friendly)
// ======================================================
internal static class Q06_ConcurrencyLimit
{
    /*
      Scenario:
      You need to run 200 API checks but must limit concurrency to avoid rate limits.

      Questions:
      1) Implement RunWithMaxConcurrency(items, max, worker)
      2) What ordering guarantees do you want (if any)?
      3) How do you handle partial failures?
    */

    public static async Task Run()
    {
        var items = Enumerable.Range(1, 20).ToList();

        // TODO: implement then uncomment.
        // var sw = Stopwatch.StartNew();
        // var results = await RunWithMaxConcurrency(
        //     items,
        //     maxConcurrency: 4,
        //     worker: async i =>
        //     {
        //         await Task.Delay(50);
        //         return i * 2;
        //     });
        // sw.Stop();
        // Console.WriteLine($"Count={results.Count}, Example={results[0]}, ElapsedMs={sw.ElapsedMilliseconds}");
    }

    // TODO: Implement using SemaphoreSlim or Channel.
    public static Task<List<TResult>> RunWithMaxConcurrency<TItem, TResult>(
        IReadOnlyList<TItem> items,
        int maxConcurrency,
        Func<TItem, Task<TResult>> worker)
    {
        throw new NotImplementedException();
    }
}

// ======================================================
// Q07) Dedupe failures by signature (triage)
// ======================================================
internal static class Q07_DedupeFailures
{
    /*
      Questions:
      1) What fields should be in a failure signature (suite/test/reason/stack/build?) and why?
      2) Implement grouping and sorting by frequency.
    */

    public sealed record Failure(string Suite, string Test, string Reason, string Stack);

    public static void Run()
    {
        var fails = new[]
        {
            new Failure("Checkout", "ApplyCoupon", "Timeout", "stackA"),
            new Failure("Checkout", "ApplyCoupon", "Timeout", "stackA"),
            new Failure("Checkout", "ApplyCoupon", "Assertion", "stackB"),
            new Failure("Search", "BasicSearch", "Timeout", "stackC"),
        };

        // TODO: implement GroupBySignature, then print:
        // signature + count
        // var groups = GroupBySignature(fails);
    }

    // TODO: Return groups sorted by descending Count.
    public static List<(string Signature, int Count)> GroupBySignature(IEnumerable<Failure> failures)
    {
        throw new NotImplementedException();
    }
}

// ======================================================
// Q08) Diff test results: new failures vs fixed
// ======================================================
internal static class Q08_DiffResults
{
    /*
      Questions:
      1) Define “NewFailure” and “Fixed” precisely.
      2) Implement diff keyed by Suite|Test.
      3) How do you handle missing tests or renamed tests?
    */

    public sealed record TestResult(string Suite, string Test, string Status)
    {
        public string Key => $"{Suite}|{Test}";
    }

    public sealed record Diff(
        List<TestResult> NewFailures,
        List<TestResult> Fixed,
        List<TestResult> StillFailing);

    public static void Run()
    {
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

        // TODO: implement ComputeDiff and print counts.
        // var diff = ComputeDiff(yesterday, today);
        // Console.WriteLine($"NewFailures={diff.NewFailures.Count}, Fixed={diff.Fixed.Count}, StillFailing={diff.StillFailing.Count}");
    }

    // TODO: Implement:
    // - NewFailures: today FAIL and (yesterday missing OR yesterday PASS)
    // - Fixed: today PASS and yesterday FAIL
    // - StillFailing: today FAIL and yesterday FAIL
    public static Diff ComputeDiff(IEnumerable<TestResult> yesterday, IEnumerable<TestResult> today)
    {
        throw new NotImplementedException();
    }
}

// ======================================================
// Extra quick “write answer in code” prompts (no TODOs)
// ======================================================
internal static class WriteAnswersHere
{
    /*
      Write short answers (2-6 lines each) in comments:

      A) Explain boxing/unboxing and one place it shows up unexpectedly in C#.
      B) Why is catching Exception usually a smell? Name one acceptable case.
      C) IEnumerable vs IList vs IReadOnlyList: what can each guarantee?
      D) String concatenation in a loop: what’s wrong and what to do instead?
      E) Explain “nondeterminism” in tests and 5 common causes.
    */
}
