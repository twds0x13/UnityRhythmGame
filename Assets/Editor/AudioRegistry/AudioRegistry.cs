using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AudioScannerGenerator
{
    [MenuItem("Tools/Audio/Scan Audio Folders and Generate Classes")]
    public static void ScanAndGenerateAudioClasses()
    {
        try
        {
            // 定义要扫描的音频文件夹路径
            string[] audioFolders =
            {
                "Assets/Audio/BGM",
                "Assets/Audio/SFX",
                // "Assets/Audio/UI",
                // "Assets/Audio/Ambience",
            };

            // 创建输出目录
            string outputDirectory = "Assets/Audio/AudioRegistry";
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // 扫描每个文件夹并生成对应的类
            foreach (string folder in audioFolders)
            {
                if (Directory.Exists(folder))
                {
                    string className = GetClassNameFromPath(folder);
                    string code = GenerateAudioClass(folder, className);

                    string filePath = Path.Combine(outputDirectory, $"{className}.cs");
                    File.WriteAllText(filePath, code);

                    Debug.Log($"Generated {className} from {folder}");
                }
                else
                {
                    Debug.LogWarning($"Folder does not exist: {folder}");
                }
            }

            // 刷新AssetDatabase以确保新文件被识别
            AssetDatabase.Refresh();

            Debug.Log("Audio class generation completed!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in audio generation: {ex.Message}");
        }
    }

    private static string GetClassNameFromPath(string path)
    {
        // 从路径中提取最后一个文件夹名作为类名
        string folderName = Path.GetFileName(path);
        // 确保类名是有效的C#标识符
        return MakeValidIdentifier(folderName);
    }

    private static string GenerateAudioClass(string folderPath, string className)
    {
        StringBuilder sb = new StringBuilder();

        // 添加文件头
        sb.AppendLine("// 自动生成类 - 不要手动修改");
        sb.AppendLine("// 文件夹源: " + folderPath);
        sb.AppendLine("// 生成日期: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();

        // 添加命名空间
        sb.AppendLine("namespace AudioRegistry");
        sb.AppendLine("{");

        // 开始类定义
        sb.AppendLine($"    public static class {className}");
        sb.AppendLine("    {");

        // 扫描文件夹中的音频文件
        string[] audioFiles = Directory
            .GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
            .Where(file => IsAudioFile(file))
            .ToArray();

        // 为每个音频文件生成常量
        foreach (string filePath in audioFiles)
        {
            try
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string constantName = MakeValidIdentifier(fileName);
                // string relativePath = GetRelativePath(filePath, folderPath);

                sb.AppendLine($"        public const string {constantName} = \"{constantName}\";");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing file {filePath}: {ex.Message}");
            }
        }

        // 结束类定义
        sb.AppendLine("    }");

        // 结束命名空间
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static bool IsAudioFile(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        return extension == ".wav"
            || extension == ".mp3"
            || extension == ".ogg"
            || extension == ".aiff"
            || extension == ".aif"
            || extension == ".flac";
    }

    private static string GetRelativePath(string fullPath, string basePath)
    {
        try
        {
            // 确保路径使用统一的分隔符
            fullPath = fullPath.Replace('\\', '/');
            basePath = basePath.Replace('\\', '/');

            // 确保基路径以分隔符结尾
            if (!basePath.EndsWith("/"))
                basePath += "/";

            // 检查完整路径是否以基路径开头
            if (!fullPath.StartsWith(basePath))
            {
                Debug.LogWarning($"Path '{fullPath}' does not start with base path '{basePath}'");
                return Path.GetFileName(fullPath); // 回退到只返回文件名
            }

            // 提取相对路径
            string relativePath = fullPath.Substring(basePath.Length);
            return relativePath;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error getting relative path: {ex.Message}");
            return Path.GetFileName(fullPath); // 回退到只返回文件名
        }
    }

    private static string MakeValidIdentifier(string input)
    {
        // 移除无效字符并将其转换为有效的C#标识符
        StringBuilder sb = new StringBuilder();

        // 处理第一个字符 - 必须是字母或下划线
        if (input.Length > 0)
        {
            if (char.IsLetter(input[0]) || input[0] == '_')
            {
                sb.Append(input[0]);
            }
            else
            {
                sb.Append('_');
            }
        }

        // 处理剩余字符
        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsLetterOrDigit(input[i]) || input[i] == '_')
            {
                sb.Append(input[i]);
            }
            else
            {
                sb.Append('_');
            }
        }

        return sb.ToString();
    }
}
