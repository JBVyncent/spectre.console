using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Spectre.Console.Phantom.Runner;

/// <summary>
/// Launches a child process attached to a ConPTY pseudo-console
/// via <c>CreateProcessW</c> with <c>PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE</c>.
/// </summary>
internal sealed class ProcessRunner : IDisposable
{
    private IntPtr _processHandle;
    private IntPtr _threadHandle;
    private IntPtr _attributeList;
    private bool _disposed;

    public int ProcessId { get; }

    public bool HasExited
    {
        get
        {
            if (_processHandle == IntPtr.Zero)
            {
                return true;
            }

            ConPtyNative.GetExitCodeProcess(_processHandle, out var code);
            return code != ConPtyNative.STILL_ACTIVE;
        }
    }

    public int ExitCode
    {
        get
        {
            ConPtyNative.GetExitCodeProcess(_processHandle, out var code);
            return (int)code;
        }
    }

    private ProcessRunner(IntPtr processHandle, IntPtr threadHandle, IntPtr attributeList, int processId)
    {
        _processHandle = processHandle;
        _threadHandle = threadHandle;
        _attributeList = attributeList;
        ProcessId = processId;
    }

    /// <summary>
    /// Launch a process attached to the given pseudo-console.
    /// </summary>
    /// <param name="commandLine">Full command line (executable + arguments).</param>
    /// <param name="pty">The pseudo-console to attach.</param>
    /// <param name="workingDirectory">Optional working directory.</param>
    /// <exception cref="Win32Exception">Thrown if process creation fails.</exception>
    public static ProcessRunner Launch(string commandLine, PseudoConsole pty, string? workingDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(commandLine);
        ArgumentNullException.ThrowIfNull(pty);

        // Allocate the attribute list
        var attrListSize = IntPtr.Zero;
        ConPtyNative.InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref attrListSize);

        var attributeList = Marshal.AllocHGlobal(attrListSize);
        if (!ConPtyNative.InitializeProcThreadAttributeList(attributeList, 1, 0, ref attrListSize))
        {
            Marshal.FreeHGlobal(attributeList);
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to initialize process attribute list.");
        }

        // Attach the pseudo-console handle
        if (!ConPtyNative.UpdateProcThreadAttribute(
            attributeList,
            0,
            (IntPtr)ConPtyNative.PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE,
            pty.Handle,
            (IntPtr)IntPtr.Size,
            IntPtr.Zero,
            IntPtr.Zero))
        {
            ConPtyNative.DeleteProcThreadAttributeList(attributeList);
            Marshal.FreeHGlobal(attributeList);
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set pseudo-console attribute.");
        }

        var startupInfo = new ConPtyNative.StartupInfoEx
        {
            StartupInfo = new ConPtyNative.StartupInfo
            {
                cb = Marshal.SizeOf<ConPtyNative.StartupInfoEx>(),
            },
            lpAttributeList = attributeList,
        };

        if (!ConPtyNative.CreateProcessW(
            null,
            commandLine,
            IntPtr.Zero,
            IntPtr.Zero,
            false,
            ConPtyNative.EXTENDED_STARTUPINFO_PRESENT,
            IntPtr.Zero,
            workingDirectory,
            in startupInfo,
            out var processInfo))
        {
            ConPtyNative.DeleteProcThreadAttributeList(attributeList);
            Marshal.FreeHGlobal(attributeList);
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to create process: {commandLine}");
        }

        return new ProcessRunner(processInfo.hProcess, processInfo.hThread, attributeList, processInfo.dwProcessId);
    }

    /// <summary>
    /// Wait for the process to exit asynchronously.
    /// </summary>
    public Task<int> WaitForExitAsync(CancellationToken ct = default)
    {
        if (HasExited)
        {
            return Task.FromResult(ExitCode);
        }

        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        var waitHandle = new ManualResetEvent(false)
        {
            SafeWaitHandle = new Microsoft.Win32.SafeHandles.SafeWaitHandle(_processHandle, ownsHandle: false),
        };

        var registration = ThreadPool.RegisterWaitForSingleObject(
            waitHandle,
            (_, timedOut) =>
            {
                if (timedOut)
                {
                    tcs.TrySetCanceled();
                }
                else
                {
                    tcs.TrySetResult(ExitCode);
                }

                waitHandle.Dispose();
            },
            null,
            Timeout.Infinite,
            executeOnlyOnce: true);

        ct.Register(() =>
        {
            registration.Unregister(null);
            waitHandle.Dispose();
            tcs.TrySetCanceled(ct);
        });

        return tcs.Task;
    }

    /// <summary>
    /// Forcefully terminate the process.
    /// </summary>
    public void Kill()
    {
        if (_processHandle != IntPtr.Zero && !HasExited)
        {
            ConPtyNative.TerminateProcess(_processHandle, 1);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_threadHandle != IntPtr.Zero)
        {
            ConPtyNative.CloseHandle(_threadHandle);
            _threadHandle = IntPtr.Zero;
        }

        if (_processHandle != IntPtr.Zero)
        {
            ConPtyNative.CloseHandle(_processHandle);
            _processHandle = IntPtr.Zero;
        }

        if (_attributeList != IntPtr.Zero)
        {
            ConPtyNative.DeleteProcThreadAttributeList(_attributeList);
            Marshal.FreeHGlobal(_attributeList);
            _attributeList = IntPtr.Zero;
        }
    }
}
