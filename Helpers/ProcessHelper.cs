using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace ProcessExplorerPro.Helpers
{
    public static class ProcessHelper
    {
        public static (bool IsSigned, string Publisher, string CertificateDetails) GetDigitalSignature(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return (false, "Unsigned", "File not found");
            }

            try
            {
                var cert2 = X509CertificateLoader.LoadCertificateFromFile(filePath);
                string publisher = cert2.SubjectName.Name;
                
                // Clean up publisher string (e.g. CN=Microsoft Corporation, O=Microsoft Corporation, etc.)
                if (publisher.Contains("CN="))
                {
                    int start = publisher.IndexOf("CN=") + 3;
                    int end = publisher.IndexOf(',', start);
                    if (end > start)
                    {
                        publisher = publisher.Substring(start, end - start);
                    }
                    else
                    {
                        publisher = publisher.Substring(start);
                    }
                }

                return (true, publisher, $"Issuer: {cert2.IssuerName.Name}, Serial: {cert2.SerialNumber}");
            }
            catch
            {
                return (false, "Unsigned", "No valid digital signature found");
            }
        }

        public static List<string> GetLoadedModules(int processId)
        {
            var modules = new List<string>();
            try
            {
                using var process = Process.GetProcessById(processId);
                foreach (ProcessModule module in process.Modules)
                {
                    if (module.FileName != null)
                        modules.Add(module.FileName);
                }
            }
            catch (Exception ex)
            {
                modules.Add($"Access Denied ({ex.Message}). Run as Administrator to view dlls.");
            }
            return modules;
        }

        public static List<ProcessThreadInfo> GetProcessThreads(int processId)
        {
            var threads = new List<ProcessThreadInfo>();
            try
            {
                using var process = Process.GetProcessById(processId);
                foreach (ProcessThread thread in process.Threads)
                {
                    threads.Add(new ProcessThreadInfo
                    {
                        Id = thread.Id,
                        State = thread.ThreadState.ToString(),
                        Priority = thread.PriorityLevel.ToString(),
                        StartTime = thread.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        BasePriority = thread.BasePriority,
                        CurrentPriority = thread.CurrentPriority
                    });
                }
            }
            catch (Exception ex)
            {
                threads.Add(new ProcessThreadInfo
                {
                    Id = -1,
                    State = "Access Denied",
                    Priority = ex.Message,
                    StartTime = "N/A"
                });
            }
            return threads;
        }
    }

    public class ProcessThreadInfo
    {
        public int Id { get; set; }
        public string State { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public int BasePriority { get; set; }
        public int CurrentPriority { get; set; }
    }
}
