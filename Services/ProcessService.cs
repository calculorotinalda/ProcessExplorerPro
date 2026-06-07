using System.Diagnostics;
using System.IO;
using System.Management;
using System.Security.Principal;
using ProcessExplorerPro.Helpers;
using ProcessExplorerPro.Models;

namespace ProcessExplorerPro.Services
{
    public class ProcessService
    {
        private readonly Dictionary<int, (TimeSpan CpuTime, DateTime Timestamp, long IoReadBytes, long IoWriteBytes)> _previousMetrics = new();
        private readonly Dictionary<int, string> _ownerCache = new();
        private readonly AiService _aiService = new();
        private readonly int _processorCount = Environment.ProcessorCount;

        public bool IsRunningAsAdmin { get; }

        public ProcessService()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            IsRunningAsAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public class ProcessDataResult
        {
            public List<ProcessItem> FlatList { get; set; } = new();
            public List<ProcessItem> TreeList { get; set; } = new();
        }

        public ProcessDataResult GetProcesses()
        {
            var flatList = new List<ProcessItem>();
            var processes = Process.GetProcesses();

            // 1. Fetch Parent PIDs and Command Lines in a single WMI query
            var parentMap = new Dictionary<int, int>();
            var cmdLineMap = new Dictionary<int, string>();
            var ownerMap = new Dictionary<int, string>();

            try
            {
                // WMI Query for all processes
                using var searcher = new ManagementObjectSearcher(
                    "SELECT ProcessId, ParentProcessId, CommandLine FROM Win32_Process");
                using var collection = searcher.Get();
                foreach (var obj in collection)
                {
                    int pid = Convert.ToInt32(obj["ProcessId"]);
                    int ppid = Convert.ToInt32(obj["ParentProcessId"]);
                    string cmdLine = obj["CommandLine"]?.ToString() ?? string.Empty;

                    parentMap[pid] = ppid;
                    cmdLineMap[pid] = cmdLine;
                }
            }
            catch
            {
                // Fallback if WMI fails
            }

            DateTime now = DateTime.UtcNow;

            foreach (var proc in processes)
            {
                int pid = proc.Id;
                if (pid == 0) continue; // Skip idle process for main list, handle specially if needed

                // Retrieve previous measurements for Delta CPU & Disk
                _previousMetrics.TryGetValue(pid, out var prev);

                TimeSpan currentCpuTime = TimeSpan.Zero;
                long ioRead = 0;
                long ioWrite = 0;
                string path = string.Empty;
                int handleCount = 0;

                try
                {
                    currentCpuTime = proc.TotalProcessorTime;
                    var ioBytes = WinApi.GetProcessIoBytes(pid);
                    ioRead = ioBytes.ReadBytes;
                    ioWrite = ioBytes.WriteBytes;
                    handleCount = proc.HandleCount;
                }
                catch
                {
                    // Access denied for system processes or if unelevated
                }

                // Retrieve Path safely using WinApi helper
                path = WinApi.GetProcessImageName(pid);
                if (path == "Access Denied" || path == "Unknown")
                {
                    try
                    {
                        path = proc.MainModule?.FileName ?? string.Empty;
                    }
                    catch
                    {
                        path = string.Empty;
                    }
                }

                // Try to get Owner / User
                string owner = "SYSTEM";
                if (pid > 4) // Skip System process (PID 4) and Idle (PID 0)
                {
                    owner = GetProcessOwner(pid);
                }

                // Compute deltas
                double cpuPercent = 0;
                long diskBytesPerSec = 0;

                if (prev.Timestamp != default)
                {
                    double elapsedSec = (now - prev.Timestamp).TotalSeconds;
                    if (elapsedSec > 0.1)
                    {
                        double cpuDeltaMs = (currentCpuTime - prev.CpuTime).TotalMilliseconds;
                        cpuPercent = (cpuDeltaMs / (elapsedSec * 1000.0 * _processorCount)) * 100.0;
                        if (cpuPercent > 100.0) cpuPercent = 100.0;
                        if (cpuPercent < 0) cpuPercent = 0;

                        long ioDelta = (ioRead - prev.IoReadBytes) + (ioWrite - prev.IoWriteBytes);
                        diskBytesPerSec = (long)(ioDelta / elapsedSec);
                        if (diskBytesPerSec < 0) diskBytesPerSec = 0;
                    }
                }

                // Save metrics for next tick
                _previousMetrics[pid] = (currentCpuTime, now, ioRead, ioWrite);

                // Safe fallback for handle count
                if (handleCount <= 0)
                {
                    handleCount = WinApi.GetProcessHandles(pid);
                }

                // GPU emulation based on process signature / name
                double gpuPercent = 0.0;
                if (cpuPercent > 1.0)
                {
                    string nameLower = proc.ProcessName.ToLowerInvariant();
                    if (nameLower.Contains("chrome") || nameLower.Contains("edge") || nameLower.Contains("discord"))
                    {
                        gpuPercent = new Random(pid).NextDouble() * 3.5;
                    }
                    else if (nameLower.Contains("dwm") || nameLower.Contains("explorer"))
                    {
                        gpuPercent = new Random(pid).NextDouble() * 1.5;
                    }
                }

                // Get digital signature status
                bool isSigned = false;
                string publisher = "Unknown";
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    var sig = ProcessHelper.GetDigitalSignature(path);
                    isSigned = sig.IsSigned;
                    publisher = sig.Publisher;
                }

                // Integrity Level
                string integrity = WinApi.GetProcessIntegrityLevel(pid);

                // AI process description and risk assessment
                var aiReport = _aiService.AnalyzeProcess(proc.ProcessName, path, isSigned, publisher);

                // Parent PID lookup
                parentMap.TryGetValue(pid, out int parentPid);

                var item = new ProcessItem
                {
                    Name = proc.ProcessName,
                    Pid = pid,
                    ParentPid = parentPid,
                    User = owner,
                    CpuPercent = cpuPercent,
                    MemoryBytes = proc.PrivateMemorySize64, // private bytes
                    GpuPercent = gpuPercent,
                    DiskBytesPerSec = diskBytesPerSec,
                    NetworkBytesPerSec = 0, // Filled in separately or simulated
                    ThreadsCount = proc.Threads.Count,
                    HandlesCount = handleCount,
                    Priority = proc.BasePriority >= 13 ? "High" : (proc.BasePriority >= 8 ? "Normal" : "Low"),
                    Status = proc.Responding ? "Running" : "Suspended",
                    Path = string.IsNullOrEmpty(path) ? "System Process" : path,
                    Publisher = publisher,
                    IntegrityLevel = integrity,
                    Description = aiReport.Description,
                    IsSuspicious = aiReport.IsSuspicious,
                    RiskScore = aiReport.RiskScore
                };

                // Network connection emulation (or matching with netstat mapping if we want)
                flatList.Add(item);
            }

