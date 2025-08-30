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
            // ����Ҫɨ�����Ƶ�ļ���·��
            string[] audioFolders =
            {
                "Assets/Audio/BGM",
                "Assets/Audio/SFX",
                // "Assets/Audio/UI",
                // "Assets/Audio/Ambience",
            };

            // �������Ŀ¼
            string outputDirectory = "Assets/Audio/AudioRegistry";
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // ɨ��ÿ���ļ��в����ɶ�Ӧ����
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

            // ˢ��AssetDatabase��ȷ�����ļ���ʶ��
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
        // ��·������ȡ���һ���ļ�������Ϊ����
        string folderName = Path.GetFileName(path);
        // ȷ����������Ч��C#��ʶ��
        return MakeValidIdentifier(folderName);
    }

    private static string GenerateAudioClass(string folderPath, string className)
    {
        StringBuilder sb = new StringBuilder();

        // ����ļ�ͷ
        sb.AppendLine("// �Զ������� - ��Ҫ�ֶ��޸�");
        sb.AppendLine("// �ļ���Դ: " + folderPath);
        sb.AppendLine("// ��������: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();

        // ��������ռ�
        sb.AppendLine("namespace AudioRegistry");
        sb.AppendLine("{");

        // ��ʼ�ඨ��
        sb.AppendLine($"    public static class {className}");
        sb.AppendLine("    {");

        // ɨ���ļ����е���Ƶ�ļ�
        string[] audioFiles = Directory
            .GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
            .Where(file => IsAudioFile(file))
            .ToArray();

        // Ϊÿ����Ƶ�ļ����ɳ���
        foreach (string filePath in audioFiles)
        {
            try
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string constantName = MakeValidIdentifier(fileName);
                string relativePath = GetRelativePath(filePath, folderPath);

                sb.AppendLine($"        public const string {constantName} = \"{relativePath}\";");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing file {filePath}: {ex.Message}");
            }
        }

        // �����ඨ��
        sb.AppendLine("    }");

        // ���������ռ�
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
            // ȷ��·��ʹ��ͳһ�ķָ���
            fullPath = fullPath.Replace('\\', '/');
            basePath = basePath.Replace('\\', '/');

            // ȷ����·���Էָ�����β
            if (!basePath.EndsWith("/"))
                basePath += "/";

            // �������·���Ƿ��Ի�·����ͷ
            if (!fullPath.StartsWith(basePath))
            {
                Debug.LogWarning($"Path '{fullPath}' does not start with base path '{basePath}'");
                return Path.GetFileName(fullPath); // ���˵�ֻ�����ļ���
            }

            // ��ȡ���·��
            string relativePath = fullPath.Substring(basePath.Length);
            return relativePath;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error getting relative path: {ex.Message}");
            return Path.GetFileName(fullPath); // ���˵�ֻ�����ļ���
        }
    }

    private static string MakeValidIdentifier(string input)
    {
        // �Ƴ���Ч�ַ�������ת��Ϊ��Ч��C#��ʶ��
        StringBuilder sb = new StringBuilder();

        // �����һ���ַ� - ��������ĸ���»���
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

        // ����ʣ���ַ�
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
