#nullable enable
#r "nuget: Humanizer, 2.14.1"

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;

static string ThisFile([CallerFilePath] string p = "") => p;
var scriptDir  = Path.GetDirectoryName(ThisFile())!;
var projectDir = Path.GetFullPath(Path.Combine(scriptDir, "..")); // src/TinyBDD
var outDir     = Path.Combine(projectDir, "Generated");
Directory.CreateDirectory(outDir);

// ===== Dimensions =====
enum Step  { When, And, But, Then }
enum Kind  { Transform, Effect, Predicate, Assert, AssertTransformOut }
enum Title { Explicit, Auto }
enum Async { Sync, Task, ValueTask }
enum Token { None, Token }

static readonly Title[] Titles  = { Title.Explicit, Title.Auto };
static readonly Async[] Asyncs  = { Async.Sync, Async.Task, Async.ValueTask };
static readonly Token[] Tokens  = { Token.None, Token.Token };

// ===== Tiny DSL for methods =====
sealed record MethodSpec(
    string Summary,
    string[] XmlExtras,
    string ReturnType,
    string Name,
    string Generics,
    (string Type, string Name)[] Parameters,
    string Body
);

// ---- helpers to keep things DRY ----
static string[] Xml(params string?[] lines) =>
    lines.Where(l => l is not null).Select(l => l!).ToArray();

static string XmlEscape(string s) => s
    .Replace("&", "&amp;")
    .Replace("<", "&lt;")
    .Replace(">", "&gt;");

string TP(Kind k) => k == Kind.Transform ? "<TOut>" : "";
string TypeOut(Kind k) => k switch {
    Kind.Transform          => "TOut",
    Kind.Predicate          => "bool",
    Kind.Effect             => "T",
    Kind.Assert             => "T",
    Kind.AssertTransformOut => "TOut",
    _                       => "T"
};

string Summary(Step step, Kind kind, Title title, Async @async, Token token)
{
    var ttl = title == Title.Explicit ? "with an explicit title" : "with a default title";
    return kind switch {
        Kind.Transform => $"Adds a <c>{step}</c> transformation {ttl} using {(token==Token.Token?"a token-aware ":"")}{@async.Humanize(LetterCasing.LowerCase)} function.",
        Kind.Effect    => $"Adds a <c>{step}</c> side-effect {ttl}. Keeps the current value.",
        Kind.Predicate => $"Adds a <c>Then</c> boolean assertion {ttl}{(token==Token.Token?" observing a token.":"")}",
        Kind.Assert    => $"Adds a <c>Then</c> assertion {ttl}{(token==Token.Token?" observing a token.":"")}",
        Kind.AssertTransformOut => $"Adds a <c>Then</c> transform that returns a value used only for assertion side-effects.",
        _ => ""
    };
}

// Produce the C# for a MethodSpec (single responsibility: rendering)
string Render(MethodSpec m)
{
    var sb = new StringBuilder();
    sb.AppendLine("    /// <summary>");
    sb.AppendLine("    /// " + m.Summary);
    sb.AppendLine("    /// </summary>");
    
    foreach (var x in m.XmlExtras)
    {
        var line = x?.TrimStart() ?? "";
        if (line.Length == 0) continue;
        // If caller already provided a "/// ..." line, don't add another "///"
        sb.AppendLine(line.StartsWith("///") ? "    " + line
                                             : "    /// " + line);
    }
    
    var paramList = string.Join(", ", m.Parameters.Select(p => $"{p.Type} {p.Name}"));
    sb.AppendLine($"    public {m.ReturnType} {m.Name}{m.Generics}({paramList}) =>");
    sb.AppendLine($"        {m.Body};");
    sb.AppendLine();
    return sb.ToString();
}

// ===== Signature matrix (only what ToCT(...) supports in your core) =====
bool TrySig(Step step, Kind kind, Async @async, Token token, out string sig)
{
    string tIn  = "T";
    string tOut = TypeOut(kind);
    sig = "";

    switch (kind)
    {
        case Kind.Transform:
            if (token == Token.None)
                sig = @async switch {
                    Async.Sync      => $"Func<{tIn}, {tOut}>",
                    Async.Task      => $"Func<{tIn}, Task<{tOut}>>",
                    Async.ValueTask => $"Func<{tIn}, ValueTask<{tOut}>>",
                    _               => ""
                };
            else
                sig = @async switch {
                    Async.Task      => $"Func<{tIn}, CancellationToken, Task<{tOut}>>",
                    Async.ValueTask => $"Func<{tIn}, CancellationToken, ValueTask<{tOut}>>",
                    _               => ""
                };
            return sig.Length > 0;

        case Kind.Effect:
            if (token == Token.None)
                sig = @async switch {
                    Async.Sync      => $"Action<{tIn}>",
                    Async.Task      => $"Func<{tIn}, Task>",
                    Async.ValueTask => $"Func<{tIn}, ValueTask>",
                    _               => ""
                };
            else
                sig = @async switch {
                    Async.Task      => $"Func<{tIn}, CancellationToken, Task>",
                    Async.ValueTask => $"Func<{tIn}, CancellationToken, ValueTask>",
                    _               => ""
                };
            return sig.Length > 0;

        case Kind.Predicate:
            if (token == Token.None)
                sig = @async switch {
                    Async.Sync      => $"Func<{tIn}, bool>",
                    Async.Task      => $"Func<{tIn}, Task<bool>>",
                    Async.ValueTask => $"Func<{tIn}, ValueTask<bool>>",
                    _               => ""
                };
            else
                sig = @async switch {
                    Async.Task      => $"Func<{tIn}, CancellationToken, Task<bool>>",
                    Async.ValueTask => $"Func<{tIn}, CancellationToken, ValueTask<bool>>",
                    _               => ""
                };
            return sig.Length > 0;

        case Kind.Assert:
            if (token == Token.None)
                sig = @async switch {
                    Async.Task      => $"Func<{tIn}, Task>",
                    Async.ValueTask => $"Func<{tIn}, ValueTask>",
                    _               => ""
                };
            else
                sig = @async switch {
                    Async.Task      => $"Func<{tIn}, CancellationToken, Task>",
                    Async.ValueTask => $"Func<{tIn}, CancellationToken, ValueTask>",
                    _               => ""
                };
            return sig.Length > 0;

        case Kind.AssertTransformOut:
            if (token == Token.None)
                sig = $"Func<{tIn}, Task<{tOut}>>";
            else
                sig = $"Func<{tIn}, CancellationToken, Task<{tOut}>>";
            return true;
    }
    return false;
}

