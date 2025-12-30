---
_layout: landing
---

# TinyBDD — Fluent BDD for .NET, zero ceremony

<section class="landing-hero">
  <div class="hero-copy">
    <p class="lead">Write expressive Given/When/Then tests that feel great and run anywhere (xUnit, NUnit, MSTest) without framework lock-in.</p>
    <div class="hero-actions">
      <a class="btn btn-primary" href="/user-guide/getting-started.md">Get started</a>
      <a class="btn btn-secondary" href="/user-guide/index.md">Read the guide</a>
    </div>
    <div class="hero-badges">
      <span class="hero-badge">xUnit</span>
      <span class="hero-badge">NUnit</span>
      <span class="hero-badge">MSTest</span>
      <span class="hero-badge">Async-first</span>
      <span class="hero-badge">Gherkin output</span>
    </div>
  </div>
  <div class="hero-media">
    <img src="images/tinyBDD.png" alt="TinyBDD" width="160" />
  </div>
</section>

<section class="landing-section">
  <h2>Why TinyBDD</h2>
  <div class="feature-grid">
    <div class="feature-card">
      <h3>Tiny, readable core</h3>
      <p>Understand the engine in minutes and keep it close to your tests.</p>
    </div>
    <div class="feature-card">
      <h3>Fluent, async-first</h3>
      <p>Given/When/Then chains with async built in for modern test flows.</p>
    </div>
    <div class="feature-card">
      <h3>Deferred expectations</h3>
      <p>Compose reasons and hints, throw only when awaited.</p>
    </div>
    <div class="feature-card">
      <h3>Step IO lineage</h3>
      <p>Track inputs, outputs, and the current item across steps.</p>
    </div>
    <div class="feature-card">
      <h3>Assertion agnostic</h3>
      <p>Works with any assertion library you already use.</p>
    </div>
    <div class="feature-card">
      <h3>Adapters included</h3>
      <p>Optional adapters for xUnit, NUnit, and MSTest.</p>
    </div>
    <div class="feature-card">
      <h3>Gherkin reporting</h3>
      <p>Readable Given/When/Then output right in your test results.</p>
    </div>
  </div>
</section>

<section class="landing-section">
  <h2>Try it in 30 seconds</h2>
  <p>Ambient (with adapter base class or Ambient.Current set):</p>
  ```csharp
  await Given(() => 1)
       .When("double", x => x * 2)
       .Then("== 2", v => v == 2)
       .AssertPassed();
  ```

  <p>Explicit (no base class required):</p>
  ```csharp
  var ctx = Bdd.CreateContext(this);
  await Bdd.Given(ctx, "numbers", () => new[]{1,2,3})
           .When("sum", (arr, _) => Task.FromResult(arr.Sum()))
           .Then("> 0", sum => sum > 0)
           .AssertPassed();
  ```
</section>

<section class="landing-section">
  <h2>Documentation Map</h2>
  <div class="doc-grid">
    <a class="doc-card" href="/user-guide/index.html">
      <span class="doc-card-title">Introduction</span>
      <span class="doc-card-desc">What TinyBDD is and when to use it.</span>
    </a>
    <a class="doc-card" href="/user-guide/getting-started.html">
      <span class="doc-card-title">Getting Started</span>
      <span class="doc-card-desc">Install, set up, and run your first scenario.</span>
    </a>
    <a class="doc-card" href="/user-guide/bdd-fundamentals.html">
      <span class="doc-card-title">BDD Fundamentals</span>
      <span class="doc-card-desc">Core ideas and Gherkin-style thinking.</span>
    </a>
    <a class="doc-card" href="/user-guide/tdd-via-bdd.html">
      <span class="doc-card-title">BDD + TDD Workflow</span>
      <span class="doc-card-desc">Practical flow from failing to passing.</span>
    </a>
    <a class="doc-card" href="/user-guide/writing-scenarios.html">
      <span class="doc-card-title">Writing Scenarios</span>
      <span class="doc-card-desc">Crafting readable Given/When/Then steps.</span>
    </a>
    <a class="doc-card" href="/user-guide/data-and-tables.html">
      <span class="doc-card-title">Data and Tables</span>
      <span class="doc-card-desc">Drive tests with rich inputs and examples.</span>
    </a>
    <a class="doc-card" href="/user-guide/assertions-and-expectations.html">
      <span class="doc-card-title">Expectations</span>
      <span class="doc-card-desc">Fluent assertions, reasons, and hints.</span>
    </a>
    <a class="doc-card" href="/user-guide/step-io-and-state.html">
      <span class="doc-card-title">Step IO and State</span>
      <span class="doc-card-desc">Track state and lineage across steps.</span>
    </a>
    <a class="doc-card" href="/user-guide/hooks-and-lifecycle.html">
      <span class="doc-card-title">Hooks and Lifecycle</span>
      <span class="doc-card-desc">Set up and tear down scenarios cleanly.</span>
    </a>
    <a class="doc-card" href="/user-guide/running-with-test-frameworks.html">
      <span class="doc-card-title">Test Frameworks</span>
      <span class="doc-card-desc">Adapters for xUnit, NUnit, and MSTest.</span>
    </a>
    <a class="doc-card" href="/user-guide/reporting.html">
      <span class="doc-card-title">Reporting</span>
      <span class="doc-card-desc">Readable output for your test runs.</span>
    </a>
    <a class="doc-card" href="/user-guide/tips-and-tricks.html">
      <span class="doc-card-title">Tips and Tricks</span>
      <span class="doc-card-desc">Patterns and shortcuts for day-to-day use.</span>
    </a>
    <a class="doc-card" href="/user-guide/advanced-usage.html">
      <span class="doc-card-title">Advanced Usage</span>
      <span class="doc-card-desc">Extensions, customizations, and power moves.</span>
    </a>
    <a class="doc-card" href="/user-guide/troubleshooting-faq.html">
      <span class="doc-card-title">Troubleshooting</span>
      <span class="doc-card-desc">Common issues and their fixes.</span>
    </a>
    <a class="doc-card" href="/user-guide/samples-index.html">
      <span class="doc-card-title">Samples Index</span>
      <span class="doc-card-desc">Working examples you can copy.</span>
    </a>
  </div>

  <p class="landing-tip">Tip: add [Feature], [Scenario], and [Tag] to make reports shine. Base classes emit Gherkin output automatically.</p>
</section>
