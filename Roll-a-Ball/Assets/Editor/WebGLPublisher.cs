using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Roll_a_Ball.EditorTools
{
    /// <summary>
    /// Editor を停止させずに公開用 PowerShell を実行し、進捗を保持する
    /// </summary>
    internal static class WebGLPublisher
    {
        private static Process process;
        private static Task outputTask;
        private static Task errorTask;
        private static readonly ConcurrentQueue<string> messages = new ConcurrentQueue<string>();

        /// <summary>
        /// 公開処理の実行中かを返す
        /// </summary>
        public static bool IsPublishing => process != null;

        /// <summary>
        /// 最新の進捗または完了結果を返す
        /// </summary>
        public static string Status { get; private set; } = string.Empty;

        /// <summary>
        /// 公開失敗を結果表示へ伝える
        /// </summary>
        public static bool Failed { get; private set; }

        /// <summary>
        /// 公開スクリプトとローカルの暗号化 Webhook 設定を検証する
        /// </summary>
        public static bool CanPublish(string repository, string roleId, out string error)
        {
            error = string.Empty;
            if (!Regex.IsMatch(repository ?? string.Empty, @"\A[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+\z"))
            {
                error = "GitHub リポジトリは owner/repository の形式で指定してください。";
                return false;
            }

            if (!Regex.IsMatch(roleId ?? string.Empty, @"\A[0-9]{17,20}\z"))
            {
                error = "roll-a-ball の Discord ロール ID を設定してください。";
                return false;
            }

            if (!File.Exists(ScriptPath) || !File.Exists(SecretPath))
            {
                error = "公開スクリプトまたは Discord Webhook 設定がありません。Docs/WebGLPublishing.md を確認してください。";
                return false;
            }

            return true;
        }

        /// <summary>
        /// ローカルだけに保存する暗号化 Webhook の場所を返す
        /// </summary>
        public static string SecretPath => Path.Combine(Path.GetDirectoryName(Application.dataPath), "UserSettings", "DiscordWebhook.xml");

        /// <summary>
        /// リポジトリに配置した公開スクリプトの場所を返す
        /// </summary>
        private static string ScriptPath => Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Application.dataPath)), "Tools", "PublishWebGL.ps1");

        /// <summary>
        /// 成功済みビルドを公開し、到達確認後に Discord へ通知する
        /// </summary>
        public static void Start(string repository, string roleId)
        {
            if (IsPublishing)
            {
                return;
            }

            if (!CanPublish(repository, roleId, out var error))
            {
                Status = error;
                Failed = true;
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -ExecutionPolicy Bypass -File " + Quote(ScriptPath) +
                    " -OutputDirectory " + Quote(WebGLBuildUtility.OutputDirectory) +
                    " -Repository " + Quote(repository) + " -RoleId " + Quote(roleId),
                WorkingDirectory = Path.GetDirectoryName(ScriptPath),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            // OS のプロセス起動だけは例外を境界で扱い、ビルド結果と区別して表示する
            try
            {
                process = Process.Start(startInfo);
            }
            catch (Win32Exception)
            {
                Status = "PowerShell を起動できませんでした。公開手順を確認してください。";
                Failed = true;
                return;
            }

            Failed = false;
            Status = "WebGL をアップロードしています…";
            outputTask = ReadMessages(process.StandardOutput);
            errorTask = ReadMessages(process.StandardError);
            EditorApplication.update += Poll;
            AssemblyReloadEvents.beforeAssemblyReload += StopBeforeReload;
        }

        /// <summary>
        /// シェルを介さない Windows プロセス引数を引用する
        /// </summary>
        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"").TrimEnd('\\') + "\"";
        }

        /// <summary>
        /// バックグラウンドで出力を読み、Editor 用キューへ格納する
        /// </summary>
        private static async Task ReadMessages(StreamReader reader)
        {
            while (true)
            {
                var line = await reader.ReadLineAsync();
                if (line == null)
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(line))
                {
                    messages.Enqueue(line);
                }
            }
        }

        /// <summary>
        /// メインスレッドで進捗と終了状態を反映する
        /// </summary>
        private static void Poll()
        {
            while (messages.TryDequeue(out var message))
            {
                Status = message;
            }

            if (!process.HasExited || !outputTask.IsCompleted || !errorTask.IsCompleted)
            {
                return;
            }

            Failed = process.ExitCode != 0 || outputTask.IsFaulted || errorTask.IsFaulted;
            if (Failed)
            {
                Status = "公開または通知に失敗しました: " + Status;
            }

            ReleaseProcess();
        }

        /// <summary>
        /// スクリプト再読み込みで中断する公開プロセスを終了する
        /// </summary>
        private static void StopBeforeReload()
        {
            if (!process.HasExited)
            {
                process.Kill();
            }

            ReleaseProcess();
        }

        /// <summary>
        /// 公開プロセスと Editor イベントの寿命を揃える
        /// </summary>
        private static void ReleaseProcess()
        {
            EditorApplication.update -= Poll;
            AssemblyReloadEvents.beforeAssemblyReload -= StopBeforeReload;
            process.Dispose();
            process = null;
        }
    }
}