// ===== Builders (pure functions from descriptors -> MethodSpec) =====
MethodSpec MakeWhen(Kind kind, Title title, Async @async, Token token, string sig) =>
    new(
        Summary(Step.When, kind, title, @async, token),
        XmlExtras(kind, title),
        kind == Kind.Transform ? "ScenarioChain<TOut>" : "ScenarioChain<T>",
        "When",
        TP(kind),
        Params(title, sig, kind),
        kind == Kind.Transform
            ? $"Transform(StepPhase.When, StepWord.Primary, {TitleArg(title)}, ToCT(f))"
            : $"Effect(StepPhase.When, StepWord.Primary, {TitleArg(title)}, ToCT(effect))"
    );

MethodSpec MakeAndOrBut(string wordName, string wordEnum, Kind kind, Title title, Async @async, Token token, string sig) =>
    new(
        Summary(Enum.Parse<Step>(wordName), kind, title, @async, token),
        XmlExtras(kind, title),
        kind == Kind.Transform ? "ScenarioChain<TOut>" : "ScenarioChain<T>",
        wordName,
        TP(kind),
        Params(title, sig, kind),
        kind == Kind.Transform
            ? $"TransformInherit({wordEnum}, {TitleArg(title)}, ToCT(f))"
            : $"EffectInherit({wordEnum}, {TitleArg(title)}, ToCT(effect))"
    );
    
MethodSpec MakeThenAssertAction(Title title) =>
    new(
        $"Adds a <c>Then</c> assertion {(title==Title.Explicit ? "with an explicit title" : "with a default title")} using a synchronous action.",
        XmlExtras(Kind.Assert, title),
        "ThenChain<T>",
        "Then",
        "",
        Params(title, "Action<T>", Kind.Assert, explicitName: "assertion"),
        $"ThenAssert({TitleArg(title)}, ToCT(assertion))"
    );

MethodSpec MakeThenAssertActionNoValue(Title title) =>
    new(
        $"Adds a <c>Then</c> assertion {(title==Title.Explicit ? "with an explicit title" : "with a default title")} (no value parameter) using a synchronous action.",
        XmlExtras(Kind.Assert, title, noValue:true),
        "ThenChain<T>",
        "Then",
        "",
        Params(title, "Action", Kind.Assert, explicitName: "assertion"),
        $"ThenAssert({TitleArg(title)}, ToCT(assertion))"
    );

MethodSpec MakeThenPredicate(Title title, Async @async, Token token, string sig) =>
    new(
        Summary(Step.Then, Kind.Predicate, title, @async, token),
        XmlExtras(Kind.Predicate, title),
        "ThenChain<T>",
        "Then",
        "",
        Params(title, sig, Kind.Predicate, explicitName: "predicate"),
        $"ThenPredicate({TitleArg(title)}, ToCT(predicate))"
    );

MethodSpec MakeThenPredicateNoValue(Title title, Async @async)
{
    var (sig, ct) = @async == Async.Sync
        ? ("Func<bool>", "ToCT(predicate)")
        : ("Func<Task<bool>>", "ToCT(predicate)");
    return new(
        $"Adds a <c>Then</c> boolean assertion {(title==Title.Explicit?"with an explicit title":"with a default title")} (no value parameter).",
        XmlExtras(Kind.Predicate, title, noValue:true),
        "ThenChain<T>",
        "Then",
        "",
        Params(title, sig, Kind.Predicate, explicitName: "predicate"),
        $"ThenPredicateNoValue({TitleArg(title)}, {ct})"
    );
}

MethodSpec MakeThenAssert(Title title, Async @async, Token token, string sig) =>
    new(
        Summary(Step.Then, Kind.Assert, title, @async, token),
        XmlExtras(Kind.Assert, title),
        "ThenChain<T>",
        "Then",
        "",
        Params(title, sig, Kind.Assert, explicitName: "assertion"),
        $"ThenAssert({TitleArg(title)}, ToCT(assertion))"
    );

MethodSpec MakeThenAssertNoValue(Title title, Async @async)
{
    var sig = @async == Async.Task ? "Func<Task>" : "Func<ValueTask>";
    return new(
        $"Adds a <c>Then</c> assertion {(title==Title.Explicit?"with an explicit title":"with a default title")} (no value parameter).",
        XmlExtras(Kind.Assert, title, noValue:true),
        "ThenChain<T>",
        "Then",
        "",
        Params(title, sig, Kind.Assert, explicitName: "assertion"),
        $"ThenAssert({TitleArg(title)}, (_, _) => assertion())"
    );
}

