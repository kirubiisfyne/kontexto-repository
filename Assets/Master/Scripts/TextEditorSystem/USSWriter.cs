#if UNITY_EDITOR // This prevents the script from breaking your final game build
using System.IO;
using UnityEditor;
using UnityEngine;

public static class USSWriter
{
    // A simple method to append a new CSS class to an existing USS file
    public static void WriteStyleToUSS(string relativeFilePath, string className, string cssRules)
    {
        // 1. Get the absolute path on your hard drive (e.g., C:/Projects/YourGame/Assets/...)
        string fullPath = Path.Combine(Application.dataPath, relativeFilePath);

        // 2. Format the string exactly how it should look in the text file
        string newRule = $"\n.{className} {{\n    {cssRules}\n}}\n";

        // 3. Write it to the file. 
        // File.AppendAllText creates the file if it doesn't exist, or adds to the bottom if it does.
        File.AppendAllText(fullPath, newRule);

        // 4. CRITICAL: Tell Unity that a file on the hard drive changed, 
        // otherwise the Editor won't load the new styles until you click away and click back.
        AssetDatabase.Refresh();
        
        Debug.Log($"Successfully wrote .{className} to {relativeFilePath}");
    }
}
#endif