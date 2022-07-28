using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace TwitterSharp.WpfClient.Helper;

public static class ConfigHelper
{
    public static T GetValue<T>(string propertyName, T defaultValue = default)
    {
        var filePath = $"{GetMainFolder()}{propertyName}.json";

        if (!File.Exists(filePath))
        {
            return defaultValue;
        }

        using (var isoStream = new StreamReader(filePath))
        {
            var returnValue = JsonSerializer.Deserialize<T>(isoStream.ReadToEnd());

            if (returnValue != null)
                return returnValue;

            return defaultValue;
        }
    }

    public static void SetValue<T>(ref T outValue, T value, [CallerMemberName] string propertyName = "")
    {
        var filePath = $"{GetMainFolder()}{propertyName}.json";

        string jsonString = JsonSerializer.Serialize<T>(value);
        File.WriteAllText(filePath, jsonString);

        outValue = value;
    }

    public static string GetMainFolder()
    {
        var path = AppDomain.CurrentDomain.BaseDirectory + "config" + "\\";

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        return path;
    }
}