MethodSpec MakeThenAssertTransformOut(Title title, Token token, string sig) =>
    new(
        Summary(Step.Then, Kind.AssertTransformOut, title, Async.Task, token),
        Xml(
            "/// <typeparam name=\"TOut\">The result produced by the assertion delegate (ignored by the chain).</typeparam>",
            title == Title.Explicit ? "/// <param name=\"title\">Display title for the assertion.</param>" : null,
            "/// <param name=\"assertion\">Asynchronous function that receives the carried value"
            + (token==Token.Token ? " and a token" : "")
            + ".</param>",
            "/// <returns>A <see cref=\"ThenChain{TOut}\"/> for further chaining.</returns>"
        ),
        "ThenChain<TOut>",
        "Then",
        "<TOut>",
        Params(title, sig, Kind.AssertTransformOut, explicitName: "assertion"),
        token == Token.None
            ? $"ThenAssert({TitleArg(title)}, (v, _) => assertion(v))"
            : $"ThenAssert({TitleArg(title)}, assertion)"
    );

// ===== Small helpers =====
(string Type, string Name)[] Params(Title title, string sig, Kind? kind = null, string? explicitName = null)
{
    string name =
        explicitName
        ?? (kind switch
        {
            Kind.Transform           => "f",
            Kind.Effect              => "effect",
            Kind.Predicate           => "predicate",
            Kind.Assert              => "assertion",
            Kind.AssertTransformOut  => "assertion",
            _                        => "arg"
        });

    return title == Title.Explicit
        ? new[] { ("string", "title"), (sig, name) }
        : new[] { (sig, name) };
}

string TitleArg(Title t) => t == Title.Explicit ? "title" : "string.Empty";

string[] XmlExtras(Kind kind, Title title, bool noValue = false)
{
    var list = new System.Collections.Generic.List<string>();
    if (kind == Kind.Transform)
    {
        list.Add("/// <typeparam name=\"TOut\">The result type of the transformation.</typeparam>");
        if (title == Title.Explicit) list.Add("/// <param name=\"title\">Display title for this step.</param>");
        list.Add("/// <param name=\"f\">Transformation applied to the carried value.</param>");
        list.Add("/// <returns>A new <see cref=\"ScenarioChain{TOut}\"/> carrying the transformed value.</returns>");
    }
    else if (kind == Kind.Effect)
    {
        if (title == Title.Explicit) list.Add("/// <param name=\"title\">Display title for this step.</param>");
        list.Add("/// <param name=\"effect\">Side-effect that receives the carried value.</param>");
        list.Add("/// <returns>The same <see cref=\"ScenarioChain{T}\"/> for further chaining.</returns>");
    }
    else if (kind == Kind.Predicate)
    {
        if (title == Title.Explicit) list.Add("/// <param name=\"title\">Display title for the assertion.</param>");
        list.Add(noValue ? "/// <param name=\"predicate\">Predicate to evaluate.</param>" :
                           "/// <param name=\"predicate\">Predicate evaluated against the carried value.</param>");
        list.Add("/// <returns>A <see cref=\"ThenChain{T}\"/> for further chaining.</returns>");
    }
    else if (kind == Kind.Assert)
    {
        if (title == Title.Explicit) list.Add("/// <param name=\"title\">Display title for the assertion.</param>");
        list.Add(noValue ? "/// <param name=\"assertion\">Asynchronous assertion.</param>" :
                           "/// <param name=\"assertion\">Asynchronous assertion that receives the carried value.</param>");
        list.Add("/// <returns>A <see cref=\"ThenChain{T}\"/> for further chaining.</returns>");
    }
    return list.ToArray();
}

// ===== Emitters (each is a single chain, no nesting) =====
void Emit_When()
{
    var methods =
        new[] { Kind.Transform, Kind.Effect }
            .SelectMany(kind => Titles, (kind, title) => new { kind, title })
            .SelectMany(k => Asyncs, (k, @async) => new { k.kind, k.title, @async })
            .SelectMany(k => Tokens, (k, token) =>
            {
                var ok = TrySig(Step.When, k.kind, k.@async, token, out var sig);
                return new { k.kind, k.title, k.@async, token, ok, sig };
            })
            .Where(x => x.ok)
            .Select(x => MakeWhen(x.kind, x.title, x.@async, x.token, x.sig));

    var file =
        "// <auto-generated/>\nnamespace TinyBDD;\npublic sealed partial class ScenarioChain<T>\n{\n" +
        string.Concat(methods.Select(Render)) +
        "}\n";

    File.WriteAllText(Path.Combine(outDir, "ScenarioChain.When.g.cs"), file);
}

void Emit_AndBut()
{
    string Build(string wordName, string wordEnum)
    {
        var methods =
            new[] { Kind.Transform, Kind.Effect }
                .SelectMany(kind => Titles, (kind, title) => new { kind, title })
                .SelectMany(k => Asyncs, (k, @async) => new { k.kind, k.title, @async })
                .SelectMany(k => Tokens, (k, token) =>
                {
                    var ok = TrySig(Step.And, k.kind, k.@async, token, out var sig);
                    return new { k.kind, k.title, k.@async, token, ok, sig };
                })
                .Where(x => x.ok)
                .Select(x => MakeAndOrBut(wordName, wordEnum, x.kind, x.title, x.@async, x.token, x.sig));

        return string.Concat(methods.Select(Render));
    }

    var file =
        "// <auto-generated/>\nnamespace TinyBDD;\npublic sealed partial class ScenarioChain<T>\n{\n" +
        Build("And", "StepWord.And") +
        Build("But", "StepWord.But") +
        "}\n";

    File.WriteAllText(Path.Combine(outDir, "ScenarioChain.AndBut.g.cs"), file);
}

