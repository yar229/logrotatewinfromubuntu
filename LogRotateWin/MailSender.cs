using LogRotate.Consts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace LogRotate;

internal static class MailSender
{
    public static int MailLogWrapper(string mailFilename, string mailCommand,
                                  int logNum, LogInfo log)
    {
        var result = ProcessRunner.RunScript(mailCommand, mailFilename, null, 
            (EnviromentVariables.MailTo, log.LogAddress));
        return result;
    }

    public static int MailLogWrapperOriginal(string mailFilename, string mailCommand,
                                      int logNum, LogInfo log)
    {
        string? uncompressProg = (log.Flags & LogFlags.Compress) != 0
            ? log.UncompressProg : null;

        string subject = mailFilename;
        if ((log.Flags & LogFlags.MailFirst) != 0)
        {
            if ((log.Flags & LogFlags.DelayCompress) != 0)
                uncompressProg = null;
            if (uncompressProg != null)
                subject = log.Files[logNum];
        }

        return MailLog(log, mailFilename, mailCommand, uncompressProg,
                       log.LogAddress!, subject);
    }

    /// <summary>
    /// Port of mailLog(): optionally decompress into a pipe feeding the mail
    /// command "mail -s subject address".
    /// </summary>
    private static int MailLog(LogInfo log, string logFile, string mailCommand,
                               string? uncompress, string address, string subject)
    {
        FileStream mailInput;
        try
        {
            mailInput = new FileStream(logFile, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
        }
        catch (Exception ex)
        {
            Log.Message(MESS.ERROR, "failed to open {0} for mailing: {1}\n", logFile, ex.Message);
            return 1;
        }

        int rc = 0;
        int uncompressRc = 0;
        using (mailInput)
        using (var mail = new Process())
        {
            mail.StartInfo = new ProcessStartInfo
            {
                FileName = mailCommand,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
            };
            mail.StartInfo.ArgumentList.Add("-s");
            mail.StartInfo.ArgumentList.Add(subject);
            mail.StartInfo.ArgumentList.Add(address);

            try
            {
                mail.Start();
            }
            catch (Exception ex)
            {
                Log.Message(MESS.ERROR, "cannot execute mail command: {0}\n", ex.Message);
                return 1;
            }

            if (uncompress == null)
            {
                var feed = TaskHelper.Run(() =>
                {
                    using var src = mailInput;
                    using var dst = mail.StandardInput.BaseStream;
                    src.CopyTo(dst);
                });
                feed.GetAwaiter().GetResult();
            }
            else
            {
                using (var up = new Process())
                {
                    up.StartInfo = new ProcessStartInfo
                    {
                        FileName = uncompress,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                    };
                    try
                    {
                        up.Start();
                    }
                    catch (Exception ex)
                    {
                        Log.Message(MESS.ERROR, "cannot execute uncompress command: {0}\n", ex.Message);
                        return 1;
                    }

                    /* pump: logFile -> uncompress stdin
                     *       uncompress stdout -> mail stdin */
                    var feed = TaskHelper.Run(() =>
                    {
                        using var src = mailInput;
                        using var dst = up.StandardInput.BaseStream;
                        src.CopyTo(dst);
                    });
                    var pump = TaskHelper.Run(() =>
                    {
                        using var src = up.StandardOutput.BaseStream;
                        using var dst = mail.StandardInput.BaseStream;
                        src.CopyTo(dst);
                    });
                    feed.GetAwaiter().GetResult();
                    up.StandardInput.Close();
                    pump.GetAwaiter().GetResult();
                    up.WaitForExit();
                    uncompressRc = up.ExitCode;
                }
            }

            mail.StandardInput.Close();
            mail.WaitForExit();
            rc = 0;

            if (mail.ExitCode != 0)
            {
                Log.Message(MESS.ERROR, "mail command failed for {0}\n", logFile);
                rc = 1;
            }
            if (uncompress != null && uncompressRc != 0)
            {
                Log.Message(MESS.ERROR, "uncompress command failed mailing {0}\n", logFile);
                rc = 1;
            }
        }
        return rc;
    }
}