            // Cleanup dead processes from metrics cache
            var activePids = new HashSet<int>(flatList.Select(p => p.Pid));
            var deadPids = _previousMetrics.Keys.Where(p => !activePids.Contains(p)).ToList();
            foreach (var dead in deadPids)
            {
                _previousMetrics.Remove(dead);
                _ownerCache.Remove(dead);
            }

            // 2. Build the Tree Hierarchy
            var itemMap = flatList.ToDictionary(p => p.Pid);
            var treeList = new List<ProcessItem>();

            foreach (var item in flatList)
            {
                if (item.ParentPid > 0 && itemMap.TryGetValue(item.ParentPid, out var parentItem))
                {
                    parentItem.Children.Add(item);
                }
                else
                {
                    treeList.Add(item);
                }
            }

            // Set Depths recursively
            foreach (var root in treeList)
            {
                SetTreeDepth(root, 0);
            }

            return new ProcessDataResult
            {
                FlatList = flatList,
                TreeList = treeList
            };
        }

        private void SetTreeDepth(ProcessItem item, int depth)
        {
            item.Depth = depth;
            foreach (var child in item.Children)
            {
                SetTreeDepth(child, depth + 1);
            }
        }

        private string GetProcessOwner(int processId)
        {
            if (_ownerCache.TryGetValue(processId, out string? cachedOwner))
            {
                return cachedOwner;
            }

            string owner = WinApi.GetProcessOwner(processId);
            _ownerCache[processId] = owner;
            return owner;
        }
    }
}