void Emit_Then()
{
    // value-based predicates
    var preds =
        Titles
            .SelectMany(title => Asyncs, (title, @async) => new { title, @async })
            .SelectMany(t => Tokens, (t, token) =>
            {
                var ok = TrySig(Step.Then, Kind.Predicate, t.@async, token, out var sig);
                return new { t.title, t.@async, token, ok, sig };
            })
            .Where(x => x.ok)
            .Select(x => MakeThenPredicate(x.title, x.@async, x.token, x.sig));

    // no-value predicates (sync + Task only)
    var predsNoValue =
        Titles
            .SelectMany(title => new[] { Async.Sync, Async.Task },
                        (title, @async) => MakeThenPredicateNoValue(title, @async));

    // value-based asserts
    var asserts =
        Titles
            .SelectMany(title => new[] { Async.Task, Async.ValueTask }, (title, @async) => new { title, @async })
            .SelectMany(t => Tokens, (t, token) =>
            {
                var ok = TrySig(Step.Then, Kind.Assert, t.@async, token, out var sig);
                return new { t.title, t.@async, token, ok, sig };
            })
            .Where(x => x.ok)
            .Select(x => MakeThenAssert(x.title, x.@async, x.token, x.sig));

    // no-value asserts (no token-aware form in public surface)
    var assertsNoValue =
        Titles
            .SelectMany(title => new[] { Async.Task, Async.ValueTask },
                        (title, @async) => MakeThenAssertNoValue(title, @async));

    // assertion transform returning TOut (Task only; token aware + unaware)
    var assertOut =
        Titles
            .SelectMany(title => Tokens, (title, token) =>
            {
                var sig = token == Token.None ? "Func<T, Task<TOut>>"
                                              : "Func<T, CancellationToken, Task<TOut>>";
                return MakeThenAssertTransformOut(title, token, sig);
            });

    // synchronous Action<T> assertions (explicit + auto)
    var actionAsserts =
        Titles.Select(title => MakeThenAssertAction(title));

    // synchronous Action (no value) assertions (explicit + auto)
    var actionAssertsNoValue =
        Titles.Select(title => MakeThenAssertActionNoValue(title));

    var file =
        "// <auto-generated/>\nnamespace TinyBDD;\npublic sealed partial class ScenarioChain<T>\n{\n" +
        string.Concat(preds.Select(Render)) +
        string.Concat(predsNoValue.Select(Render)) +
        string.Concat(asserts.Select(Render)) +
        string.Concat(assertsNoValue.Select(Render)) +
        string.Concat(actionAsserts.Select(Render)) +          // <-- added
        string.Concat(actionAssertsNoValue.Select(Render)) +   // <-- added
        string.Concat(assertOut.Select(Render)) +
        "}\n";

    File.WriteAllText(Path.Combine(outDir, "ScenarioChain.Then.g.cs"), file);

}


// ==================== BDD.Given generator ====================

static string BddGen_ThisFile([CallerFilePath] string p = "") => p;
var BddGen_scriptDir  = Path.GetDirectoryName(BddGen_ThisFile())!;
var BddGen_projectDir = Path.GetFullPath(Path.Combine(BddGen_scriptDir, "..")); // src/TinyBDD
var BddGen_outDir     = Path.Combine(BddGen_projectDir, "Generated");
Directory.CreateDirectory(BddGen_outDir);

enum BddGenTitle { Explicit, Auto }
enum SeedDoc { None, SeedPassed, CarriedAfter, ActionSeed }

sealed record BddGenMethod(
    string Summary,
    string ReturnType,
    string Name,                 // Given
    string Generics,             // <T>, <TIn,TOut>
    (string Type, string Name)[] Params,
    string Body,                 // Seed(ctx, TitleExpr, Wrap(...))
    string[] Xml
);

(string Type, string Name)[] BddGenP(params (string, string)[] ps) => ps;

(string Type, string Name)[] BddGenParams(BddGenTitle t, params (string Type, string Name)[] rest)
{
  var head = t == BddGenTitle.Explicit
    ? new[] { ("ScenarioContext","ctx"), ("string","title") }
    : new[] { ("ScenarioContext","ctx") };
  return head.Concat(rest).ToArray();
}

string BddGen_TitleExpr(BddGenTitle t, string autoType)
  => t == BddGenTitle.Explicit ? "title" : $"AutoTitle<{autoType}>()";

string BddGen_Summary(BddGenTitle t, string shape, bool tokenAware)
{
  var ttl   = t == BddGenTitle.Explicit ? "with an explicit title" : "with an auto-generated title";
  var token = tokenAware ? " token-aware" : "";
  var safeShape = XmlEscape(shape);
  return $"Starts a{token} <c>Given</c> step {ttl} using {safeShape}.";
}

string BddGen_Render(BddGenMethod m)
{
  var sb = new StringBuilder();
  sb.AppendLine("    /// <summary>");
  sb.AppendLine("    /// " + m.Summary);
  sb.AppendLine("    /// </summary>");
  foreach (var line in m.Xml)
  {
    var l = line?.TrimStart() ?? "";
    if (l.Length == 0) continue;
    sb.AppendLine(l.StartsWith("///") ? "    " + l
                                      : "    /// " + l);
  }

  var @params = string.Join(", ", m.Params.Select(p => $"{p.Type} {p.Name}"));
  sb.AppendLine($"    public static {m.ReturnType} {m.Name}{m.Generics}({@params}) =>");
  sb.AppendLine($"        {m.Body};");
  sb.AppendLine();
  return sb.ToString();
}

