#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine.Rendering;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class RendererFeatureListHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.RendererFeatureList;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var rendererDataType = FindType("UnityEngine.Rendering.Universal.ScriptableRendererData");
            if (rendererDataType == null)
                return Fail(StatusCode.NotFound,
                    "URP ScriptableRendererData not available. Install com.unity.render-pipelines.universal");

            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline == null)
                return Fail(StatusCode.NotFound, "No SRP active. URP or HDRP must be the active render pipeline.");

            // Try to get renderer list from URP pipeline asset
            var rendererListProp = pipeline.GetType().GetProperty("rendererDataList",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (rendererListProp == null)
            {
                // Try alternative property name
                rendererListProp = pipeline.GetType().GetProperty("m_RendererDataList",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            }

            var rendererDataField = pipeline.GetType().GetField("m_RendererDataList",
                BindingFlags.NonPublic | BindingFlags.Instance);

            IList rendererList = null;
            try
            {
                if (rendererListProp != null)
                    rendererList = rendererListProp.GetValue(pipeline) as IList;
                else if (rendererDataField != null)
                    rendererList = rendererDataField.GetValue(pipeline) as IList;
            }
            catch (Exception)
            {
                // Reflection access may fail on some pipeline versions
            }

            if (rendererList == null || rendererList.Count == 0)
                return Ok("No renderer data found", new JObject { ["features"] = new JArray() });

            var features = new JArray();
            for (int i = 0; i < rendererList.Count; i++)
            {
                var rendererData = rendererList[i];
                if (rendererData == null) continue;

                var featuresProp = rendererData.GetType().GetProperty("rendererFeatures",
                    BindingFlags.Public | BindingFlags.Instance);
                var featuresList = featuresProp?.GetValue(rendererData) as IList;
                if (featuresList == null) continue;

                foreach (var feature in featuresList)
                {
                    if (feature == null) continue;
                    var featureObj = feature as UnityEngine.Object;

                    features.Add(new JObject
                    {
                        ["rendererIndex"] = i,
                        ["name"] = featureObj != null ? featureObj.name : feature.GetType().Name,
                        ["type"] = feature.GetType().Name,
                        ["fullType"] = feature.GetType().FullName,
                        ["isActive"] = GetBoolProperty(feature, "isActive", true)
                    });
                }
            }

            return Ok($"Found {features.Count} renderer feature(s)", new JObject
            {
                ["pipelineType"] = pipeline.GetType().Name,
                ["rendererCount"] = rendererList.Count,
                ["features"] = features
            });
        }

        private static Type FindType(string typeName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = asm.GetType(typeName);
                if (type != null) return type;
            }
            return null;
        }

        private static bool GetBoolProperty(object obj, string name, bool defaultValue)
        {
            var prop = obj.GetType().GetProperty(name);
            if (prop != null && prop.PropertyType == typeof(bool))
                return (bool)prop.GetValue(obj);
            return defaultValue;
        }
    }
}
#endif
