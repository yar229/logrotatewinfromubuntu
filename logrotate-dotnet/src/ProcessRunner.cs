using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace LogRotate
{
    /// <summary>
    /// Result of running an external process: exit code and captured stderr.
    /// </summary>
    public sealed class ProcessResult
    {
        public int ExitCode;
        public string StdErr = string.Empty;
        public string StdOut = string.Empty;
    }

    /// <summary>
    /// Runs scripts and external commands. On Windows scripts are executed
    /// through cmd.exe (equivalent of '/bin/sh -c' on Linux).
    /// </summary>
    public static class ProcessRunner
    {
        /// <summary>
        /// Executes an executable with arguments. Optionally captures stderr
        /// and returns it in the result instead of forwarding to console.
        /// </summary>
        public static ProcessResult Run(string fileName, IList<string> arguments,
                                        bool redirectStdErr, string? stdinFile = null,
                                        IDictionary<string, string>? env = null)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
                RedirectStandardError = redirectStdErr,
                RedirectStandardOutput = false,
                CreateNoWindow = true,
            };

            foreach (var a in arguments)
                psi.ArgumentList.Add(a);

            if (stdinFile != null)
            {
                psi.RedirectStandardInput = true;
            }

            if (env != null)
            {
                foreach (var kv in env)
                    psi.Environment[kv.Key] = kv.Value;
            }

            var result = new ProcessResult();
            try
            {
                using (var proc = Process.Start(psi)!)
                {
                    if (redirectStdErr)
                    {
                        result.StdErr = proc.StandardError.ReadToEnd();
                    }
                    if (stdinFile != null)
                    {
                        using (var stdin = proc.StandardInput)
                        using (var reader = File.OpenRead(stdinFile))
                        {
                            reader.CopyTo(stdin.BaseStream);
                        }
                    }
                    proc.WaitForExit();
                    result.ExitCode = proc.ExitCode;
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                result.ExitCode = -1;
                result.StdErr = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Runs a shell script via cmd.exe on Windows (sh -c equivalent).
        /// </summary>
        //public static int RunScript(string script, string logFn, string? logRotFn)
        //{
        //    var psi = new ProcessStartInfo
        //    {
        //        FileName = Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe",
        //        UseShellExecute = false,
        //        RedirectStandardError = false,
        //        RedirectStandardOutput = false,
        //        CreateNoWindow = false,
        //    };

        //    // On Windows, cmd.exe has no direct positional args in the way
        //    // /bin/sh does, so we export the filenames as env vars.
        //    psi.Environment["LOGROTATE_LOG"] = logFn;
        //    if (logRotFn != null)
        //        psi.Environment["LOGROTATE_LOGROTATED"] = logRotFn;

        //    psi.ArgumentList.Add("/d");
        //    psi.ArgumentList.Add("/s");
        //    psi.ArgumentList.Add("/c");
        //    script = script.Replace("\n", " ^\n");
        //    psi.ArgumentList.Add($"\"{script}\"\r\n");

        //    try
        //    {
        //        using var proc = Process.Start(psi)!;
        //        proc.WaitForExit();
        //        return proc.ExitCode;
        //    }
        //    catch (System.ComponentModel.Win32Exception)
        //    {
        //        return -1;
        //    }
        //}

        public static int RunScript(string script, string logFn, string? logRotFn)
        {
            string temp_path_orig = Path.GetTempFileName();
            string temp_path = Path.ChangeExtension(temp_path_orig, "cmd");
            File.Delete(temp_path_orig);
            try
            {
                File.WriteAllText(temp_path, script);
            }
            catch (Exception e)
            {
                return 1;
            }


            var psi = new ProcessStartInfo(Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe",
                "/S /C \"\"" + temp_path + "\" \"" + logFn + "\"\"")
            {
                UseShellExecute = false,
                RedirectStandardError = false,
                RedirectStandardOutput = false,
                CreateNoWindow = false,
            };

            // On Windows, cmd.exe has no direct positional args in the way
            // /bin/sh does, so we export the filenames as env vars.
            psi.Environment["LOGROTATE_LOG"] = logFn;
            if (logRotFn != null)
                psi.Environment["LOGROTATE_LOGROTATED"] = logRotFn;

            //psi.ArgumentList.Add("/d");
            //psi.ArgumentList.Add("/s");
            //psi.ArgumentList.Add("/c");
            //script = script.Replace("\n", " ^\n");
            //psi.ArgumentList.Add($"\"{script}\"\r\n");

            try
            {
                using var proc = Process.Start(psi)!;
                proc.WaitForExit();
                return proc.ExitCode;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return -1;
            }
        }
    }
}