// ----- central XML builder to keep comments uniform & DRY -----
string[] BddXml(BddGenTitle t, string generics, string setupSentence, string returnGeneric, SeedDoc seedDoc)
{
  var lines = new System.Collections.Generic.List<string>();

  if (t == BddGenTitle.Explicit)
    lines.Add("/// <param name=\"title\">Human-friendly step title.</param>");

  // typeparams
  var gens = generics.Trim().TrimStart('<').TrimEnd('>');
  var parts = gens.Length == 0 ? Array.Empty<string>() : gens.Split(',').Select(s => s.Trim());

  foreach (var g in parts)
  {
    if (g == "T")
    {
      var text = seedDoc switch {
        SeedDoc.ActionSeed  => "The type produced by the seed value.",
        SeedDoc.CarriedAfter=> "Type carried in the chain.",
        _                   => "The type produced by the setup function."
      };
      lines.Add($"/// <typeparam name=\"T\">{text}</typeparam>");
    }
    else if (g == "TIn")
    {
      lines.Add("/// <typeparam name=\"TIn\">Seed type.</typeparam>");
    }
    else if (g == "TOut")
    {
      lines.Add("/// <typeparam name=\"TOut\">Result type.</typeparam>");
    }
  }

  // params
  // NOTE: no spaces after commas in cref — more robust across compilers
  lines.Add("/// <param name=\"ctx\">Scenario context created by <see cref=\"CreateContext(object,string,ITraitBridge,ScenarioOptions)\"/>.</param>");
  lines.Add($"/// <param name=\"setup\">{setupSentence}</param>");

  if (seedDoc != SeedDoc.None)
  {
    var seedText = seedDoc switch {
      SeedDoc.SeedPassed   => "Seed value passed to <paramref name=\"setup\"/>.",
      SeedDoc.CarriedAfter => "Value carried after the side-effect.",
      SeedDoc.ActionSeed   => "Value seeded into the chain after performing <paramref name=\"setup\"/>.",
      _ => ""
    };
    lines.Add($"/// <param name=\"seed\">{seedText}</param>");
  }

  // returns
  lines.Add($"/// <returns>A <see cref=\"ScenarioChain{{{returnGeneric}}}\"/> for further chaining.</returns>");

  return lines.ToArray();
}

// ---- method groups ----
IEnumerable<BddGenMethod> BddGen_NoSeed(BddGenTitle t)
{
  // sync
  yield return new(
    BddGen_Summary(t, "a synchronous factory", false),
    "ScenarioChain<T>", "Given", "<T>",
    BddGenParams(t, ("Func<T>","setup")),
    $"Seed(ctx, {BddGen_TitleExpr(t, "T")}, Wrap(setup))",
    BddXml(t, "<T>", "Synchronous factory for the initial value.", "T", SeedDoc.None)
  );

  // Task<T>
  yield return new(
    BddGen_Summary(t, "an asynchronous Task-producing factory", false),
    "ScenarioChain<T>", "Given", "<T>",
    BddGenParams(t, ("Func<Task<T>>","setup")),
    $"Seed(ctx, {BddGen_TitleExpr(t, "T")}, Wrap(setup))",
    BddXml(t, "<T>", "Task-producing factory for the initial value.", "T", SeedDoc.None)
  );

  // ValueTask<T>
  yield return new(
    BddGen_Summary(t, "a ValueTask-producing factory", false),
    "ScenarioChain<T>", "Given", "<T>",
    BddGenParams(t, ("Func<ValueTask<T>>","setup")),
    $"Seed(ctx, {BddGen_TitleExpr(t, "T")}, Wrap(setup))",
    BddXml(t, "<T>", "ValueTask-producing factory for the initial value.", "T", SeedDoc.None)
  );

  // CT Task<T>
  yield return new(
    BddGen_Summary(t, "an asynchronous factory that observes a CancellationToken", true),
    "ScenarioChain<T>", "Given", "<T>",
    BddGenParams(t, ("Func<CancellationToken, Task<T>>","setup")),
    $"Seed(ctx, {BddGen_TitleExpr(t, "T")}, Wrap(setup))",
    BddXml(t, "<T>", "Asynchronous factory that observes a <see cref=\"CancellationToken\"/>.", "T", SeedDoc.None)
  );

  // CT ValueTask<T>
  yield return new(
    BddGen_Summary(t, "a ValueTask-producing factory that observes a CancellationToken", true),
    "ScenarioChain<T>", "Given", "<T>",
    BddGenParams(t, ("Func<CancellationToken, ValueTask<T>>","setup")),
    $"Seed(ctx, {BddGen_TitleExpr(t, "T")}, Wrap(setup))",
    BddXml(t, "<T>", "ValueTask-producing factory that observes a <see cref=\"CancellationToken\"/>.", "T", SeedDoc.None)
  );
}

