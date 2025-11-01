using System.Reflection;

using HarmonyLib;

using Aikido.Zen.Core.Helpers;

namespace Aikido.Zen.DotNetCore.Patches
{
    /// <summary>
    /// Patches for LLM client operations in .NET Core applications
    /// </summary>
    internal static class LLMPatches
    {
        /// <summary>
        /// Applies patches to various LLM client methods using Harmony.
        /// </summary>
        /// <param name="harmony">The Harmony instance used for patching.</param>
        public static void ApplyPatches(Harmony harmony)
        {
            // OpenAI
            PatchMethod(harmony, "OpenAI", "OpenAI.Chat.ChatClient", "CompleteChat", "System.Collections.Generic.IEnumerable`1[OpenAI.Chat.ChatMessage]", "OpenAI.Chat.ChatCompletionOptions", "System.Threading.CancellationToken");
            PatchMethod(harmony, "OpenAI", "OpenAI.Chat.ChatClient", "CompleteChatAsync", "System.Collections.Generic.IEnumerable`1[OpenAI.Chat.ChatMessage]", "OpenAI.Chat.ChatCompletionOptions", "System.Threading.CancellationToken");

            // Rystem.OpenAi
            PatchMethod(harmony, "Rystem.OpenAi", "Rystem.OpenAi.Chat.OpenAiChat", "ExecuteAsync", "System.Threading.CancellationToken");
            PatchMethod(harmony, "Rystem.OpenAi", "Rystem.OpenAi.Chat.OpenAiChat", "ExecuteAsStreamAsync", "System.Boolean", "System.Threading.CancellationToken");

            // Semantic Kernel: InvokePromptAsync
            // Dumped via DumpSkInvokePromptPatchLines()
            PatchMethod(harmony, "Microsoft.SemanticKernel.Core", "Microsoft.SemanticKernel.KernelExtensions", "InvokePromptAsync", "Microsoft.SemanticKernel.Kernel", "System.String", "Microsoft.SemanticKernel.KernelArguments", "System.String", "Microsoft.SemanticKernel.IPromptTemplateFactory", "Microsoft.SemanticKernel.PromptTemplateConfig", "System.Threading.CancellationToken");
            PatchMethod(harmony, "Microsoft.SemanticKernel.Core", "Microsoft.SemanticKernel.KernelExtensions", "InvokePromptAsync", "Microsoft.SemanticKernel.Kernel", "System.Text.Json.JsonSerializerOptions", "System.String", "Microsoft.SemanticKernel.KernelArguments", "System.String", "Microsoft.SemanticKernel.IPromptTemplateFactory", "Microsoft.SemanticKernel.PromptTemplateConfig", "System.Threading.CancellationToken");
            PatchMethod(harmony, "Microsoft.SemanticKernel.Core", "Microsoft.SemanticKernel.KernelExtensions", "InvokePromptAsync", "Microsoft.SemanticKernel.Kernel", "System.String", "Microsoft.SemanticKernel.KernelArguments", "System.String", "Microsoft.SemanticKernel.IPromptTemplateFactory", "Microsoft.SemanticKernel.PromptTemplateConfig", "System.Threading.CancellationToken");
            PatchMethod(harmony, "Microsoft.SemanticKernel.Core", "Microsoft.SemanticKernel.KernelExtensions", "InvokePromptAsync", "Microsoft.SemanticKernel.Kernel", "System.Text.Json.JsonSerializerOptions", "System.String", "Microsoft.SemanticKernel.KernelArguments", "System.String", "Microsoft.SemanticKernel.IPromptTemplateFactory", "Microsoft.SemanticKernel.PromptTemplateConfig", "System.Threading.CancellationToken");
            PatchMethod(harmony, "Microsoft.SemanticKernel.Core", "Microsoft.SemanticKernel.KernelExtensions", "InvokePromptAsync", "Microsoft.SemanticKernel.Kernel", "System.String", "Microsoft.SemanticKernel.KernelArguments", "System.String", "Microsoft.SemanticKernel.IPromptTemplateFactory", "Microsoft.SemanticKernel.PromptTemplateConfig");
        }

        static void DumpSkInvokePromptPatchLines()
        {
            // Try Core first, then fallback (covers different SK package layouts)
            var type =
                Type.GetType("Microsoft.SemanticKernel.KernelExtensions, Microsoft.SemanticKernel.Core", throwOnError: false)
                ?? Type.GetType("Microsoft.SemanticKernel.KernelExtensions, Microsoft.SemanticKernel", throwOnError: false);

            if (type == null)
            {
                Console.WriteLine("// KernelExtensions type not found in SK assemblies.");
                return;
            }

            var asmName = type.Assembly.GetName().Name; // e.g., "Microsoft.SemanticKernel.Core"
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                              .Where(m => m.Name == "InvokePromptAsync")
                              .ToArray();

            if (methods.Length == 0)
            {
                Console.WriteLine("// No InvokePromptAsync overloads found.");
                return;
            }

            Console.WriteLine($"// Found {methods.Length} InvokePromptAsync overload(s) in {asmName}");
            foreach (var m in methods)
            {
                var pars = m.GetParameters().Select(p => p.ParameterType.FullName!).ToArray();
                var quoted = pars.Select(s => $"\"{s}\"");
                var line = $"PatchMethod(harmony, \"{asmName}\", \"{type.FullName}\", \"{m.Name}\", {string.Join(", ", quoted)});";
                Console.WriteLine(line);
            }
        }

        /// <summary>
        /// Patches a method using Harmony by dynamically retrieving it via reflection.
        /// </summary>
        /// <param name="harmony">The Harmony instance used for patching.</param>
        /// <param name="assemblyName">The name of the assembly containing the type.</param>
        /// <param name="typeName">The name of the type containing the method.</param>
        /// <param name="methodName">The name of the method to patch.</param>
        /// <param name="parameterTypeNames">The names of the parameter types for the method.</param>
        private static void PatchMethod(Harmony harmony, string assemblyName, string typeName, string methodName, params string[] parameterTypeNames)
        {
            var method = ReflectionHelper.GetMethodFromAssembly(assemblyName, typeName, methodName, parameterTypeNames);
            if (method != null)
            {
                harmony.Patch(method, postfix: new HarmonyMethod(typeof(LLMPatches).GetMethod(nameof(OnLLMCallCompleted), BindingFlags.Static | BindingFlags.NonPublic)));
            }
        }

        /// <summary>
        /// Callback method executed after the original LLM method is completed.
        /// </summary>
        /// <param name="__args">The arguments passed to the original method.</param>
        /// <param name="__originalMethod">The original method being patched.</param>
        /// <param name="__instance">The instance of the LLM client being executed.</param>
        /// <param name="__result">The result returned by the original method.</param>
        private static void OnLLMCallCompleted(object[] __args, MethodBase __originalMethod, object __instance, object __result)
        {
            var assembly = __instance?.GetType().Assembly.FullName?.Split(new[] { ", Culture=" }, StringSplitOptions.RemoveEmptyEntries)[0] ?? string.Empty;
            var resolvedResult = LLMResultHelper.ResolveResult(__result);

            Aikido.Zen.Core.Patches.LLMPatcher.OnLLMCallCompleted(__args, __originalMethod, assembly, resolvedResult, Zen.GetContext());
        }
    }
}
