using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class CheckHandler : IUnityctlCommand
    {
        public string CommandName => "check";

        public CommandResponse Execute(CommandRequest request)
        {
#if UNITY_EDITOR
            try
            {
                var type = request.GetParam("type", "compile");

                if (type != "compile")
                {
                    return CommandResponse.Fail(StatusCode.InvalidParameters,
                        $"Unknown check type: {type}. Currently only 'compile' is supported.");
                }

                var assemblyNames = UnityEditor.Compilation.CompilationPipeline
                    .GetAssemblies(UnityEditor.Compilation.AssembliesType.Player)
                    .Select(a => a.name)
                    .ToArray();

                var data = new JObject
                {
                    ["assemblies"] = assemblyNames.Length,
                    ["assemblyNames"] = string.Join(", ", assemblyNames.Take(10)),
                    ["isCompiling"] = UnityEditor.EditorApplication.isCompiling
                };
                return CommandResponse.Ok("Compilation check passed", data);
            }
            catch (Exception e)
            {
                return CommandResponse.Fail(StatusCode.UnknownError,
                    $"Compile check failed: {e.Message}",
                    new List<string> { e.StackTrace });
            }
#else
            return CommandResponse.Fail(StatusCode.UnknownError, "Not running in Unity Editor");
#endif
        }
    }
}
