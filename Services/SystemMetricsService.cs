using System.Diagnostics;
using System.Runtime.InteropServices;
using AspNetBbs.Models;

namespace AspNetBbs.Services;

public interface ISystemMetricsService
{
    SystemMetricsViewModel GetMetrics();
}

public class SystemMetricsService : ISystemMetricsService
{
    private readonly object syncLock = new();

    private ulong prevSysKernel;
    private ulong prevSysUser;
    private ulong prevSysIdle;

    private TimeSpan prevProcTime;
    private DateTime prevProcSampleUtc = DateTime.UtcNow;

    private double lastSysCpu;
    private double lastProcCpu;
    private DateTime lastSampleTime = DateTime.MinValue;

    public SystemMetricsService()
    {
        InitializeBaseline();
    }

    private void InitializeBaseline()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (GetSystemTimes(out var idle, out var kernel, out var user))
                {
                    prevSysIdle = ToUInt64(idle);
                    prevSysKernel = ToUInt64(kernel);
                    prevSysUser = ToUInt64(user);
                }
            }

            using var proc = Process.GetCurrentProcess();
            prevProcTime = proc.TotalProcessorTime;
            prevProcSampleUtc = DateTime.UtcNow;
        }
        catch
        {
            // Ignore baseline initialization errors
        }
    }

    public SystemMetricsViewModel GetMetrics()
    {
        lock (syncLock)
        {
            var now = DateTime.UtcNow;
            var elapsedMs = (now - lastSampleTime).TotalMilliseconds;

            if (elapsedMs >= 300 || lastSampleTime == DateTime.MinValue)
            {
                CalculateCpuUsage(now);
                lastSampleTime = now;
            }

            return BuildViewModel();
        }
    }

    private void CalculateCpuUsage(DateTime now)
    {
        // 1. System CPU (Windows P/Invoke)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                if (GetSystemTimes(out var idle, out var kernel, out var user))
                {
                    var curIdle = ToUInt64(idle);
                    var curKernel = ToUInt64(kernel);
                    var curUser = ToUInt64(user);

                    var kerDiff = curKernel - prevSysKernel;
                    var usrDiff = curUser - prevSysUser;
                    var idlDiff = curIdle - prevSysIdle;

                    var totalSys = kerDiff + usrDiff;

                    if (totalSys > 0 && totalSys >= idlDiff)
                    {
                        var used = totalSys - idlDiff;
                        lastSysCpu = Math.Clamp(Math.Round((used * 100.0) / totalSys, 1), 0.0, 100.0);
                    }

                    prevSysKernel = curKernel;
                    prevSysUser = curUser;
                    prevSysIdle = curIdle;
                }
            }
            catch
            {
                // Fallback: keep previous or 0
            }
        }

        // 2. Process CPU
        try
        {
            using var proc = Process.GetCurrentProcess();
            var curProcCpu = proc.TotalProcessorTime;
            var procTimeDiff = (curProcCpu - prevProcTime).TotalMilliseconds;
            var timePassed = (now - prevProcSampleUtc).TotalMilliseconds * Environment.ProcessorCount;

            if (timePassed > 0)
            {
                var procPercent = (procTimeDiff / timePassed) * 100.0;
                lastProcCpu = Math.Clamp(Math.Round(procPercent, 1), 0.0, 100.0);
            }

            prevProcTime = curProcCpu;
            prevProcSampleUtc = now;
        }
        catch
        {
            // Fallback: keep previous or 0
        }
    }

    private SystemMetricsViewModel BuildViewModel()
    {
        ulong totalPhys = 0;
        ulong availPhys = 0;
        double memUsagePercent = 0;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                totalPhys = memStatus.ullTotalPhys;
                availPhys = memStatus.ullAvailPhys;
                memUsagePercent = memStatus.dwMemoryLoad;
            }
        }

        // Fallback for memory if P/Invoke failed or non-Windows
        if (totalPhys == 0)
        {
            var gcInfo = GC.GetGCMemoryInfo();
            totalPhys = (ulong)gcInfo.TotalAvailableMemoryBytes;
            availPhys = (ulong)Math.Max(0, gcInfo.TotalAvailableMemoryBytes - GC.GetTotalMemory(false));
            if (totalPhys > 0)
            {
                memUsagePercent = Math.Round((double)(totalPhys - availPhys) * 100.0 / totalPhys, 1);
            }
        }

        var usedPhys = totalPhys >= availPhys ? totalPhys - availPhys : 0;
        if (memUsagePercent <= 0 && totalPhys > 0)
        {
            memUsagePercent = Math.Round((double)usedPhys * 100.0 / totalPhys, 1);
        }

        long processWorkingSet = 0;
        var threadCount = 0;
        TimeSpan procUptime = TimeSpan.Zero;

        try
        {
            using var proc = Process.GetCurrentProcess();
            processWorkingSet = proc.WorkingSet64;
            threadCount = proc.Threads.Count;
            procUptime = DateTime.UtcNow - proc.StartTime.ToUniversalTime();
        }
        catch
        {
            // Ignore process query issues
        }

        var sysUptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
        var gcMemory = GC.GetTotalMemory(false);

        return new SystemMetricsViewModel
        {
            CpuUsagePercent = lastSysCpu,
            ProcessCpuUsagePercent = lastProcCpu,

            TotalMemoryBytes = totalPhys,
            UsedMemoryBytes = usedPhys,
            AvailableMemoryBytes = availPhys,
            MemoryUsagePercent = Math.Round(memUsagePercent, 1),
            ProcessMemoryBytes = processWorkingSet,

            TotalMemoryFormatted = FormatBytes(totalPhys),
            UsedMemoryFormatted = FormatBytes(usedPhys),
            AvailableMemoryFormatted = FormatBytes(availPhys),
            ProcessMemoryFormatted = FormatBytes((ulong)processWorkingSet),
            GcMemoryFormatted = FormatBytes((ulong)gcMemory),

            CpuCoreCount = Environment.ProcessorCount,
            MachineName = Environment.MachineName,
            OsDescription = RuntimeInformation.OSDescription,
            SystemUptime = FormatTimeSpan(sysUptime),
            ProcessUptime = FormatTimeSpan(procUptime),
            ThreadCount = threadCount,

            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    private static string FormatBytes(ulong bytes)
    {
        if (bytes >= 1024UL * 1024 * 1024)
        {
            return $"{(double)bytes / (1024 * 1024 * 1024):F2} GB";
        }
        if (bytes >= 1024UL * 1024)
        {
            return $"{(double)bytes / (1024 * 1024):F1} MB";
        }
        if (bytes >= 1024UL)
        {
            return $"{(double)bytes / 1024:F0} KB";
        }
        return $"{bytes} B";
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalDays >= 1)
        {
            return $"{(int)ts.TotalDays}일 {ts.Hours}시간 {ts.Minutes}분 {ts.Seconds}초";
        }
        if (ts.TotalHours >= 1)
        {
            return $"{ts.Hours}시간 {ts.Minutes}분 {ts.Seconds}초";
        }
        return $"{ts.Minutes}분 {ts.Seconds}초";
    }

    private static ulong ToUInt64(System.Runtime.InteropServices.ComTypes.FILETIME ft)
    {
        return ((ulong)(uint)ft.dwHighDateTime << 32) | (uint)ft.dwLowDateTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(
        out System.Runtime.InteropServices.ComTypes.FILETIME lpIdleTime,
        out System.Runtime.InteropServices.ComTypes.FILETIME lpKernelTime,
        out System.Runtime.InteropServices.ComTypes.FILETIME lpUserTime);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public MEMORYSTATUSEX()
        {
            dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);
}