IEnumerable<BddGenMethod> BddGen_SeedValue(BddGenTitle t)
{
  // Action + seed T
  yield return new(
    BddGen_Summary(t, "a synchronous setup action plus a seed value", false),
    "ScenarioChain<T>", "Given", "<T>",
    BddGenParams(t, ("Action","setup"), ("T","seed")),
    $"Seed(ctx, {BddGen_TitleExpr(t, "T")}, Wrap(setup, seed))",
    BddXml(t, "<T>", "Synchronous action that performs setup side-effects.", "T", SeedDoc.ActionSeed)
  );

  // Func<TIn,TOut> + seed
  yield return new(
    BddGen_Summary(t, "a synchronous factory that accepts a seed value", false),
    "ScenarioChain<TOut>", "Given", "<TIn, TOut>",
    BddGenParams(t, ("Func<TIn,TOut>","setup"), ("TIn","seed")),
    $"Seed(ctx, {BddGen_TitleExpr(t, "TOut")}, Wrap(setup, seed))",
    BddXml(t, "<TIn, TOut>", "Synchronous factory for the initial value.", "TOut", SeedDoc.SeedPassed)
  );

  // token-aware: TIn,CT -> Task<TOut>
  yield return new(
    BddGen_Summary(t, "an async factory (Task) that accepts a seed value and observes a CancellationToken", true),
    "ScenarioChain<TOut>", "Given", "<TIn, TOut>",
    BddGenParams(t, ("Func<TIn, CancellationToken, Task<TOut>>","setup"), ("TIn","seed")),
    $"Seed(ctx, {BddGen_TitleExpr(t, "TOut")}, Wrap(setup, seed))",
    BddXml(t, "<TIn, TOut>", "Asynchronous factory that observes a <see cref=\"CancellationToken\"/>.", "TOut", SeedDoc.SeedPassed)
  );

  // token-aware: TIn,CT -> ValueTask<TOut>
  yield return new(
    BddGen_Summary(t, "an async factory (ValueTask<T>) that accepts a seed value and observes a CancellationToken", true),
    "ScenarioChain<TOut>", "Given", "<TIn, TOut>",
    BddGenParams(t, ("Func<TIn, CancellationToken, ValueTask<TOut>>","setup"), ("TIn","seed")),
    $"Seed(ctx, {BddGen_TitleExpr(t, "TOut")}, ct => setup(seed, ct))",
    BddXml(t, "<TIn, TOut>", "ValueTask-producing factory that observes a <see cref=\"CancellationToken\"/>.", "TOut", SeedDoc.SeedPassed)
  );

  // token-aware side-effects that keep T (ValueTask)
  yield return new(
    BddGen_Summary(t, "an async side-effect (ValueTask) that observes a CancellationToken and keeps the seed value", true),
    "ScenarioChain<T>", "Given", "<T>",
    BddGenParams(t, ("Func<CancellationToken, ValueTask>","setup"), ("T","seed")),
    $"Seed(ctx, {BddGen_TitleExpr(t, "T")}, Wrap(setup, seed))",
    BddXml(t, "<T>", "Asynchronous side-effect that observes a <see cref=\"CancellationToken\"/>.", "T", SeedDoc.CarriedAfter)
  );

  // token-aware side-effects that keep T (Task)
  yield return new(
    BddGen_Summary(t, "an async side-effect (Task) that observes a CancellationToken and keeps the seed value", true),
    "ScenarioChain<T>", "Given", "<T>",
    BddGenParams(t, ("Func<CancellationToken, Task>","setup"), ("T","seed")),
    $"Seed(ctx, {BddGen_TitleExpr(t, "T")}, Wrap(setup, seed))",
    BddXml(t, "<T>", "Asynchronous side-effect that observes a <see cref=\"CancellationToken\"/>.", "T", SeedDoc.CarriedAfter)
  );

  // token-aware: T,CT -> ValueTask<T>
  yield return new(
    BddGen_Summary(t, "an async factory (ValueTask<T>) that accepts a seed value and observes a CancellationToken", true),
    "ScenarioChain<T>", "Given", "<T>",
    BddGenParams(t, ("Func<T, CancellationToken, ValueTask<T>>","setup"), ("T","seed")),
    // IMPORTANT: disambiguate Wrap overload with explicit <T>
    $"Seed(ctx, {BddGen_TitleExpr(t, "T")}, Wrap<T>(setup, seed))",
    BddXml(t, "<T>", "Asynchronous factory that observes a <see cref=\"CancellationToken\"/>.", "T", SeedDoc.SeedPassed)
  );

  // token-aware: T,CT -> ValueTask (side-effect that keeps T)
  yield return new(
    BddGen_Summary(t, "an async side-effect (ValueTask) that accepts a seed value and observes a CancellationToken", true),
    "ScenarioChain<T>", "Given", "<T>",
    BddGenParams(t, ("Func<T, CancellationToken, ValueTask>","setup"), ("T","seed")),
    $"Seed(ctx, {BddGen_TitleExpr(t, "T")}, Wrap(setup, seed))",
    BddXml(t, "<T>", "Asynchronous side-effect that observes a <see cref=\"CancellationToken\"/>.", "T", SeedDoc.CarriedAfter)
  );
}

IEnumerable<BddGenMethod> BddGen_SeedTask(BddGenTitle t)
{
  // Task<TIn> seed: Func<Task<TIn>, TOut>
  yield return new(
    BddGen_Summary(t, "a synchronous factory that accepts Task-based seed", false),
    "ScenarioChain<TOut>", "Given", "<TIn, TOut>",
    BddGenParams(t, ("Func<Task<TIn>, TOut>","setup"), ("Task<TIn>","seed")),
    $"Seed(ctx, {BddGen_TitleExpr(t, "TOut")}, Wrap(setup, seed))",
    BddXml(t, "<TIn, TOut>", "Factory that accepts a <see cref=\"Task{TIn}\"/>.", "TOut", SeedDoc.SeedPassed)
  );

  // Task<TIn> seed: token-aware → TOut
  yield return new(
    BddGen_Summary(t, "a synchronous factory that accepts Task-based seed and observes a CancellationToken", true),
    "ScenarioChain<TOut>", "Given", "<TIn, TOut>",
    BddGenParams(t, ("Func<Task<TIn>, CancellationToken, TOut>","setup"), ("Task<TIn>","seed")),
    $"Seed(ctx, {BddGen_TitleExpr(t, "TOut")}, Wrap(setup, seed))",
    BddXml(t, "<TIn, TOut>", "Factory that observes a <see cref=\"CancellationToken\"/>.", "TOut", SeedDoc.SeedPassed)
  );

  // Task<TIn> seed: token-aware → Task<TOut>
  yield return new(
    BddGen_Summary(t, "an async factory (Task) that accepts Task-based seed and observes a CancellationToken", true),
    "ScenarioChain<TOut>", "Given", "<TIn, TOut>",
    BddGenParams(t, ("Func<Task<TIn>, CancellationToken, Task<TOut>>","setup"), ("Task<TIn>","seed")),
    $"Seed(ctx, {BddGen_TitleExpr(t, "TOut")}, Wrap(setup, seed))",
    BddXml(t, "<TIn, TOut>", "Asynchronous factory that observes a <see cref=\"CancellationToken\"/>.", "TOut", SeedDoc.SeedPassed)
  );

  // Task<TIn> seed: token-aware → ValueTask<TOut>
  yield return new(
    BddGen_Summary(t, "an async factory (ValueTask) that accepts Task-based seed and observes a CancellationToken", true),
    "ScenarioChain<TOut>", "Given", "<TIn, TOut>",
    BddGenParams(t, ("Func<Task<TIn>, CancellationToken, ValueTask<TOut>>","setup"), ("Task<TIn>","seed")),
    $"Seed(ctx, {BddGen_TitleExpr(t, "TOut")}, Wrap(setup, seed))",
    BddXml(t, "<TIn, TOut>", "ValueTask-producing factory that observes a <see cref=\"CancellationToken\"/>.", "TOut", SeedDoc.SeedPassed)
  );

  // Task<T> seed: token-aware side-effects that keep T (ValueTask)
  yield return new(
    BddGen_Summary(t, "an async side-effect (ValueTask) that accepts Task-based seed and observes a CancellationToken", true),
    "ScenarioChain<T>", "Given", "<T>",
    BddGenParams(t, ("Func<Task<T>, CancellationToken, ValueTask>","setup"), ("Task<T>","seed")),
    $"Seed(ctx, {BddGen_TitleExpr(t, "T")}, Wrap(setup, seed))",
    BddXml(t, "<T>", "Asynchronous side-effect that observes a <see cref=\"CancellationToken\"/>.", "T", SeedDoc.CarriedAfter)
  );

  // Task<T> seed: token-aware side-effects that keep T (Task)
  yield return new(
    BddGen_Summary(t, "an async side-effect (Task) that accepts Task-based seed and observes a CancellationToken", true),
    "ScenarioChain<T>", "Given", "<T>",
    BddGenParams(t, ("Func<Task<T>, CancellationToken, Task>","setup"), ("Task<T>","seed")),
    $"Seed(ctx, {BddGen_TitleExpr(t, "T")}, Wrap(setup, seed))",
    BddXml(t, "<T>", "Asynchronous side-effect that observes a <see cref=\"CancellationToken\"/>.", "T", SeedDoc.CarriedAfter)
  );
}

