// namespace TinyBDD;
//
// /// <summary>
// /// Provides extension methods for fluent BDD step chaining with async and sync overloads for Given, When, Then, And, and But steps.
// /// </summary>
// public static class BddFluentTaskExtensions
// {
//     public static async Task<WhenBuilder<TGiven, TNewOut>> And<TGiven, TOut, TNewOut>(
//         this Task<WhenBuilder<TGiven, TOut>> whenTask,
//         string title,
//         Func<TOut, CancellationToken, Task<TNewOut>> transform,
//         string stepName = nameof(And))
//     {
//         var when = await whenTask.ConfigureAwait(false);
//
//         return new WhenBuilder<TGiven, TNewOut>(
//             when.Ctx,
//             when.Given,
//             title,
//             async (given, ct) =>
//             {
//                 var result = await when.Fn(given, ct).ConfigureAwait(false);
//                 return await Bdd.RunStepAsync(when.Ctx, stepName, title,
//                     () => transform(result, ct)).ConfigureAwait(false);
//             });
//     }
//     
//     public static Task<WhenBuilder<TGiven, TNewOut>> But<TGiven, TOut, TNewOut>(
//         this Task<WhenBuilder<TGiven, TOut>> whenTask,
//         string title,
//         Func<TOut, CancellationToken, Task<TNewOut>> transform,
//         string stepName = nameof(And)) => 
//         And(whenTask, title, transform, stepName);
//     
//     
//     public static async Task<WhenBuilder<TGiven, TNewOut?>> And<TGiven, TOut, TNewOut>(
//         this Task<WhenBuilder<TGiven, TOut>> whenTask,
//         string title,
//         Func<TOut, TNewOut> transform,
//         string stepName = nameof(And))
//     {
//         var when = await whenTask.ConfigureAwait(false);
//
//         return new WhenBuilder<TGiven, TNewOut?>(
//             when.Ctx,
//             when.Given,
//             title,
//             async (given, ct) =>
//             {
//                 var result = await when.Fn(given, ct).ConfigureAwait(false);
//                 return Bdd.RunStep(when.Ctx, stepName, title, () => transform(result));
//             });
//     }
//     
//     
//     public static Task<WhenBuilder<TGiven, TNewOut?>> But<TGiven, TOut, TNewOut>(
//         this Task<WhenBuilder<TGiven, TOut>> whenTask,
//         string title,
//         Func<TOut, TNewOut> transform,
//         string stepName = nameof(But)) => 
//         And(whenTask, title, transform, stepName);
//
//     /// <summary>
//     /// Adds a <c>When</c> step to the current <c>Given</c> chain using a synchronous transformation without a cancellation token.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <typeparam name="TOut">The type produced by this <c>When</c> step.</typeparam>
//     /// <param name="given">The <see cref="GivenBuilder{TGiven}"/> to extend.</param>
//     /// <param name="title">The display title for this <c>When</c> step in the scenario output.</param>
//     /// <param name="transform">A synchronous transformation that receives the <typeparamref name="TGiven"/> value and returns a <typeparamref name="TOut"/>.</param>
//     /// <returns>A <see cref="Task{TResult}"/> that resolves to a <see cref="WhenBuilder{TGiven, TOut}"/> for further chaining.</returns>
//     public static Task<WhenBuilder<TGiven, TOut>> When<TGiven, TOut>(
//         this GivenBuilder<TGiven> given,
//         string title,
//         Func<TGiven, TOut> transform)
//     {
//         var value = Bdd.RunStep(given.Ctx, "Given", given.Title,
//             () => given.Fn(CancellationToken.None));
//     }
//
//     /// <summary>
//     /// Adds a <c>When</c> step with a default title using a synchronous transformation without a cancellation token.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <typeparam name="TOut">The type produced by this <c>When</c> step.</typeparam>
//     /// <param name="given">The <see cref="GivenBuilder{TGiven}"/> to extend.</param>
//     /// <param name="transform">A synchronous transformation that receives the <typeparamref name="TGiven"/> value.</param>
//     /// <returns>A <see cref="Task{TResult}"/> that resolves to a <see cref="WhenBuilder{TGiven, TOut}"/> for further chaining.</returns>
//     public static Task<WhenBuilder<TGiven, TOut>> When<TGiven, TOut>(
//         this GivenBuilder<TGiven> given,
//         Func<TGiven, TOut> transform)
//         => given.When("When action", transform);
//
//
//     /// <summary>
//     /// Adds a <c>When</c> step to the current <c>Given</c> chain using an asynchronous action that receives the given value and a cancellation token.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <param name="given">The <see cref="GivenBuilder{TGiven}"/> to extend.</param>
//     /// <param name="title">The display title for this <c>When</c> step in the scenario output.</param>
//     /// <param name="actionAsync">An asynchronous action that receives the <typeparamref name="TGiven"/> value and a <see cref="CancellationToken"/>.</param>
//     /// <returns>A <see cref="Task{TResult}"/> that resolves to a <see cref="WhenBuilder{TGiven}"/> for further chaining.</returns>
//     public static async Task<WhenBuilder<TGiven>> When<TGiven>(
//         this GivenBuilder<TGiven> given,
//         string title,
//         Func<TGiven, CancellationToken, Task> actionAsync)
//     {
//         // Execute Given now (still lazy until outer await reaches here)
//         var value = await Bdd.RunStepAsync(given.Ctx, "Given", given.Title,
//             () => given.Fn(CancellationToken.None)).ConfigureAwait(false);
//         return new WhenBuilder<TGiven>(given.Ctx, value, title, actionAsync);
//     }
//     
//     
//     // public static async Task<WhenBuilder<TGiven>> When<TGiven, TOut>(
//     //     this GivenBuilder<TGiven> given,
//     //     string title,
//     //     Func<TGiven, CancellationToken, Task<TOut>> predicate)
//     // {
//     //     // Execute Given now (still lazy until outer await reaches here)
//     //     var value = await Bdd.RunStepAsync(given.Ctx, "Given", given.Title,
//     //         () => given.Fn(CancellationToken.None)).ConfigureAwait(false);
//     //     return new WhenBuilder<TGiven>(given.Ctx, value, title, actionAsync);
//     // }
//
//     /// <summary>
//     /// Adds a <c>When</c> step to the current <c>Given</c> chain using an asynchronous action that receives the given value.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <param name="given">The <see cref="GivenBuilder{TGiven}"/> to extend.</param>
//     /// <param name="title">The display title for this <c>When</c> step in the scenario output.</param>
//     /// <param name="actionAsync">An asynchronous action that receives the <typeparamref name="TGiven"/> value.</param>
//     /// <returns>A <see cref="Task{TResult}"/> that resolves to a <see cref="WhenBuilder{TGiven}"/> for further chaining.</returns>
//     public static Task<WhenBuilder<TGiven>> When<TGiven>(
//         this GivenBuilder<TGiven> given,
//         string title,
//         Func<TGiven, Task> actionAsync)
//         => given.When(title, (g, _) => actionAsync(g));
//
//     /// <summary>
//     /// Adds a <c>When</c> step with a default title using an asynchronous action that receives the given value and a cancellation token.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <param name="given">The <see cref="GivenBuilder{TGiven}"/> to extend.</param>
//     /// <param name="actionAsync">An asynchronous action that receives the <typeparamref name="TGiven"/> value and a <see cref="CancellationToken"/>.</param>
//     /// <returns>A <see cref="Task{TResult}"/> that resolves to a <see cref="WhenBuilder{TGiven}"/> for further chaining.</returns>
//     public static Task<WhenBuilder<TGiven>> When<TGiven>(
//         this GivenBuilder<TGiven> given,
//         Func<TGiven, CancellationToken, Task> actionAsync)
//         => given.When("When action", actionAsync);
//
//     /// <summary>
//     /// Adds a <c>When</c> step with a default title using an asynchronous action that receives the given value.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <param name="given">The <see cref="GivenBuilder{TGiven}"/> to extend.</param>
//     /// <param name="actionAsync">An asynchronous action that receives the <typeparamref name="TGiven"/> value.</param>
//     /// <returns>A <see cref="Task{TResult}"/> that resolves to a <see cref="WhenBuilder{TGiven}"/> for further chaining.</returns>
//     public static Task<WhenBuilder<TGiven>> When<TGiven>(
//         this GivenBuilder<TGiven> given,
//         Func<TGiven, Task> actionAsync)
//         => given.When("When action", actionAsync);
//
//     /// <summary>
//     /// Defines an asynchronous <c>When</c> step that transforms the value produced by the preceding
//     /// <c>Given</c> step into a new result of type <typeparamref name="TOut"/>.
//     /// </summary>
//     /// <typeparam name="TGiven">The type of the value produced by the preceding <c>Given</c> step.</typeparam>
//     /// <typeparam name="TOut">The type of the result produced by this <c>When</c> step.</typeparam>
//     /// <param name="given">The preceding <c>Given</c> builder.</param>
//     /// <param name="title">A human-readable description of the <c>When</c> step.</param>
//     /// <param name="actionAsync">
//     /// An asynchronous delegate that takes the <typeparamref name="TGiven"/> value and a <see cref="CancellationToken"/>,
//     /// performs the <c>When</c> action, and returns the resulting <typeparamref name="TOut"/>.
//     /// </param>
//     /// <returns>A task producing a <see cref="WhenBuilder{TGiven, TOut}"/> for chaining subsequent steps.</returns>
//     public static async Task<WhenBuilder<TGiven, TOut>> When<TGiven, TOut>(
//         this GivenBuilder<TGiven> given,
//         string title,
//         Func<TGiven, CancellationToken, Task<TOut>> actionAsync)
//     {
//         var value = await Bdd.RunStepAsync(given.Ctx, "Given", given.Title,
//             () => given.Fn(CancellationToken.None)).ConfigureAwait(false);
//         return new WhenBuilder<TGiven, TOut>(given.Ctx, value, title, actionAsync);
//     }
//
//     /// <summary>
//     /// Defines an asynchronous <c>When</c> step that transforms the value produced by the preceding
//     /// <c>Given</c> step into a new result of type <typeparamref name="TOut"/>.
//     /// </summary>
//     /// <remarks>
//     /// This overload is a shorthand for the token-aware version, using <see cref="CancellationToken.None"/>.
//     /// </remarks>
//     /// <typeparam name="TGiven">The type of the value produced by the preceding <c>Given</c> step.</typeparam>
//     /// <typeparam name="TOut">The type of the result produced by this <c>When</c> step.</typeparam>
//     /// <param name="given">The preceding <c>Given</c> builder.</param>
//     /// <param name="title">A human-readable description of the <c>When</c> step.</param>
//     /// <param name="actionAsync">
//     /// An asynchronous delegate that takes the <typeparamref name="TGiven"/> value,
//     /// performs the <c>When</c> action, and returns the resulting <typeparamref name="TOut"/>.
//     /// </param>
//     /// <returns>A task producing a <see cref="WhenBuilder{TGiven, TOut}"/> for chaining subsequent steps.</returns>
//     public static Task<WhenBuilder<TGiven, TOut>> When<TGiven, TOut>(
//         this GivenBuilder<TGiven> given,
//         string title,
//         Func<TGiven, Task<TOut>> actionAsync)
//         => given.When<TGiven, TOut>(title, (g, _) => actionAsync(g));
//
//     /// <summary>
//     /// Defines an asynchronous <c>When</c> step with a default title that transforms the value
//     /// produced by the preceding <c>Given</c> step into a new result of type <typeparamref name="TOut"/>.
//     /// </summary>
//     /// <typeparam name="TGiven">The type of the value produced by the preceding <c>Given</c> step.</typeparam>
//     /// <typeparam name="TOut">The type of the result produced by this <c>When</c> step.</typeparam>
//     /// <param name="given">The preceding <c>Given</c> builder.</param>
//     /// <param name="actionAsync">
//     /// An asynchronous delegate that takes the <typeparamref name="TGiven"/> value and a <see cref="CancellationToken"/>,
//     /// performs the <c>When</c> action, and returns the resulting <typeparamref name="TOut"/>.
//     /// </param>
//     /// <returns>A task producing a <see cref="WhenBuilder{TGiven, TOut}"/> for chaining subsequent steps.</returns>
//     public static Task<WhenBuilder<TGiven, TOut>> When<TGiven, TOut>(
//         this GivenBuilder<TGiven> given,
//         Func<TGiven, CancellationToken, Task<TOut>> actionAsync)
//         => given.When("When action", actionAsync);
//
//     /// <summary>
//     /// Defines an asynchronous <c>When</c> step with a default title that transforms the value
//     /// produced by the preceding <c>Given</c> step into a new result of type <typeparamref name="TOut"/>.
//     /// </summary>
//     /// <remarks>
//     /// This overload is a shorthand for the token-aware version, using <see cref="CancellationToken.None"/>.
//     /// </remarks>
//     /// <typeparam name="TGiven">The type of the value produced by the preceding <c>Given</c> step.</typeparam>
//     /// <typeparam name="TOut">The type of the result produced by this <c>When</c> step.</typeparam>
//     /// <param name="given">The preceding <c>Given</c> builder.</param>
//     /// <param name="actionAsync">
//     /// An asynchronous delegate that takes the <typeparamref name="TGiven"/> value,
//     /// performs the <c>When</c> action, and returns the resulting <typeparamref name="TOut"/>.
//     /// </param>
//     /// <returns>A task producing a <see cref="WhenBuilder{TGiven, TOut}"/> for chaining subsequent steps.</returns>
//     public static Task<WhenBuilder<TGiven, TOut>> When<TGiven, TOut>(
//         this GivenBuilder<TGiven> given,
//         Func<TGiven, Task<TOut>> actionAsync)
//         => given.When("When action", actionAsync);
//
//     /// <summary>
//     /// Adds a <c>When</c> step to the current <c>Given</c> chain using a synchronous side effect action that receives the given value and a cancellation token.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <param name="given">The <see cref="GivenBuilder{TGiven}"/> to extend.</param>
//     /// <param name="title">The display title for this <c>When</c> step in the scenario output.</param>
//     /// <param name="action">A synchronous action that receives the <typeparamref name="TGiven"/> value and a <see cref="CancellationToken"/>.</param>
//     /// <returns>A <see cref="Task{TResult}"/> that resolves to a <see cref="WhenBuilder{TGiven, TGiven}"/> for further chaining.</returns>
//     public static Task<WhenBuilder<TGiven, TGiven>> When<TGiven>(
//         this GivenBuilder<TGiven> given,
//         string title,
//         Action<TGiven, CancellationToken> action)
//         => given.When(title, (g, ct) =>
//         {
//             action(g, ct);
//             return Task.FromResult(g);
//         });
//
//     /// <summary>
//     /// Adds a <c>When</c> step to the current <c>Given</c> chain using a synchronous side-effect action that receives the given value.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <param name="given">The <see cref="GivenBuilder{TGiven}"/> to extend.</param>
//     /// <param name="title">The display title for this <c>When</c> step in the scenario output.</param>
//     /// <param name="action">A synchronous action that receives the <typeparamref name="TGiven"/> value.</param>
//     /// <returns>A <see cref="Task{TResult}"/> that resolves to a <see cref="WhenBuilder{TGiven, TGiven}"/> for further chaining.</returns>
//     public static Task<WhenBuilder<TGiven, TGiven>> When<TGiven>(
//         this GivenBuilder<TGiven> given,
//         string title,
//         Action<TGiven> action)
//         => given.When(title, (g, _) =>
//         {
//             action(g);
//             return Task.FromResult(g);
//         });
//
//     /// <summary>
//     /// Adds a <c>When</c> step with a default title using a synchronous side-effect action that receives the given value and a cancellation token.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <param name="given">The <see cref="GivenBuilder{TGiven}"/> to extend.</param>
//     /// <param name="action">A synchronous action that receives the <typeparamref name="TGiven"/> value and a <see cref="CancellationToken"/>.</param>
//     /// <returns>A <see cref="Task{TResult}"/> that resolves to a <see cref="WhenBuilder{TGiven, TGiven}"/> for further chaining.</returns>
//     public static Task<WhenBuilder<TGiven, TGiven>> When<TGiven>(
//         this GivenBuilder<TGiven> given,
//         Action<TGiven, CancellationToken> action)
//         => given.When("When action", action);
//
//     /// <summary>
//     /// Adds a <c>When</c> step with a default title using a synchronous side-effect action that receives the given value.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <param name="given">The <see cref="GivenBuilder{TGiven}"/> to extend.</param>
//     /// <param name="action">A synchronous action that receives the <typeparamref name="TGiven"/> value.</param>
//     /// <returns>A <see cref="Task{TResult}"/> that resolves to a <see cref="WhenBuilder{TGiven, TGiven}"/> for further chaining.</returns>
//     public static Task<WhenBuilder<TGiven, TGiven>> When<TGiven>(
//         this GivenBuilder<TGiven> given,
//         Action<TGiven> action)
//         => given.When("When action", action);
//
//
//     /// <summary>
//     /// Adds a <c>Then</c> step directly after a <c>Given</c> step using an asynchronous assertion that receives the value and a cancellation token.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the <c>Given</c> step.</typeparam>
//     /// <param name="given">The <see cref="GivenBuilder{TGiven}"/> to extend.</param>
//     /// <param name="title">The display title for this <c>Then</c> step.</param>
//     /// <param name="assertion">An asynchronous assertion that receives the <typeparamref name="TGiven"/> value and a <see cref="CancellationToken"/>.</param>
//     /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder{TGiven}"/> for chaining more assertions.</returns>
//     public static async Task<ThenBuilder<TGiven>> Then<TGiven>(
//         this GivenBuilder<TGiven> given,
//         string title,
//         Func<TGiven, CancellationToken, Task> assertion)
//     {
//         var value = await Bdd.RunStepAsync(given.Ctx, "Given", given.Title,
//             () => given.Fn(CancellationToken.None)).ConfigureAwait(false);
//
//         await Bdd.RunStepAsync(given.Ctx, "Then", title,
//             () => assertion(value, CancellationToken.None)).ConfigureAwait(false);
//
//         return new ThenBuilder<TGiven>(given.Ctx, value);
//     }
//
//     /// <summary>
//     /// Adds a <c>Then</c> step directly after a <c>Given</c> step using an asynchronous assertion that receives the value.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the <c>Given</c> step.</typeparam>
//     /// <param name="given">The <see cref="GivenBuilder{TGiven}"/> to extend.</param>
//     /// <param name="title">The display title for this <c>Then</c> step.</param>
//     /// <param name="assertion">An asynchronous assertion that receives the <typeparamref name="TGiven"/> value.</param>
//     /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder{TGiven}"/> for further chaining.</returns>
//     public static Task<ThenBuilder<TGiven>> Then<TGiven>(
//         this GivenBuilder<TGiven> given,
//         string title,
//         Func<TGiven, Task> assertion)
//         => given.Then(title, (v, _) => assertion(v));
//
//     /// <summary>
//     /// Adds a <c>Then</c> step with a default title directly after a <c>Given</c> step using an asynchronous assertion that receives the value and a cancellation token.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the <c>Given</c> step.</typeparam>
//     /// <param name="given">The <see cref="GivenBuilder{TGiven}"/> to extend.</param>
//     /// <param name="assertion">An asynchronous assertion that receives the <typeparamref name="TGiven"/> value and a <see cref="CancellationToken"/>.</param>
//     /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder{TGiven}"/> for further chaining.</returns>
//     public static Task<ThenBuilder<TGiven>> Then<TGiven>(
//         this GivenBuilder<TGiven> given,
//         Func<TGiven, CancellationToken, Task> assertion)
//         => given.Then("Then assertion", assertion);
//
//     /// <summary>
//     /// Adds a <c>Then</c> step with a default title directly after a <c>Given</c> step using an asynchronous assertion that receives the value.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the <c>Given</c> step.</typeparam>
//     /// <param name="given">The <see cref="GivenBuilder{TGiven}"/> to extend.</param>
//     /// <param name="assertion">An asynchronous assertion that receives the <typeparamref name="TGiven"/> value.</param>
//     /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder{TGiven}"/> for further chaining.</returns>
//     public static Task<ThenBuilder<TGiven>> Then<TGiven>(
//         this GivenBuilder<TGiven> given,
//         Func<TGiven, Task> assertion)
//         => given.Then("Then assertion", assertion);
//
//     // ---------------------------
//     // Then(...) after When(...) (untyped)
//     // ---------------------------
//
//     /// <summary>
//     /// Adds a <c>Then</c> step after an untyped <c>When</c> step using an asynchronous assertion that receives a cancellation token.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <param name="whenTask">The task that resolves to the preceding <see cref="WhenBuilder{TGiven}"/>.</param>
//     /// <param name="title">The display title for this <c>Then</c> step.</param>
//     /// <param name="assertion">An asynchronous assertion that receives a <see cref="CancellationToken"/>.</param>
//     /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder"/> for chaining more assertions.</returns>
//     public static async Task<ThenBuilder> Then<TGiven>(
//         this Task<WhenBuilder<TGiven>> whenTask,
//         string title,
//         Func<CancellationToken, Task> assertion)
//     {
//         var when = await whenTask.ConfigureAwait(false);
//
//         // Execute When
//         await Bdd.RunStepAsync(when.Ctx, "When", when.Title,
//             () => when.Fn(when.Given, CancellationToken.None)).ConfigureAwait(false);
//
//         // Execute Then
//         await Bdd.RunStepAsync(when.Ctx, "Then", title,
//             () => assertion(CancellationToken.None)).ConfigureAwait(false);
//
//         return new ThenBuilder(when.Ctx);
//     }
//
//     /// <summary>
//     /// Adds a <c>Then</c> step after an untyped <c>When</c> step using an asynchronous assertion without a cancellation token.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <param name="whenTask">The task that resolves to the preceding <see cref="WhenBuilder{TGiven}"/>.</param>
//     /// <param name="title">The display title for this <c>Then</c> step.</param>
//     /// <param name="assertion">An asynchronous assertion delegate.</param>
//     /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder"/> for further chaining.</returns>
//     public static Task<ThenBuilder> Then<TGiven>(
//         this Task<WhenBuilder<TGiven>> whenTask,
//         string title,
//         Func<Task> assertion)
//         => whenTask.Then(title, _ => assertion());
//
//     /// <summary>
//     /// Adds a <c>Then</c> step with a default title after an untyped <c>When</c> step using an asynchronous assertion that receives a cancellation token.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <param name="whenTask">The task that resolves to the preceding <see cref="WhenBuilder{TGiven}"/>.</param>
//     /// <param name="assertion">An asynchronous assertion that receives a <see cref="CancellationToken"/>.</param>
//     /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder"/> for further chaining.</returns>
//     public static Task<ThenBuilder> Then<TGiven>(
//         this Task<WhenBuilder<TGiven>> whenTask,
//         Func<CancellationToken, Task> assertion)
//         => whenTask.Then("Then assertion", assertion);
//
//     /// <summary>
//     /// Adds a <c>Then</c> step with a default title after an untyped <c>When</c> step using an asynchronous assertion without a cancellation token.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <param name="whenTask">The task that resolves to the preceding <see cref="WhenBuilder{TGiven}"/>.</param>
//     /// <param name="assertion">An asynchronous assertion delegate.</param>
//     /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder"/> for further chaining.</returns>
//     public static Task<ThenBuilder> Then<TGiven>(
//         this Task<WhenBuilder<TGiven>> whenTask,
//         Func<Task> assertion)
//         => whenTask.Then("Then assertion", assertion);
//
//     // ---------------------------
//     // Then(...) after When(..., TOut) (typed)
//     // ---------------------------
//
//     /// <summary>
//     /// Adds a <c>Then</c> step after a typed <c>When</c> step using an asynchronous assertion that receives the <c>When</c> result and a cancellation token.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <typeparam name="TOut">The type produced by the preceding <c>When</c> step.</typeparam>
//     /// <param name="whenTask">The task that resolves to the preceding <see cref="WhenBuilder{TGiven, TOut}"/>.</param>
//     /// <param name="title">The display title for this <c>Then</c> step.</param>
//     /// <param name="assertion">An asynchronous assertion that receives the <typeparamref name="TOut"/> value and a <see cref="CancellationToken"/>.</param>
//     /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder{TOut}"/> for further chaining.</returns>
//     public static async Task<ThenBuilder<TOut>> Then<TGiven, TOut>(
//         this Task<WhenBuilder<TGiven, TOut>> whenTask,
//         string title,
//         Func<TOut, CancellationToken, Task> assertion)
//     {
//         var when = await whenTask.ConfigureAwait(false);
//
//         var result = await Bdd.RunStepAsync(when.Ctx, "When", when.Title,
//             () => when.Fn(when.Given, CancellationToken.None)).ConfigureAwait(false);
//
//         await Bdd.RunStepAsync(when.Ctx, "Then", title,
//             () => assertion(result, CancellationToken.None)).ConfigureAwait(false);
//
//         return new ThenBuilder<TOut>(when.Ctx, result);
//     }
//
//     public static async Task<ThenBuilder<TOut>> Then<TGiven, TInner, TOut>(
//         this Task<WhenBuilder<TGiven, TInner>> whenTask,
//         string title,
//         Func<TInner, CancellationToken, Task<TOut>> assertion,
//         CancellationToken cancellationToken = default)
//     {
//         var when = await whenTask.ConfigureAwait(false);
//         var result = await Bdd.RunStepAsync(when.Ctx, "When", when.Title,
//             () => when.Fn(when.Given, cancellationToken)).ConfigureAwait(false);
//         
//         var transformed = await Bdd.RunStepAsync(when.Ctx, "Then", title,
//             () => assertion(result, cancellationToken)).ConfigureAwait(false);
//
//         return new ThenBuilder<TOut>(when.Ctx, transformed);
//     }
//     
//     
//     public static async Task<ThenBuilder<TOut>> Then<TGiven, TInner, TOut>(
//         this Task<WhenBuilder<TGiven, TInner>> whenTask,
//         string title,
//         Func<TInner, TOut> assertion)
//     {
//         var when = await whenTask.ConfigureAwait(false);
//         var result = await Bdd.RunStepAsync(when.Ctx, "When", when.Title,
//             () => when.Fn(when.Given, CancellationToken.None)).ConfigureAwait(false);
//         
//         var transformed = assertion(result);
//
//         return new ThenBuilder<TOut>(when.Ctx, transformed);
//     }
//
//     /// <summary>
//     /// Adds a <c>Then</c> step after a typed <c>When</c> step using an asynchronous assertion without a cancellation token.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <typeparam name="TOut">The type produced by the preceding <c>When</c> step.</typeparam>
//     /// <param name="whenTask">The task that resolves to the preceding <see cref="WhenBuilder{TGiven, TOut}"/>.</param>
//     /// <param name="title">The display title for this <c>Then</c> step.</param>
//     /// <param name="assertion">An asynchronous assertion that receives the <typeparamref name="TOut"/> value.</param>
//     /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder{TOut}"/> for further chaining.</returns>
//     public static Task<ThenBuilder<TOut>> Then<TGiven, TOut>(
//         this Task<WhenBuilder<TGiven, TOut>> whenTask,
//         string title,
//         Func<TOut, Task> assertion)
//         => whenTask.Then(title, (v, _) => assertion(v));
//
//     /// <summary>
//     /// Adds a <c>Then</c> step with a default title after a typed <c>When</c> step using an asynchronous assertion that receives the <c>When</c> result and a cancellation token.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <typeparam name="TOut">The type produced by the preceding <c>When</c> step.</typeparam>
//     /// <param name="whenTask">The task that resolves to the preceding <see cref="WhenBuilder{TGiven, TOut}"/>.</param>
//     /// <param name="assertion">An asynchronous assertion that receives the <typeparamref name="TOut"/> value and a <see cref="CancellationToken"/>.</param>
//     /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder{TOut}"/> for further chaining.</returns>
//     public static Task<ThenBuilder<TOut>> Then<TGiven, TOut>(
//         this Task<WhenBuilder<TGiven, TOut>> whenTask,
//         Func<TOut, CancellationToken, Task> assertion)
//         => whenTask.Then("Then assertion", assertion);
//
//     /// <summary>
//     /// Adds a <c>Then</c> step with a default title after a typed <c>When</c> step using an asynchronous assertion without a cancellation token.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <typeparam name="TOut">The type produced by the preceding <c>When</c> step.</typeparam>
//     /// <param name="whenTask">The task that resolves to the preceding <see cref="WhenBuilder{TGiven, TOut}"/>.</param>
//     /// <param name="assertion">An asynchronous assertion that receives the <typeparamref name="TOut"/> value.</param>
//     /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder{TOut}"/> for further chaining.</returns>
//     public static Task<ThenBuilder<TOut>> Then<TGiven, TOut>(
//         this Task<WhenBuilder<TGiven, TOut>> whenTask,
//         Func<TOut, Task> assertion)
//         => whenTask.Then("Then assertion", assertion);
//
//     // ---------------------------
//     // And / But after Then (untyped)
//     // ---------------------------
//
//     /// <summary>
//     /// Adds an <c>And</c> step that executes an asynchronous assertion after a previous <c>Then</c> step.
//     /// </summary>
//     /// <param name="thenTask">The task that resolves to the preceding <see cref="ThenBuilder"/>.</param>
//     /// <param name="title">The display title for this step.</param>
//     /// <param name="assertion">An asynchronous assertion that receives a <see cref="CancellationToken"/>.</param>
//     /// <param name="stepName">The BDD keyword to display; defaults to <c>And</c>. Use <c>But</c> for the <see cref="But(Task{ThenBuilder}, string, Func{CancellationToken, Task})"/> overload.</param>
//     /// <returns>The same <see cref="ThenBuilder"/> for further chaining.</returns>
//     public static async Task<ThenBuilder> And(
//         this Task<ThenBuilder> thenTask,
//         string title,
//         Func<CancellationToken, Task> assertion,
//         string stepName = nameof(And))
//     {
//         var then = await thenTask.ConfigureAwait(false);
//         await Bdd.RunStepAsync(then.Ctx, stepName, title,
//             () => assertion(CancellationToken.None)).ConfigureAwait(false);
//         return then;
//     }
//
//     /// <summary>
//     /// Adds an <c>And</c> step that executes an asynchronous assertion after a previous <c>Then</c> step.
//     /// </summary>
//     /// <param name="thenTask">The task that resolves to the preceding <see cref="ThenBuilder"/>.</param>
//     /// <param name="title">The display title for this step.</param>
//     /// <param name="assertion">An asynchronous assertion delegate.</param>
//     /// <param name="stepName">The BDD keyword to display; defaults to <c>And</c>.</param>
//     /// <returns>The same <see cref="ThenBuilder"/> for further chaining.</returns>
//     public static Task<ThenBuilder> And(
//         this Task<ThenBuilder> thenTask,
//         string title,
//         Func<Task> assertion,
//         string stepName = nameof(And))
//         => thenTask.And(title, _ => assertion(), stepName);
//
//     /// <summary>
//     /// Adds an <c>And</c> step with a default title that executes an asynchronous assertion after a previous <c>Then</c> step.
//     /// </summary>
//     /// <param name="thenTask">The task that resolves to the preceding <see cref="ThenBuilder"/>.</param>
//     /// <param name="assertion">An asynchronous assertion that receives a <see cref="CancellationToken"/>.</param>
//     /// <returns>The same <see cref="ThenBuilder"/> for further chaining.</returns>
//     public static Task<ThenBuilder> And(
//         this Task<ThenBuilder> thenTask,
//         Func<CancellationToken, Task> assertion)
//         => thenTask.And("And assertion", assertion);
//
//     /// <summary>
//     /// Adds an <c>And</c> step with a default title that executes an asynchronous assertion after a previous <c>Then</c> step.
//     /// </summary>
//     /// <param name="thenTask">The task that resolves to the preceding <see cref="ThenBuilder"/>.</param>
//     /// <param name="assertion">An asynchronous assertion delegate.</param>
//     /// <returns>The same <see cref="ThenBuilder"/> for further chaining.</returns>
//     public static Task<ThenBuilder> And(
//         this Task<ThenBuilder> thenTask,
//         Func<Task> assertion)
//         => thenTask.And("And assertion", assertion);
//
//     /// <summary>
//     /// Adds a <c>But</c> step that executes an asynchronous assertion after a previous <c>Then</c> step.
//     /// </summary>
//     /// <param name="thenTask">The task that resolves to the preceding <see cref="ThenBuilder"/>.</param>
//     /// <param name="title">The display title for this step.</param>
//     /// <param name="assertion">An asynchronous assertion that receives a <see cref="CancellationToken"/>.</param>
//     /// <returns>The same <see cref="ThenBuilder"/> for further chaining.</returns>
//     public static Task<ThenBuilder> But(
//         this Task<ThenBuilder> thenTask,
//         string title,
//         Func<CancellationToken, Task> assertion)
//         => thenTask.And(title, assertion, nameof(But));
//
//     /// <summary>
//     /// Adds a <c>But</c> step that executes an asynchronous assertion after a previous <c>Then</c> step.
//     /// </summary>
//     /// <param name="thenTask">The task that resolves to the preceding <see cref="ThenBuilder"/>.</param>
//     /// <param name="title">The display title for this step.</param>
//     /// <param name="assertion">An asynchronous assertion delegate.</param>
//     /// <returns>The same <see cref="ThenBuilder"/> for further chaining.</returns>
//     public static Task<ThenBuilder> But(
//         this Task<ThenBuilder> thenTask,
//         string title,
//         Func<Task> assertion)
//         => thenTask.And(title, assertion);
//
//     // ---------------------------
//     // And / But after Then<T> (typed)
//     // ---------------------------
//
//     /// <summary>
//     /// Adds an <c>And</c> step that executes an asynchronous assertion against the value from a preceding typed <c>Then</c> step.
//     /// </summary>
//     /// <typeparam name="T">The type carried by the preceding <see cref="ThenBuilder{T}"/>.</typeparam>
//     /// <param name="thenTask">The task that resolves to the preceding <see cref="ThenBuilder{T}"/>.</param>
//     /// <param name="title">The display title for this step.</param>
//     /// <param name="assertion">An asynchronous assertion that receives the value and a <see cref="CancellationToken"/>.</param>
//     /// <param name="stepName">The BDD keyword to display; defaults to <c>And</c>. Use <c>But</c> for the corresponding <see cref="But{T}(Task{ThenBuilder{T}}, string, Func{T, CancellationToken, Task})"/> overload.</param>
//     /// <returns>The same <see cref="ThenBuilder{T}"/> for further chaining.</returns>
//     public static async Task<ThenBuilder<T>> And<T>(
//         this Task<ThenBuilder<T>> thenTask,
//         string title,
//         Func<T, CancellationToken, Task> assertion,
//         string stepName = nameof(And))
//     {
//         var then = await thenTask.ConfigureAwait(false);
//         await Bdd.RunStepAsync(then.Ctx, stepName, title,
//             () => assertion(then.Value, CancellationToken.None)).ConfigureAwait(false);
//         return then;
//     }
//
//     /// <summary>
//     /// Adds an <c>And</c> step that executes an asynchronous assertion against the value from a preceding typed <c>Then</c> step.
//     /// </summary>
//     /// <typeparam name="T">The type carried by the preceding <see cref="ThenBuilder{T}"/>.</typeparam>
//     /// <param name="thenTask">The task that resolves to the preceding <see cref="ThenBuilder{T}"/>.</param>
//     /// <param name="title">The display title for this step.</param>
//     /// <param name="assertion">An asynchronous assertion that receives the value.</param>
//     /// <returns>The same <see cref="ThenBuilder{T}"/> for further chaining.</returns>
//     public static Task<ThenBuilder<T>> And<T>(
//         this Task<ThenBuilder<T>> thenTask,
//         string title,
//         Func<T, Task> assertion)
//         => thenTask.And(title, (v, _) => assertion(v));
//
//     /// <summary>
//     /// Adds an <c>And</c> step with a default title that executes an asynchronous assertion against the value from a preceding typed <c>Then</c> step.
//     /// </summary>
//     /// <typeparam name="T">The type carried by the preceding <see cref="ThenBuilder{T}"/>.</typeparam>
//     /// <param name="thenTask">The task that resolves to the preceding <see cref="ThenBuilder{T}"/>.</param>
//     /// <param name="assertion">An asynchronous assertion that receives the value and a <see cref="CancellationToken"/>.</param>
//     /// <returns>The same <see cref="ThenBuilder{T}"/> for further chaining.</returns>
//     public static Task<ThenBuilder<T>> And<T>(
//         this Task<ThenBuilder<T>> thenTask,
//         Func<T, CancellationToken, Task> assertion)
//         => thenTask.And("And assertion", assertion);
//
//     /// <summary>
//     /// Adds an <c>And</c> step with a default title that executes an asynchronous assertion against the value from a preceding typed <c>Then</c> step.
//     /// </summary>
//     /// <typeparam name="T">The type carried by the preceding <see cref="ThenBuilder{T}"/>.</typeparam>
//     /// <param name="thenTask">The task that resolves to the preceding <see cref="ThenBuilder{T}"/>.</param>
//     /// <param name="assertion">An asynchronous assertion that receives the value.</param>
//     /// <returns>The same <see cref="ThenBuilder{T}"/> for further chaining.</returns>
//     public static Task<ThenBuilder<T>> And<T>(
//         this Task<ThenBuilder<T>> thenTask,
//         Func<T, Task> assertion)
//         => thenTask.And("And assertion", assertion);
//
//     /// <summary>
//     /// Adds a <c>But</c> step that executes an asynchronous assertion against the value from a preceding typed <c>Then</c> step.
//     /// </summary>
//     /// <typeparam name="T">The type carried by the preceding <see cref="ThenBuilder{T}"/>.</typeparam>
//     /// <param name="thenTask">The task that resolves to the preceding <see cref="ThenBuilder{T}"/>.</param>
//     /// <param name="title">The display title for this step.</param>
//     /// <param name="assertion">An asynchronous assertion that receives the value and a <see cref="CancellationToken"/>.</param>
//     /// <returns>The same <see cref="ThenBuilder{T}"/> for further chaining.</returns>
//     public static Task<ThenBuilder<T>> But<T>(
//         this Task<ThenBuilder<T>> thenTask,
//         string title,
//         Func<T, CancellationToken, Task> assertion)
//         => thenTask.And(title, assertion, nameof(But));
//
//     /// <summary>
//     /// Adds a <c>But</c> step that executes an asynchronous assertion against the value from a preceding typed <c>Then</c> step.
//     /// </summary>
//     /// <typeparam name="T">The type carried by the preceding <see cref="ThenBuilder{T}"/>.</typeparam>
//     /// <param name="thenTask">The task that resolves to the preceding <see cref="ThenBuilder{T}"/>.</param>
//     /// <param name="title">The display title for this step.</param>
//     /// <param name="assertion">An asynchronous assertion that receives the value.</param>
//     /// <returns>The same <see cref="ThenBuilder{T}"/> for further chaining.</returns>
//     public static Task<ThenBuilder<T>> But<T>(
//         this Task<ThenBuilder<T>> thenTask,
//         string title,
//         Func<T, Task> assertion)
//         => thenTask.And(title, (v, _) => assertion(v), nameof(But));
//
//     // -------------------------------------------------------
//     // THEN (untyped When)  — predicate overloads
//     // -------------------------------------------------------
//
//     /// <summary>
//     /// Adds a <c>Then</c> step after an untyped <c>When</c> step using an asynchronous boolean predicate with cancellation.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <param name="whenTask">The task that resolves to the preceding <see cref="WhenBuilder{TGiven}"/>.</param>
//     /// <param name="title">The display title for this <c>Then</c> step.</param>
//     /// <param name="predicate">An asynchronous boolean predicate. If it evaluates to <see langword="false"/>, a <see cref="BddAssertException"/> is thrown.</param>
//     /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder"/> for further chaining.</returns>
//     public static Task<ThenBuilder> Then<TGiven>(
//         this Task<WhenBuilder<TGiven>> whenTask,
//         string title,
//         Func<CancellationToken, Task<bool>> predicate)
//         => whenTask.Then(title, async ct =>
//         {
//             if (!await predicate(ct).ConfigureAwait(false))
//                 throw new BddAssertException($"Assertion failed: {title}");
//         });
//
//     /// <summary>
//     /// Adds a <c>Then</c> step after an untyped <c>When</c> step using an asynchronous boolean predicate.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <param name="whenTask">The task that resolves to the preceding <see cref="WhenBuilder{TGiven}"/>.</param>
//     /// <param name="title">The display title for this <c>Then</c> step.</param>
//     /// <param name="predicate">An asynchronous boolean predicate. If it evaluates to <see langword="false"/>, a <see cref="BddAssertException"/> is thrown.</param>
//     /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder"/> for further chaining.</returns>
//     public static Task<ThenBuilder> Then<TGiven>(
//         this Task<WhenBuilder<TGiven>> whenTask,
//         string title,
//         Func<Task<bool>> predicate)
//         => whenTask.Then(title, _ => predicate());
//
//     /// <summary>
//     /// Adds a <c>Then</c> step with a default title after an untyped <c>When</c> step using an asynchronous boolean predicate with cancellation.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <param name="whenTask">The task that resolves to the preceding <see cref="WhenBuilder{TGiven}"/>.</param>
//     /// <param name="predicate">An asynchronous boolean predicate. If it evaluates to <see langword="false"/>, a <see cref="BddAssertException"/> is thrown.</param>
//     /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder"/> for further chaining.</returns>
//     public static Task<ThenBuilder> Then<TGiven>(
//         this Task<WhenBuilder<TGiven>> whenTask,
//         Func<CancellationToken, Task<bool>> predicate)
//         => whenTask.Then("Then assertion", predicate);
//
//     /// <summary>
//     /// Adds a <c>Then</c> step with a default title after an untyped <c>When</c> step using an asynchronous boolean predicate.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <param name="whenTask">The task that resolves to the preceding <see cref="WhenBuilder{TGiven}"/>.</param>
//     /// <param name="predicate">An asynchronous boolean predicate. If it evaluates to <see langword="false"/>, a <see cref="BddAssertException"/> is thrown.</param>
//     /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder"/> for further chaining.</returns>
//     public static Task<ThenBuilder> Then<TGiven>(
//         this Task<WhenBuilder<TGiven>> whenTask,
//         Func<Task<bool>> predicate)
//         => whenTask.Then("Then assertion", predicate);
//
//     // -------------------------------------------------------
//     // THEN (typed When<TOut>) — predicate overloads
//     // -------------------------------------------------------
//
//     /// <summary>
//     /// Adds a <c>Then</c> step after a typed <c>When</c> step using an asynchronous boolean predicate with cancellation.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <typeparam name="TOut">The type produced by the preceding <c>When</c> step.</typeparam>
//     /// <param name="whenTask">The task that resolves to the preceding <see cref="WhenBuilder{TGiven, TOut}"/>.</param>
//     /// <param name="title">The display title for this <c>Then</c> step.</param>
//     /// <param name="predicate">An asynchronous boolean predicate evaluated against the <typeparamref name="TOut"/> value. If it evaluates to <see langword="false"/>, a <see cref="BddAssertException"/> is thrown.</param>
//     /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder{TOut}"/> for further chaining.</returns>
//     public static Task<ThenBuilder<TOut>> Then<TGiven, TOut>(
//         this Task<WhenBuilder<TGiven, TOut>> whenTask,
//         string title,
//         Func<TOut, CancellationToken, Task<bool>> predicate)
//         => whenTask.Then(title, async (value, ct) =>
//         {
//             if (!await predicate(value, ct).ConfigureAwait(false))
//                 throw new BddAssertException($"Assertion failed: {title}");
//         });
//
//
//     /// <summary>
//     /// Adds a <c>Then</c> step after a typed <c>When</c> step using an asynchronous boolean predicate.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <typeparam name="TOut">The type produced by the preceding <c>When</c> step.</typeparam>
//     /// <param name="whenTask">The task that resolves to the preceding <see cref="WhenBuilder{TGiven, TOut}"/>.</param>
//     /// <param name="title">The display title for this <c>Then</c> step.</param>
//     /// <param name="predicate">An asynchronous boolean predicate evaluated against the <typeparamref name="TOut"/> value.</param>
//     /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder{TOut}"/> for further chaining.</returns>
//     public static Task<ThenBuilder<TOut>> Then<TGiven, TOut>(
//         this Task<WhenBuilder<TGiven, TOut>> whenTask,
//         string title,
//         Func<TOut, Task<bool>> predicate)
//         => whenTask.Then(title, (v, _) => predicate(v));
//
//     /// <summary>
//     /// Adds a <c>Then</c> step with a default title after a typed <c>When</c> step using an asynchronous boolean predicate with cancellation.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <typeparam name="TOut">The type produced by the preceding <c>When</c> step.</typeparam>
//     /// <param name="whenTask">The task that resolves to the preceding <see cref="WhenBuilder{TGiven, TOut}"/>.</param>
//     /// <param name="predicate">An asynchronous boolean predicate evaluated against the <typeparamref name="TOut"/> value.</param>
//     /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder{TOut}"/> for further chaining.</returns>
//     public static Task<ThenBuilder<TOut>> Then<TGiven, TOut>(
//         this Task<WhenBuilder<TGiven, TOut>> whenTask,
//         Func<TOut, CancellationToken, Task<bool>> predicate)
//         => whenTask.Then("Then assertion", predicate);
//
//     /// <summary>
//     /// Adds a <c>Then</c> step with a default title after a typed <c>When</c> step using an asynchronous boolean predicate.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <typeparam name="TOut">The type produced by the preceding <c>When</c> step.</typeparam>
//     /// <param name="whenTask">The task that resolves to the preceding <see cref="WhenBuilder{TGiven, TOut}"/>.</param>
//     /// <param name="predicate">An asynchronous boolean predicate evaluated against the <typeparamref name="TOut"/> value.</param>
//     /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder{TOut}"/> for further chaining.</returns>
//     public static Task<ThenBuilder<TOut>> Then<TGiven, TOut>(
//         this Task<WhenBuilder<TGiven, TOut>> whenTask,
//         Func<TOut, Task<bool>> predicate)
//         => whenTask.Then("Then assertion", predicate);
//
//     // ---------------------------
//     // Then(...) after When(..., TOut) (typed) — SYNC BOOL
//     // ---------------------------
//
//     /// <summary>
//     /// Adds a <c>Then</c> step after a typed <c>When</c> step using a synchronous boolean predicate.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <typeparam name="TOut">The type produced by the preceding <c>When</c> step.</typeparam>
//     /// <param name="whenTask">The task that resolves to the preceding <see cref="WhenBuilder{TGiven, TOut}"/>.</param>
//     /// <param name="title">The display title for this <c>Then</c> step.</param>
//     /// <param name="predicate">A synchronous boolean predicate evaluated against the <typeparamref name="TOut"/> value.</param>
//     /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder{TOut}"/> for further chaining.</returns>
//     public static Task<ThenBuilder<TOut>> Then<TGiven, TOut>(
//         this Task<WhenBuilder<TGiven, TOut>> whenTask,
//         string title,
//         Func<TOut, bool> predicate)
//         => whenTask.Then(title, (v, _) =>
//         {
//             if (!predicate(v)) throw new BddAssertException(title);
//             return Task.CompletedTask;
//         });
//
//     /// <summary>
//     /// Adds a <c>Then</c> step with a default title after a typed <c>When</c> step using a synchronous boolean predicate.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <typeparam name="TOut">The type produced by the preceding <c>When</c> step.</typeparam>
//     /// <param name="whenTask">The task that resolves to the preceding <see cref="WhenBuilder{TGiven, TOut}"/>.</param>
//     /// <param name="predicate">A synchronous boolean predicate evaluated against the <typeparamref name="TOut"/> value.</param>
//     /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder{TOut}"/> for further chaining.</returns>
//     public static Task<ThenBuilder<TOut>> Then<TGiven, TOut>(
//         this Task<WhenBuilder<TGiven, TOut>> whenTask,
//         Func<TOut, bool> predicate)
//         => whenTask.Then("Then assertion", predicate);
//
//
//     // ---------------------------
//     // Then(...) after When(... TGiven) (untyped) — SYNC BOOL
//     // ---------------------------
//
//     /// <summary>
//     /// Adds a <c>Then</c> step after an untyped <c>When</c> step using a synchronous boolean predicate.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <param name="whenTask">The task that resolves to the preceding <see cref="WhenBuilder{TGiven}"/>.</param>
//     /// <param name="title">The display title for this <c>Then</c> step.</param>
//     /// <param name="predicate">A synchronous boolean predicate.</param>
//     /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder"/> for further chaining.</returns>
//     public static Task<ThenBuilder> Then<TGiven>(
//         this Task<WhenBuilder<TGiven>> whenTask,
//         string title,
//         Func<bool> predicate)
//         => whenTask.Then(title, _ =>
//         {
//             if (!predicate()) throw new BddAssertException(title);
//             return Task.CompletedTask;
//         });
//
//     /// <summary>
//     /// Adds a <c>Then</c> step with a default title after an untyped <c>When</c> step using a synchronous boolean predicate.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <param name="whenTask">The task that resolves to the preceding <see cref="WhenBuilder{TGiven}"/>.</param>
//     /// <param name="predicate">A synchronous boolean predicate.</param>
//     /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder"/> for further chaining.</returns>
//     public static Task<ThenBuilder> Then<TGiven>(
//         this Task<WhenBuilder<TGiven>> whenTask,
//         Func<bool> predicate)
//         => whenTask.Then("Then assertion", predicate);
//
//     // -------------------------------------------------------
//     // AND / BUT on Then<T> — predicate overloads
//     // -------------------------------------------------------
//
//     /// <summary>
//     /// Adds an <c>And</c> step after a typed <c>Then</c> step using an asynchronous boolean predicate with cancellation.
//     /// </summary>
//     /// <typeparam name="T">The type carried by the preceding <see cref="ThenBuilder{T}"/>.</typeparam>
//     /// <param name="thenTask">The task that resolves to the preceding <see cref="ThenBuilder{T}"/>.</param>
//     /// <param name="title">The display title for this step.</param>
//     /// <param name="predicate">An asynchronous boolean predicate evaluated against the value. If it evaluates to <see langword="false"/>, a <see cref="BddAssertException"/> is thrown.</param>
//     /// <returns>The same <see cref="ThenBuilder{T}"/> for further chaining.</returns>
//     public static Task<ThenBuilder<T>> And<T>(
//         this Task<ThenBuilder<T>> thenTask,
//         string title,
//         Func<T, CancellationToken, Task<bool>> predicate)
//         => thenTask.And(title, async (v, ct) =>
//         {
//             if (!await predicate(v, ct).ConfigureAwait(false))
//                 throw new BddAssertException($"Assertion failed: {title}");
//         });
//
//     /// <summary>
//     /// Adds an <c>And</c> step after a typed <c>Then</c> step using an asynchronous boolean predicate.
//     /// </summary>
//     /// <typeparam name="T">The type carried by the preceding <see cref="ThenBuilder{T}"/>.</typeparam>
//     /// <param name="thenTask">The task that resolves to the preceding <see cref="ThenBuilder{T}"/>.</param>
//     /// <param name="title">The display title for this step.</param>
//     /// <param name="predicate">An asynchronous boolean predicate evaluated against the value.</param>
//     /// <returns>The same <see cref="ThenBuilder{T}"/> for further chaining.</returns>
//     public static Task<ThenBuilder<T>> And<T>(
//         this Task<ThenBuilder<T>> thenTask,
//         string title,
//         Func<T, Task<bool>> predicate)
//         => thenTask.And(title, (v, _) => predicate(v));
//
//
//     // /// <summary>
//     // /// Adds an <c>And</c> step with a default title after a typed <c>Then</c> step using an asynchronous boolean predicate with cancellation.
//     // /// </summary>
//     // /// <typeparam name="T">The type carried by the preceding <see cref="ThenBuilder{T}"/>.</typeparam>
//     // /// <param name="thenTask">The task that resolves to the preceding <see cref="ThenBuilder{T}"/>.</param>
//     // /// <param name="title">The display title for this step.</param>
//     // /// <param name="predicate">An asynchronous boolean predicate evaluated against the value. If it evaluates to <see langword="false"/>, a <see cref="BddAssertException"/> is thrown.</param>
//     // /// <returns>The same <see cref="ThenBuilder{T}"/> for further chaining.</returns>
//     // public static Task<ThenBuilder<T>> And<T>(
//     //     this Task<ThenBuilder<T>> thenTask,
//     //     string title,
//     //     Func<T, bool> predicate)
//     //     => thenTask.And(title, v =>
//     //     {
//     //         if (!predicate(v))
//     //             throw new BddAssertException($"Assertion failed: {title}");
//     //
//     //         return Task.CompletedTask;
//     //     });
//
//     /// <summary>
//     /// Adds a <c>But</c> step after a typed <c>Then</c> step using an asynchronous boolean predicate with cancellation.
//     /// </summary>
//     /// <typeparam name="T">The type carried by the preceding <see cref="ThenBuilder{T}"/>.</typeparam>
//     /// <param name="thenTask">The task that resolves to the preceding <see cref="ThenBuilder{T}"/>.</param>
//     /// <param name="title">The display title for this step.</param>
//     /// <param name="predicate">An asynchronous boolean predicate evaluated against the value.</param>
//     /// <returns>The same <see cref="ThenBuilder{T}"/> for further chaining.</returns>
//     public static Task<ThenBuilder<T>> But<T>(
//         this Task<ThenBuilder<T>> thenTask,
//         string title,
//         Func<T, CancellationToken, Task<bool>> predicate)
//         => thenTask.And(title, predicate, nameof(But));
//
//     /// <summary>
//     /// Adds a <c>But</c> step after a typed <c>Then</c> step using an asynchronous boolean predicate.
//     /// </summary>
//     /// <typeparam name="T">The type carried by the preceding <see cref="ThenBuilder{T}"/>.</typeparam>
//     /// <param name="thenTask">The task that resolves to the preceding <see cref="ThenBuilder{T}"/>.</param>
//     /// <param name="title">The display title for this step.</param>
//     /// <param name="predicate">An asynchronous boolean predicate evaluated against the value.</param>
//     /// <returns>The same <see cref="ThenBuilder{T}"/> for further chaining.</returns>
//     public static Task<ThenBuilder<T>> But<T>(
//         this Task<ThenBuilder<T>> thenTask,
//         string title,
//         Func<T, Task<bool>> predicate)
//         => thenTask.And(title, (v, _) => predicate(v), nameof(But));
//
//     /// <summary>
//     /// Defines a <c>Then</c> transform directly after a <c>Given</c>, acting as an alias for
//     /// <see cref="When{TGiven, TOut}(GivenBuilder{TGiven}, string, Func{TGiven, CancellationToken, Task{TOut}})"/>.
//     /// This overload accepts a token-aware asynchronous transformation that produces a value of type
//     /// <typeparamref name="TOut"/> from the <typeparamref name="TGiven"/> value.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <typeparam name="TOut">The type produced by this transform.</typeparam>
//     /// <param name="given">The preceding <see cref="GivenBuilder{TGiven}"/>.</param>
//     /// <param name="title">Display title for this step in the scenario output.</param>
//     /// <param name="transform">
//     /// An asynchronous delegate receiving the <typeparamref name="TGiven"/> value and a
//     /// <see cref="CancellationToken"/>, returning a <typeparamref name="TOut"/> result.
//     /// </param>
//     /// <returns>
//     /// A <see cref="Task{TResult}"/> that resolves to a <see cref="WhenBuilder{TGiven, TOut}"/>
//     /// for chaining subsequent <c>Then</c>/<c>And</c>/<c>But</c> assertions.
//     /// </returns>
//     public static Task<ThenBuilder<TOut>> Then<TGiven, TOut>(
//         this GivenBuilder<TGiven> given,
//         string title,
//         Func<TGiven, CancellationToken, Task<TOut>> transform)
//         => given
//             .When(title, (v,_) => Task.FromResult(v))
//             .Then(title, transform);
//
//
//     /// <summary>
//     /// Defines a <c>Then</c> transform directly after a <c>Given</c>, acting as an alias for
//     /// <see cref="When{TGiven, TOut}(GivenBuilder{TGiven}, string, Func{TGiven, Task{TOut}})"/>.
//     /// This overload accepts an asynchronous transformation (without a token) that produces a value of type
//     /// <typeparamref name="TOut"/> from the <typeparamref name="TGiven"/> value.
//     /// </summary>
//     /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     /// <typeparam name="TOut">The type produced by this transform.</typeparam>
//     /// <param name="given">The preceding <see cref="GivenBuilder{TGiven}"/>.</param>
//     /// <param name="title">Display title for this step in the scenario output.</param>
//     /// <param name="transform">
//     /// An asynchronous delegate receiving the <typeparamref name="TGiven"/> value and returning a
//     /// <typeparamref name="TOut"/> result.
//     /// </param>
//     /// <returns>
//     /// A <see cref="Task{TResult}"/> that resolves to a <see cref="WhenBuilder{TGiven, TOut}"/>
//     /// for chaining subsequent <c>Then</c>/<c>And</c>/<c>But</c> assertions.
//     /// </returns>
//     public static Task<ThenBuilder<TOut>> Then<TGiven, TOut>(
//         this GivenBuilder<TGiven> given,
//         string title,
//         Func<TGiven, Task<TOut>> transform)
//         => given
//             .When<TGiven, TOut>(title, (g, _) => transform(g))
//             .Then(title, (v, _) => Task.FromResult(v));
//
//
//     /// <summary>
//     /// Adds an <c>And</c> assertion after a typed <c>Then</c>, using a synchronous boolean predicate.
//     /// The step is recorded with kind <c>And</c>. If <paramref name="predicate"/> returns <see langword="false"/>,
//     /// a <see cref="BddAssertException"/> is thrown.
//     /// </summary>
//     /// <typeparam name="T">The value type carried by the preceding <see cref="ThenBuilder{T}"/>.</typeparam>
//     /// <param name="thenTask">The task resolving to the preceding <see cref="ThenBuilder{T}"/>.</param>
//     /// <param name="title">Display title for this <c>And</c> step.</param>
//     /// <param name="predicate">Synchronous predicate evaluated against the carried value.</param>
//     /// <param name="stepName">
//     /// Internal verb to record for this step (defaults to <c>"And"</c>). Use <c>nameof(But)</c> to log as <c>But</c>.
//     /// </param>
//     /// <returns>The same <see cref="ThenBuilder{T}"/> (wrapped in a task) for further chaining.</returns>
//     public static Task<ThenBuilder<T>> And<T>(
//         this Task<ThenBuilder<T>> thenTask,
//         string title,
//         Func<T, bool> predicate,
//         string stepName = nameof(And))
//         => thenTask.And(title, (v, _) =>
//         {
//             if (!predicate(v)) throw new BddAssertException(title);
//             return Task.CompletedTask;
//         }, stepName);
//
//
//     /// <summary>
//     /// Adds an <c>And</c> assertion after a typed <c>Then</c> with a default title,
//     /// using a synchronous boolean predicate. If the predicate returns <see langword="false"/>,
//     /// a <see cref="BddAssertException"/> is thrown.
//     /// </summary>
//     /// <typeparam name="T">The value type carried by the preceding <see cref="ThenBuilder{T}"/>.</typeparam>
//     /// <param name="thenTask">The task resolving to the preceding <see cref="ThenBuilder{T}"/>.</param>
//     /// <param name="predicate">Synchronous predicate evaluated against the carried value.</param>
//     /// <returns>The same <see cref="ThenBuilder{T}"/> (wrapped in a task) for further chaining.</returns>
//     public static Task<ThenBuilder<T>> And<T>(
//         this Task<ThenBuilder<T>> thenTask,
//         Func<T, bool> predicate)
//         => thenTask.And("And assertion", predicate);
//
//
//     /// <summary>
//     /// Adds a <c>But</c> assertion after a typed <c>Then</c>, using a synchronous boolean predicate.
//     /// The step is recorded with kind <c>But</c>. If <paramref name="predicate"/> returns <see langword="false"/>,
//     /// a <see cref="BddAssertException"/> is thrown.
//     /// </summary>
//     /// <typeparam name="T">The value type carried by the preceding <see cref="ThenBuilder{T}"/>.</typeparam>
//     /// <param name="thenTask">The task resolving to the preceding <see cref="ThenBuilder{T}"/>.</param>
//     /// <param name="title">Display title for this <c>But</c> step.</param>
//     /// <param name="predicate">Synchronous predicate evaluated against the carried value.</param>
//     /// <returns>The same <see cref="ThenBuilder{T}"/> (wrapped in a task) for further chaining.</returns>
//     public static Task<ThenBuilder<T>> But<T>(
//         this Task<ThenBuilder<T>> thenTask,
//         string title,
//         Func<T, bool> predicate)
//         => thenTask.And(title, predicate, nameof(But));
//
//
//     /// <summary>
//     /// Adds a <c>But</c> assertion after a typed <c>Then</c> with a default title,
//     /// using a synchronous boolean predicate. The step is recorded with kind <c>But</c>.
//     /// If the predicate returns <see langword="false"/>, a <see cref="BddAssertException"/> is thrown.
//     /// </summary>
//     /// <typeparam name="T">The value type carried by the preceding <see cref="ThenBuilder{T}"/>.</typeparam>
//     /// <param name="thenTask">The task resolving to the preceding <see cref="ThenBuilder{T}"/>.</param>
//     /// <param name="predicate">Synchronous predicate evaluated against the carried value.</param>
//     /// <returns>The same <see cref="ThenBuilder{T}"/> (wrapped in a task) for further chaining.</returns>
//     public static Task<ThenBuilder<T>> But<T>(
//         this Task<ThenBuilder<T>> thenTask,
//         Func<T, bool> predicate)
//         => thenTask.And("But assertion", predicate, nameof(But));
//
//
//     // ---------------------------
//     // And / But directly after When(..., TOut) (typed) — predicate overloads
//     // ---------------------------
//
//     // /// <summary>
//     // /// Adds an <c>And</c> assertion directly after a typed <c>When</c> step using an asynchronous boolean predicate with cancellation.
//     // /// </summary>
//     // /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     // /// <typeparam name="TOut">The type produced by the preceding <c>When</c> step.</typeparam>
//     // /// <param name="whenTask">The task that resolves to the preceding <see cref="WhenBuilder{TGiven, TOut}"/>.</param>
//     // /// <param name="title">The display title for this <c>And</c> step.</param>
//     // /// <param name="predicate">An asynchronous boolean predicate evaluated against the <typeparamref name="TOut"/> value. If it evaluates to <see langword="false"/>, a <see cref="BddAssertException"/> is thrown.</param>
//     // /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder{TOut}"/> for further chaining.</returns>
//     // public static async Task<ThenBuilder<TOut>> And<TGiven, TOut>(
//     //     this Task<WhenBuilder<TGiven, TOut>> whenTask,
//     //     string title,
//     //     Func<TOut, CancellationToken, Task<bool>> predicate)
//     // {
//     //     var when = await whenTask.ConfigureAwait(false);
//     //
//     //     var result = await Bdd.RunStepAsync(when.Ctx, "When", when.Title,
//     //         () => when.Fn(when.Given, CancellationToken.None)).ConfigureAwait(false);
//     //
//     //     await Bdd.RunStepAsync(when.Ctx, nameof(And), title, async () =>
//     //     {
//     //         if (!await predicate(result, CancellationToken.None).ConfigureAwait(false))
//     //             throw new BddAssertException($"Assertion failed: {title}");
//     //     }).ConfigureAwait(false);
//     //
//     //     return new ThenBuilder<TOut>(when.Ctx, result);
//     // }
//     //
//     // /// <summary>
//     // /// Adds an <c>And</c> assertion directly after a typed <c>When</c> step using an asynchronous boolean predicate.
//     // /// </summary>
//     // /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     // /// <typeparam name="TOut">The type produced by the preceding <c>When</c> step.</typeparam>
//     // /// <param name="whenTask">The task that resolves to the preceding <see cref="WhenBuilder{TGiven, TOut}"/>.</param>
//     // /// <param name="title">The display title for this <c>And</c> step.</param>
//     // /// <param name="predicate">An asynchronous boolean predicate evaluated against the <typeparamref name="TOut"/> value.</param>
//     // /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder{TOut}"/> for further chaining.</returns>
//     // public static Task<ThenBuilder<TOut>> And<TGiven, TOut>(
//     //     this Task<WhenBuilder<TGiven, TOut>> whenTask,
//     //     string title,
//     //     Func<TOut, Task<bool>> predicate)
//     //     => whenTask.And(title, (v, _) => predicate(v));
//     //
//     // /// <summary>
//     // /// Adds an <c>And</c> assertion directly after a typed <c>When</c> step using a synchronous boolean predicate.
//     // /// </summary>
//     // /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
//     // /// <typeparam name="TOut">The type produced by the preceding <c>When</c> step.</typeparam>
//     // /// <param name="whenTask">The task that resolves to the preceding <see cref="WhenBuilder{TGiven, TOut}"/>.</param>
//     // /// <param name="title">The display title for this <c>And</c> step.</param>
//     // /// <param name="predicate">A synchronous boolean predicate evaluated against the <typeparamref name="TOut"/> value.</param>
//     // /// <returns>A <see cref="Task"/> that resolves to a <see cref="ThenBuilder{TOut}"/> for further chaining.</returns>
//     // public static Task<ThenBuilder<TOut>> And<TGiven, TOut>(
//     //     this Task<WhenBuilder<TGiven, TOut>> whenTask,
//     //     string title,
//     //     Func<TOut, bool> predicate)
//     //     => whenTask.And(title, (v, _) => Task.FromResult(predicate(v)));
// }