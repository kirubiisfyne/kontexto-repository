using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Master.Scripts.CutsceneSystem
{
    /// <summary>
    /// Root object for cutscene dialogue JSON mapping.
    /// Supports multiple lines per shot: intro[i] or outro[i] contains a list of lines for shot index i.
    /// </summary>
    [Serializable]
    public class CutsceneDialogueData
    {
        public List<List<string>> intro = new List<List<string>>();
        public List<List<string>> outro = new List<List<string>>();

        /// <summary>
        /// Retrieves the list of lines for the specified shot index.
        /// </summary>
        public List<string> GetLinesForShot(bool isIntro, int shotIndex)
        {
            var list = isIntro ? intro : outro;
            if (list != null && shotIndex >= 0 && shotIndex < list.Count)
            {
                return list[shotIndex];
            }
            return null;
        }

        /// <summary>
        /// Flexible parser supporting both arrays of line arrays and arrays of single strings.
        /// </summary>
        public static CutsceneDialogueData Parse(string jsonContent)
        {
            var data = new CutsceneDialogueData();
            if (string.IsNullOrEmpty(jsonContent)) return data;

            var root = JObject.Parse(jsonContent);
            ParseSection(root["intro"], data.intro);
            ParseSection(root["outro"], data.outro);

            return data;
        }

        private static void ParseSection(JToken token, List<List<string>> targetList)
        {
            if (token == null || !token.HasValues) return;

            foreach (var item in token)
            {
                if (item is JArray arr)
                {
                    var lines = new List<string>();
                    foreach (var lineToken in arr)
                    {
                        lines.Add(lineToken.ToString());
                    }
                    targetList.Add(lines);
                }
                else
                {
                    // Fallback for single string item
                    targetList.Add(new List<string> { item.ToString() });
                }
            }
        }
    }
}