IEnumerable<BddGenMethod> BddGen_SeedValueTask(BddGenTitle t)
{
  // ValueTask<TIn> seed: Func<ValueTask<TIn>, TOut>
  yield return new(
    BddGen_Summary(t, "a synchronous factory that accepts ValueTask-based seed", false),
    "ScenarioChain<TOut>", "Given", "<TIn, TOut>",
    BddGenParams(t, ("Func<ValueTask<TIn>, TOut>","setup"), ("ValueTask<TIn>","seed")),
    $"Seed(ctx, {BddGen_TitleExpr(t, "TOut")}, Wrap(setup, seed))",
    BddXml(t, "<TIn, TOut>", "Factory that accepts a <see cref=\"ValueTask{TIn}\"/>.", "TOut", SeedDoc.SeedPassed)
  );

  // ValueTask<TIn> seed: token-aware → TOut
  yield return new(
    BddGen_Summary(t, "a synchronous factory that accepts ValueTask-based seed and observes a CancellationToken", true),
    "ScenarioChain<TOut>", "Given", "<TIn, TOut>",
    BddGenParams(t, ("Func<ValueTask<TIn>, CancellationToken, TOut>","setup"), ("ValueTask<TIn>","seed")),
    $"Seed(ctx, {BddGen_TitleExpr(t, "TOut")}, Wrap(setup, seed))",
    BddXml(t, "<TIn, TOut>", "Factory that observes a <see cref=\"CancellationToken\"/>.", "TOut", SeedDoc.SeedPassed)
  );

  // ValueTask<TIn> seed: token-aware → ValueTask<TOut>
  yield return new(
    BddGen_Summary(t, "an async factory (ValueTask) that accepts ValueTask-based seed and observes a CancellationToken", true),
    "ScenarioChain<TOut>", "Given", "<TIn, TOut>",
    BddGenParams(t, ("Func<ValueTask<TIn>, CancellationToken, ValueTask<TOut>>","setup"), ("ValueTask<TIn>","seed")),
    // Inline unless you add Wrap<TIn,TOut>(Func<ValueTask<TIn>,CT,ValueTask<TOut>>, ValueTask<TIn>)
    $"Seed(ctx, {BddGen_TitleExpr(t, "TOut")}, ct => setup(seed, ct))",
    BddXml(t, "<TIn, TOut>", "ValueTask-producing factory that observes a <see cref=\"CancellationToken\"/>.", "TOut", SeedDoc.SeedPassed)
  );
}

// --- main fold-in emitter ----------------------------------------------------

void BddGen_Emit(string projectDir)
{
  var all =
      Enumerable.Empty<BddGenMethod>()
      .Concat(BddGen_NoSeed(BddGenTitle.Explicit))
      .Concat(BddGen_NoSeed(BddGenTitle.Auto))
      .Concat(BddGen_SeedValue(BddGenTitle.Explicit))
      .Concat(BddGen_SeedValue(BddGenTitle.Auto))
      .Concat(BddGen_SeedTask(BddGenTitle.Explicit))
      .Concat(BddGen_SeedTask(BddGenTitle.Auto))
      .Concat(BddGen_SeedValueTask(BddGenTitle.Explicit))
      .Concat(BddGen_SeedValueTask(BddGenTitle.Auto));

  var content =
$@"// <auto-generated/>
using System.Threading;
using System.Threading.Tasks;

namespace TinyBDD;

/// <summary>
/// Generated Given(...) overloads for Bdd that normalize factories/seeds to
/// <c>Func&lt;CancellationToken, ValueTask&lt;T&gt;&gt;</c> and call <c>ScenarioChain.Seed</c>.
/// </summary>
public static partial class Bdd
{{
{string.Concat(all.Select(BddGen_Render))}}}
";

  var path = Path.Combine(projectDir, "Generated", "Bdd.Given.g.cs");
  File.WriteAllText(path, content);
  Console.WriteLine($@"Wrote {path}");
}


