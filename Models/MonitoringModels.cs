namespace AspNetBbs.Models;

public class SystemMetricsViewModel
{
    public double CpuUsagePercent { get; set; }
    public double ProcessCpuUsagePercent { get; set; }

    public ulong TotalMemoryBytes { get; set; }
    public ulong UsedMemoryBytes { get; set; }
    public ulong AvailableMemoryBytes { get; set; }
    public double MemoryUsagePercent { get; set; }
    public long ProcessMemoryBytes { get; set; }

    public string TotalMemoryFormatted { get; set; } = string.Empty;
    public string UsedMemoryFormatted { get; set; } = string.Empty;
    public string AvailableMemoryFormatted { get; set; } = string.Empty;
    public string ProcessMemoryFormatted { get; set; } = string.Empty;
    public string GcMemoryFormatted { get; set; } = string.Empty;

    public int CpuCoreCount { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public string OsDescription { get; set; } = string.Empty;
    public string SystemUptime { get; set; } = string.Empty;
    public string ProcessUptime { get; set; } = string.Empty;
    public int ThreadCount { get; set; }

    public string Timestamp { get; set; } = string.Empty;
}

