using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Spectre.Console.Phantom.Runner;

/// <summary>
/// Managed wrapper around a Windows ConPTY pseudo-console.
/// Creates the input/output pipes and the pseudo-console handle.
/// </summary>
internal sealed class PseudoConsole : IDisposable
{
    private IntPtr _handle;
    private readonly SafeFileHandle _ptyInputReadPipe;
    private readonly SafeFileHandle _ptyOutputWritePipe;
    private bool _disposed;

    /// <summary>
    /// Write keystrokes to this stream (our end of the input pipe).
    /// </summary>
    public FileStream InputStream { get; }

    /// <summary>
    /// Read process output from this stream (our end of the output pipe).
    /// </summary>
    public FileStream OutputStream { get; }

    /// <summary>
    /// The ConPTY handle for process attachment.
    /// </summary>
    internal IntPtr Handle => _handle;

    /// <summary>
    /// Raw output pipe handle for direct ReadFile calls.
    /// </summary>
    internal SafeFileHandle OutputPipeHandle { get; }

    /// <summary>
    /// Raw input pipe handle for direct WriteFile calls.
    /// </summary>
    internal SafeFileHandle InputPipeHandle { get; }

    public int Width { get; }
    public int Height { get; }

    private PseudoConsole(
        IntPtr handle,
        FileStream inputStream,
        FileStream outputStream,
        SafeFileHandle inputPipeHandle,
        SafeFileHandle outputPipeHandle,
        SafeFileHandle ptyInputReadPipe,
        SafeFileHandle ptyOutputWritePipe,
        int width,
        int height)
    {
        _handle = handle;
        InputStream = inputStream;
        OutputStream = outputStream;
        InputPipeHandle = inputPipeHandle;
        OutputPipeHandle = outputPipeHandle;
        _ptyInputReadPipe = ptyInputReadPipe;
        _ptyOutputWritePipe = ptyOutputWritePipe;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Create a new pseudo-console with the specified dimensions.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">Thrown on non-Windows platforms.</exception>
    /// <exception cref="Win32Exception">Thrown if ConPTY creation fails.</exception>
    public static PseudoConsole Create(int width, int height)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException(
                "ConPTY is only available on Windows 10 version 1809 or later.");
        }

        var sa = new ConPtyNative.SecurityAttributes
        {
            nLength = Marshal.SizeOf<ConPtyNative.SecurityAttributes>(),
            bInheritHandle = 1, // TRUE
        };

        // Input pipe: we write to inputWritePipe, ConPTY reads from inputReadPipe
        if (!ConPtyNative.CreatePipe(out var inputReadPipe, out var inputWritePipe, in sa, 0))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create input pipe.");
        }

        // Output pipe: ConPTY writes to outputWritePipe, we read from outputReadPipe
        if (!ConPtyNative.CreatePipe(out var outputReadPipe, out var outputWritePipe, in sa, 0))
        {
            inputReadPipe.Dispose();
            inputWritePipe.Dispose();
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create output pipe.");
        }

        var size = new ConPtyNative.Coord { X = (short)width, Y = (short)height };
        var hr = ConPtyNative.CreatePseudoConsole(size, inputReadPipe, outputWritePipe, 0, out var handle);

        if (hr != ConPtyNative.S_OK)
        {
            inputReadPipe.Dispose();
            inputWritePipe.Dispose();
            outputReadPipe.Dispose();
            outputWritePipe.Dispose();
            throw new Win32Exception(hr, "Failed to create pseudo-console. Requires Windows 10 v1809+.");
        }

        // IMPORTANT: Do NOT close the ConPTY-side pipe ends here.
        // ConPTY uses these handles directly (does not duplicate them).
        // They must remain open for the lifetime of the ConPTY.
        // They are closed in Dispose() after ClosePseudoConsole().

        // Wrap our-side handles in FileStream for managed read/write.
        // isAsync: false — CreatePipe handles are synchronous (no FILE_FLAG_OVERLAPPED).
        var inputStream = new FileStream(inputWritePipe, FileAccess.Write, bufferSize: 256, isAsync: false);
        var outputStream = new FileStream(outputReadPipe, FileAccess.Read, bufferSize: 4096, isAsync: false);

        return new PseudoConsole(handle, inputStream, outputStream, inputWritePipe, outputReadPipe,
            inputReadPipe, outputWritePipe, width, height);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ConPtyNative.ClosePseudoConsole(_handle);
        _handle = IntPtr.Zero;

        // Close ConPTY-side pipe ends now that ConPTY is closed
        _ptyInputReadPipe.Dispose();
        _ptyOutputWritePipe.Dispose();

        // Close our-side streams (and their underlying pipe handles)
        InputStream.Dispose();
        OutputStream.Dispose();
    }

}