// ===== ThenChain generation (And/But assertions) =============================

// Summary text specialized for ThenChain's And/But assertion steps
string TC_Summary(string wordName, Kind kind, Title title, Async @async, Token token)
{
    var ttl = title == Title.Explicit ? "with an explicit title" : "with a default title";
    return kind switch
    {
        Kind.Predicate => $"Adds an <c>{wordName}</c> boolean assertion {ttl}{(token==Token.Token ? " observing a token." : "")}",
        Kind.Assert    => $"Adds an <c>{wordName}</c> assertion {ttl}{(token==Token.Token ? " observing a token." : "")}",
        _ => ""
    };
}

MethodSpec TC_MakeAndOrButPredicate(string wordName, string wordEnum, Title title, Async @async, Token token, string sig)
{
    var wrapTitle = title == Title.Explicit ? "title" : $"nameof({wordName})";
    return new(
        TC_Summary(wordName, Kind.Predicate, title, @async, token),
        XmlExtras(Kind.Predicate, title),
        "ThenChain<T>",
        wordName,
        "",
        Params(title, sig, Kind.Predicate, explicitName: "predicate"),
        // Step title metadata uses TitleArg(title); predicate's failure label uses wrapTitle
        $"Add({wordEnum}, {TitleArg(title)}, Wrap({wrapTitle}, predicate))"
    );
}

MethodSpec TC_MakeAndOrButAssert(string wordName, string wordEnum, Title title, Async @async, Token token, string sig)
{
    return new(
        TC_Summary(wordName, Kind.Assert, title, @async, token),
        XmlExtras(Kind.Assert, title),
        "ThenChain<T>",
        wordName,
        "",
        Params(title, sig, Kind.Assert, explicitName: "assertion"),
        $"Add({wordEnum}, {TitleArg(title)}, Wrap(assertion))"
    );
}

// No-value assertion (Func<Task>) — matches your manual surface
MethodSpec TC_MakeAndOrButAssertNoValue(string wordName, string wordEnum, Title title)
{
    return new(
        $"Adds a <c>{wordName}</c> assertion {(title==Title.Explicit? "with an explicit title":"with a default title")} (no value parameter).",
        XmlExtras(Kind.Assert, title, noValue:true),
        "ThenChain<T>",
        wordName,
        "",
        Params(title, "Func<Task>", Kind.Assert, explicitName: "assertion"),
        $"Add({wordEnum}, {TitleArg(title)}, Wrap(assertion))"
    );
}

// Synchronous Action<T> assertion — also part of your manual surface
MethodSpec TC_MakeAndOrButAssertAction(string wordName, string wordEnum, Title title)
{
    return new(
        $"Adds a <c>{wordName}</c> assertion {(title==Title.Explicit? "with an explicit title":"with a default title")} using a synchronous action.",
        XmlExtras(Kind.Assert, title),
        "ThenChain<T>",
        wordName,
        "",
        Params(title, "Action<T>", Kind.Assert, explicitName: "assertion"),
        $"Add({wordEnum}, {TitleArg(title)}, Wrap(assertion))"
    );
}

void Emit_ThenChain_AndBut()
{
    string Build(string wordName, string wordEnum)
    {
        // Predicates: sync, Task<bool>, ValueTask<bool>, token-aware Task/ValueTask
        var preds =
            Titles
                .SelectMany(t => Asyncs, (title, @async) => new { title, @async })
                .SelectMany(k => Tokens, (k, token) =>
                {
                    var ok = TrySig(Step.Then, Kind.Predicate, k.@async, token, out var sig);
                    return new { k.title, k.@async, token, ok, sig };
                })
                .Where(x => x.ok)
                .Select(x => TC_MakeAndOrButPredicate(wordName, wordEnum, x.title, x.@async, x.token, x.sig));

        // Assertions with value: Task/ValueTask (+ token-aware)
        var assertsWithValue =
            Titles
                .SelectMany(t => new[] { Async.Task, Async.ValueTask }, (title, @async) => new { title, @async })
                .SelectMany(k => Tokens, (k, token) =>
                {
                    var ok = TrySig(Step.Then, Kind.Assert, k.@async, token, out var sig);
                    return new { k.title, k.@async, token, ok, sig };
                })
                .Where(x => x.ok)
                .Select(x => TC_MakeAndOrButAssert(wordName, wordEnum, x.title, x.@async, x.token, x.sig));

        // No-value assertion (Func<Task>) — explicit + auto
        var assertsNoValue =
            Titles.Select(title => TC_MakeAndOrButAssertNoValue(wordName, wordEnum, title));

        // Synchronous Action<T> assertion — explicit + auto
        var actionSync =
            Titles.Select(title => TC_MakeAndOrButAssertAction(wordName, wordEnum, title));

        return string.Concat(preds.Select(Render))
             + string.Concat(assertsWithValue.Select(Render))
             + string.Concat(assertsNoValue.Select(Render))
             + string.Concat(actionSync.Select(Render));
    }

    var file =
        "// <auto-generated/>\nnamespace TinyBDD;\npublic readonly partial struct ThenChain<T>\n{\n" +
        Build("And", "StepWord.And") +
        Build("But", "StepWord.But") +
        "}\n";

    File.WriteAllText(Path.Combine(outDir, "ThenChain.AndBut.g.cs"), file);
}

// ==================== /BDD.Given generator ====================

// ===== Run all =====
Emit_When();
Emit_AndBut();
Emit_Then();
Emit_ThenChain_AndBut(); 
BddGen_Emit(projectDir);
Console.WriteLine($"Wrote {outDir}");
