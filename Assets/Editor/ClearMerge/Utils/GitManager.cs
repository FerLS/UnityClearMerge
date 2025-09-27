using UnityEngine;
using System.Diagnostics;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using Debug = UnityEngine.Debug;
using ClearMerge.Scenes;

namespace ClearMerge.Utils
{
    /// <summary>
    /// Git integration manager for ClearMerge - handles Git operations and conflict detection
    /// </summary>
    public static class GitManager
    {

        #region Git Operations
        public static void PushChanges()
        {
            try
            {
                string result = RunGitCommand("push");
                Debug.Log("Git push result: " + result);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("fatal:") || e.Message.Contains("error:"))
                {
                    Debug.LogError("Git push error: " + e.Message);
                }
                else
                {
                    Debug.Log("Git push result: " + e.Message);
                }
            }
        }
        #endregion
        /// <summary>
        /// Manages the complete commit and push process for a resolved scene
        /// </summary>
        public static bool CommitAndPushResolvedScene(string scenePath, bool isNewScene, string newSceneName = null)
        {
            string effectivePath = scenePath;
            string commitMessage = $"Resolved scene conflicts in {Path.GetFileName(scenePath)}";

            // If it's a new scene, adjust the path
            if (isNewScene && !string.IsNullOrEmpty(newSceneName))
            {
                string directory = Path.GetDirectoryName(scenePath);
                effectivePath = Path.Combine(directory, newSceneName);
            }

            // Handle branching strategies
            try
            {
                // For now, just commit directly - branching can be added later if needed
                RunGitCommand($"add \"{effectivePath}\"");
                string result = RunGitCommand($"commit -m \"{commitMessage}\"");

                if (!string.IsNullOrEmpty(result) && !result.Contains("nothing to commit"))
                {
                    PushChanges();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error committing resolved scene: {ex.Message}");
                return false;
            }
        }

        public static void StageFileInGit(string assetPath)
        {
            try
            {
                string repoRoot = Application.dataPath.Replace("/Assets", "").Replace("\\Assets", "");
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"add \"{assetPath}\"",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = repoRoot
                };
                using var proc = System.Diagnostics.Process.Start(psi);
                proc.WaitForExit(2000);
                if (proc.ExitCode != 0)
                {
                    var err = proc.StandardError.ReadToEnd();
                    Debug.LogWarning($"git add failed: {err}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"git add exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Ejecuta `git status --porcelain` y devuelve la lista de rutas
        /// de archivos en conflicto (estados AA o UU).
        /// </summary>
        public static List<string> ScanConflictsWithGit()
        {
            var result = new List<string>();
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "status --porcelain",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Application.dataPath
                                             .Replace("/Assets", "")
                                             .Replace("\\Assets", "")
                };

                using (var proc = System.Diagnostics.Process.Start(psi))
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();
                    if (proc.ExitCode != 0)
                    {
                        string err = proc.StandardError.ReadToEnd();
                        Debug.LogWarning($"Git status failed: {err}");
                        return result;
                    }

                    foreach (var line in output
                             .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        // solo AA (both added) o UU (both modified)
                        if (line.StartsWith("AA ") || line.StartsWith("UU "))
                        {
                            var path = line.Substring(3).Trim();
                            result.Add(path);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not check Git status for conflicts: {e.Message}");
            }
            return result;
        }

        #region Branch Management
        public static string GetCurrentBranch()
        {
            try
            {
                string result = RunGitCommand("rev-parse --abbrev-ref HEAD");
                return result.Trim();
            }
            catch (Exception e)
            {
                Debug.LogError("Git branch check error: " + e.Message);
                return "unknown";
            }
        }
        #endregion

        #region Conflict Detection


        public static string GetFileContent(string filePath, string revision = "HEAD")
        {
            try
            {
                string result = RunGitCommand($"show {revision}:{filePath}");
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"Git file content error for {filePath}: {e.Message}");
                return "";
            }
        }

        #endregion


        #region Core Git Command Execution
        private static string RunGitCommand(string arguments)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "git";
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
                {
                    throw new Exception($"Git command failed: {error}");
                }

                return output;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error executing git command '{arguments}': {e.Message}");
                throw;
            }
        }


        #endregion




    }
}