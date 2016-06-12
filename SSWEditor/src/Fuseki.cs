using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;

namespace SSWEditor
{
    class Fuseki
    {
        public static void Start(bool showFusekiConsole)
        {
            Stop();
            var arguments = new List<string>
            {
                "-Xmx1200M",
                "-jar fuseki/fuseki-server.jar",
                "--update",
                "--port=" + MainForm.config.FusekiPort,
                "--pages fuseki/pages",
                "--loc \"" + MainForm.DocumentRoot + "\"",
                "/ds"
            };

            var installPath = MainForm.GetJavaInstallationPath();
            var javaPath = Path.Combine(installPath, "bin\\Java.exe");

            var processInfo = new ProcessStartInfo(javaPath, string.Join(" ", arguments))
                                  {
                                      CreateNoWindow = true,
                                      UseShellExecute = showFusekiConsole
                                  };
            var proc = Process.Start(processInfo);
            if (proc == null)
            {
                throw new Exception("error during staring fuseki");
            }
        }

        public static void Stop()
        {
            const string query = "SELECT ProcessId "
                                 + "FROM Win32_Process "
                                 + "WHERE Name = 'java.exe' "
                                 + "AND CommandLine LIKE '%fuseki-server.jar%'";

            List<Process> servers = null;
            using (var results = new ManagementObjectSearcher(query).Get())
                servers = results.Cast<ManagementObject>()
                                 .Select(mo => Process.GetProcessById((int)(uint)mo["ProcessId"]))
                                 .ToList();

            foreach (var process in servers)
            {
                process.Kill();
            }
        }
    }
}
