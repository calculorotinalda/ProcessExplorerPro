using System.IO;
using System.Text;
using ProcessExplorerPro.Models;

namespace ProcessExplorerPro.Services
{
    public static class ExportService
    {
        public static void ExportToCsv(IEnumerable<ProcessItem> items, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Name,PID,User,CPU %,Memory,GPU %,Disk I/O,Network I/O,Threads,Handles,Priority,Status,Path");

            foreach (var item in items)
            {
                sb.AppendLine($"\"{item.Name}\",{item.Pid},\"{item.User}\",{item.CpuPercent:F2},\"{item.MemoryString}\",{item.GpuPercent:F1},\"{item.DiskString}\",\"{item.NetworkString}\",{item.ThreadsCount},{item.HandlesCount},\"{item.Priority}\",\"{item.Status}\",\"{item.Path.Replace("\"", "\"\"")}\"");
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        public static void ExportToJson(IEnumerable<ProcessItem> items, string filePath)
        {
            var list = items.Select(i => new
            {
                i.Name,
                i.Pid,
                i.User,
                i.CpuPercent,
                i.MemoryBytes,
                i.MemoryString,
                i.GpuPercent,
                i.DiskBytesPerSec,
                i.DiskString,
                i.NetworkBytesPerSec,
                i.NetworkString,
                i.ThreadsCount,
                i.HandlesCount,
                i.Priority,
                i.Status,
                i.Path,
                i.Publisher,
                i.IntegrityLevel,
                i.Description,
                i.RiskScore
            });

            string json = System.Text.Json.JsonSerializer.Serialize(list, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }

        public static void ExportToHtml(IEnumerable<ProcessItem> items, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='en'>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset='UTF-8'>");
            sb.AppendLine("    <title>Process Explorer Pro - Export Report</title>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #0f1015; color: #e1e3e6; margin: 0; padding: 20px; }");
            sb.AppendLine("        h1 { color: #5c62d6; border-bottom: 2px solid #232530; padding-bottom: 10px; }");
            sb.AppendLine("        .meta { margin-bottom: 20px; color: #848694; font-size: 14px; }");
            sb.AppendLine("        table { width: 100%; border-collapse: collapse; background-color: #161821; border-radius: 8px; overflow: hidden; margin-top: 20px; box-shadow: 0 4px 12px rgba(0,0,0,0.3); }");
            sb.AppendLine("        th, td { padding: 12px 15px; text-align: left; border-bottom: 1px solid #232530; }");
            sb.AppendLine("        th { background-color: #232530; color: #5c62d6; font-weight: 600; text-transform: uppercase; font-size: 12px; letter-spacing: 0.5px; }");
            sb.AppendLine("        tr:hover { background-color: #1e202f; }");
            sb.AppendLine("        .badge { display: inline-block; padding: 3px 8px; border-radius: 12px; font-size: 11px; font-weight: 600; }");
            sb.AppendLine("        .badge-system { background-color: #1d3557; color: #a8dadc; }");
            sb.AppendLine("        .badge-user { background-color: #457b9d; color: #f1faee; }");
            sb.AppendLine("        .badge-danger { background-color: #e63946; color: #ffffff; }");
            sb.AppendLine("        .badge-warn { background-color: #fca311; color: #14213d; }");
            sb.AppendLine("        .badge-ok { background-color: #2a9d8f; color: #ffffff; }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("    <h1>Process Explorer Pro - Export Report</h1>");
            sb.AppendLine($"    <div class='meta'>Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | Total Processes: {items.Count()}</div>");
            sb.AppendLine("    <table>");
            sb.AppendLine("        <thead>");
            sb.AppendLine("            <tr>");
            sb.AppendLine("                <th>Name</th>");
            sb.AppendLine("                <th>PID</th>");
            sb.AppendLine("                <th>User</th>");
            sb.AppendLine("                <th>CPU %</th>");
            sb.AppendLine("                <th>Memory</th>");
            sb.AppendLine("                <th>GPU %</th>");
            sb.AppendLine("                <th>Disk I/O</th>");
            sb.AppendLine("                <th>Network I/O</th>");
            sb.AppendLine("                <th>Threads</th>");
            sb.AppendLine("                <th>Risk</th>");
            sb.AppendLine("                <th>Status</th>");
            sb.AppendLine("            </tr>");
            sb.AppendLine("        </thead>");
            sb.AppendLine("        <tbody>");

            foreach (var item in items)
            {
                string riskBadge = item.RiskScore >= 50 ? "<span class='badge badge-danger'>High</span>" : (item.RiskScore >= 20 ? "<span class='badge badge-warn'>Medium</span>" : "<span class='badge badge-ok'>Low</span>");
                string userBadge = item.User.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase) ? $"<span class='badge badge-system'>{item.User}</span>" : $"<span class='badge badge-user'>{item.User}</span>";

                sb.AppendLine("            <tr>");
                sb.AppendLine($"                <td><strong>{item.Name}</strong><br/><small style='color:#6c6e7a'>{item.Path}</small></td>");
                sb.AppendLine($"                <td>{item.Pid}</td>");
                sb.AppendLine($"                <td>{userBadge}</td>");
                sb.AppendLine($"                <td>{item.CpuPercent:F2}%</td>");
                sb.AppendLine($"                <td>{item.MemoryString}</td>");
                sb.AppendLine($"                <td>{item.GpuPercent:F1}%</td>");
                sb.AppendLine($"                <td>{item.DiskString}</td>");
                sb.AppendLine($"                <td>{item.NetworkString}</td>");
                sb.AppendLine($"                <td>{item.ThreadsCount}</td>");
                sb.AppendLine($"                <td>{riskBadge} ({item.RiskScore})</td>");
                sb.AppendLine($"                <td>{item.Status}</td>");
                sb.AppendLine("            </tr>");
            }

            sb.AppendLine("        </tbody>");
            sb.AppendLine("    </table>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }
    }
}
