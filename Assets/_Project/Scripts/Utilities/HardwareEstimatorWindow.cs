#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kontexto.EditorTools
{
    public class HardwareEstimatorWindow : EditorWindow
    {
        private string minGpu = "Not Analyzed";
        private string recGpu = "Not Analyzed";
        private string minCpu = "Not Analyzed";
        private string recCpu = "Not Analyzed";
        private string minVram = "Not Analyzed";
        private string recVram = "Not Analyzed";
        private string minRam = "8 GB RAM";
        private string recRam = "16 GB RAM";

        private string primaryBottleneck = "N/A";
        private string optimizationTip = "Click 'Analyze Active Scene' to run hardware estimation.";

        private int sceneTriangles = 0;
        private int sceneVertices = 0;
        private int sceneDrawCalls = 0;
        private int sceneBatches = 0;
        private int sceneShadowCasters = 0;
        private string urpShadowRes = "Standard (2048)";

        private Vector2 scrollPosition;

        [MenuItem("Tools/Hardware Requirements Estimator")]
        public static void ShowWindow()
        {
            HardwareEstimatorWindow window = GetWindow<HardwareEstimatorWindow>("Hardware Estimator");
            window.minSize = new Vector2(450, 550);
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Title Banner
            EditorGUILayout.Space(10);
            GUILayout.Label("🎮 Kontexto Hardware Requirements Estimator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tool inspects the active scene's geometry, draw calls, batching efficiency, URP settings, and memory allocations to estimate minimum and recommended PC hardware requirements.", 
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            // Action Button
            GUI.backgroundColor = new Color(0.38f, 0.4f, 0.95f);
            if (GUILayout.Button("🔍 Analyze Active Scene & Estimate Specs", GUILayout.Height(38)))
            {
                AnalyzeActiveScene();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(15);

            // Measured Scene Metrics
            EditorGUILayout.LabelField("📊 Active Scene Ground-Truth Metrics", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Triangles:", sceneTriangles.ToString("N0"));
            EditorGUILayout.LabelField("Vertices:", sceneVertices.ToString("N0"));
            EditorGUILayout.LabelField("Draw Calls:", sceneDrawCalls.ToString("N0"));
            EditorGUILayout.LabelField("Batches:", sceneBatches.ToString("N0"));
            EditorGUILayout.LabelField("Shadow Casters:", sceneShadowCasters.ToString("N0"));
            EditorGUILayout.LabelField("URP Shadow Map Res:", urpShadowRes);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("💻 Minimum Hardware Requirements (720p @ 30 FPS - Low)", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("GPU:", minGpu);
            EditorGUILayout.LabelField("CPU:", minCpu);
            EditorGUILayout.LabelField("VRAM:", minVram);
            EditorGUILayout.LabelField("System RAM:", minRam);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("🚀 Recommended Hardware Requirements (1080p @ 60 FPS - High)", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("GPU:", recGpu);
            EditorGUILayout.LabelField("CPU:", recCpu);
            EditorGUILayout.LabelField("VRAM:", recVram);
            EditorGUILayout.LabelField("System RAM:", recRam);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("⚠️ Engine Performance & Bottleneck Diagnostics", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox($"Primary Bottleneck: {primaryBottleneck}\n\nRecommendation: {optimizationTip}", MessageType.Warning);

            EditorGUILayout.EndScrollView();
        }

        private void AnalyzeActiveScene()
        {
            // 1. Query live Stats from Unity Editor
            sceneTriangles = UnityStats.triangles;
            sceneVertices = UnityStats.vertices;
            sceneDrawCalls = UnityStats.drawCalls;
            sceneBatches = UnityStats.batches;
            sceneShadowCasters = UnityStats.shadowCasters;

            // 2. Query URP Pipeline Asset
            UniversalRenderPipelineAsset urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            int shadowMapRes = 2048;
            if (urpAsset != null)
            {
                shadowMapRes = (int)urpAsset.mainLightShadowmapResolution;
                urpShadowRes = $"{shadowMapRes} x {shadowMapRes}";
            }
            else
            {
                urpShadowRes = "Built-in / Default Pipeline";
            }

            // 3. Calculate Performance Metrics
            float gpuIndex = (sceneTriangles / 150000f) * 1.0f + (shadowMapRes / 2048f) * 1.2f + (sceneShadowCasters / 100f) * 0.5f;
            float cpuIndex = (sceneDrawCalls / 400f) * 1.5f;

            long totalAllocatedMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            float estMemGB = (totalAllocatedMemory / (1024f * 1024f * 1024f)) + 1.5f;

            // 4. Map to Hardware GPU Specifications
            if (gpuIndex <= 1.5f)
            {
                minGpu = "GTX 750 Ti / AMD Radeon RX 550 / Intel Iris Xe";
                recGpu = "GTX 1660 (6GB) / AMD Radeon RX 580";
            }
            else if (gpuIndex <= 3.2f)
            {
                minGpu = "GTX 1050 Ti (4GB) / AMD Radeon RX 560";
                recGpu = "RTX 2060 (6GB) / AMD Radeon RX 6600";
            }
            else
            {
                minGpu = "GTX 1060 (6GB) / AMD Radeon RX 580";
                recGpu = "RTX 3070 (8GB) / AMD Radeon RX 6700 XT";
            }

            // 5. Map to Hardware CPU Specifications
            if (cpuIndex <= 1.5f)
            {
                minCpu = "Intel Core i3-4130 / AMD FX-6300";
                recCpu = "Intel Core i5-10400 / AMD Ryzen 5 3600";
            }
            else if (cpuIndex <= 3.0f)
            {
                minCpu = "Intel Core i5-7400 / AMD Ryzen 3 1200";
                recCpu = "Intel Core i5-11600K / AMD Ryzen 5 5600X";
            }
            else
            {
                minCpu = "Intel Core i5-9400 / AMD Ryzen 5 2600";
                recCpu = "Intel Core i7-12700K / AMD Ryzen 7 5800X";
            }

            // 6. Map VRAM & System RAM
            int calculatedMinVram = Mathf.Max(2, Mathf.CeilToInt(estMemGB * 0.8f));
            int calculatedRecVram = Mathf.Max(4, Mathf.CeilToInt(estMemGB * 1.5f));

            minVram = $"{calculatedMinVram} GB VRAM";
            recVram = $"{calculatedRecVram} GB VRAM";

            minRam = "8 GB RAM";
            recRam = "16 GB RAM";

            // 7. Bottleneck & Optimization Diagnostics
            if (cpuIndex > gpuIndex)
            {
                primaryBottleneck = "CPU Bound (Draw Call Submissions)";
                optimizationTip = "High unbatched draw calls detected. Consider combining static meshes using MeshBaker or enabling Static Batching in your scene objects.";
            }
            else if (sceneTriangles > 300000)
            {
                primaryBottleneck = "GPU Bound (High Poly Density)";
                optimizationTip = "Triangle count is high for standard hardware. Use LODs (Level of Detail) or decimate distant campus buildings.";
            }
            else if (shadowMapRes > 2048)
            {
                primaryBottleneck = "GPU Bound (Shadow Filtering & Resolution)";
                optimizationTip = "High shadow map resolution detected in URP settings. Lowering to 2048 or adjusting shadow cascades will significantly improve minimum GPU performance.";
            }
            else
            {
                primaryBottleneck = "Balanced GPU/CPU Load";
                optimizationTip = "Performance distribution is balanced. Minimum requirements are well-suited for entry-level hardware.";
            }
        }
    }
}
#endif
