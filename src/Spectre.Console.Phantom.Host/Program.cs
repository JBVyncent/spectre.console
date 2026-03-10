// Spectre.Console.Phantom.Host — ConPTY bridge process
//
// This process creates a ConPTY pseudo-console and launches the target process
// inside it. Communication with the parent process uses named pipes to avoid
// ConPTY interfering with standard handle redirection.
//
// Usage: Spectre.Console.Phantom.Host <width> <height> <inputPipeName> <outputPipeName> [workingDirectory] -- <commandLine>
//
// The host exits with the same exit code as the target process.

using System.IO.Pipes;
using Spectre.Console.Phantom.Runner;

// Parse arguments
if (args.Length < 6)
{
    Console.Error.WriteLine("Usage: Spectre.Console.Phantom.Host <width> <height> <inputPipe> <outputPipe> [workingDirectory] -- <commandLine...>");
    return 1;
}

var width = int.Parse(args[0]);
var height = int.Parse(args[1]);
var inputPipeName = args[2];
var outputPipeName = args[3];

// Find the "--" separator
var separatorIndex = Array.IndexOf(args, "--");
if (separatorIndex < 0 || separatorIndex >= args.Length - 1)
{
    Console.Error.WriteLine("Missing '--' separator before command line.");
    return 1;
}

var workingDirectory = separatorIndex > 4 ? args[4] : null;
var commandLine = string.Join(' ', args.Skip(separatorIndex + 1));

// Connect to named pipes created by the parent process
using var inputPipe = new NamedPipeClientStream(".", inputPipeName, PipeDirection.In);
using var outputPipe = new NamedPipeClientStream(".", outputPipeName, PipeDirection.Out);

inputPipe.Connect(5000);
outputPipe.Connect(5000);

// Create ConPTY — this may alter standard handles, but we don't use them
using var pty = PseudoConsole.Create(width, height);
using var proc = ProcessRunner.Launch(commandLine, pty, workingDirectory);

// Forward ConPTY output → output pipe (background thread)
var outputDone = new ManualResetEventSlim(false);
var outputForwarder = Task.Run(() =>
{
    var buffer = new byte[4096];
    try
    {
        while (true)
        {
            int n;
            try
            {
                n = pty.OutputStream.Read(buffer, 0, buffer.Length);
            }
            catch (IOException) { break; }
            catch (ObjectDisposedException) { break; }

            if (n == 0) break;

            outputPipe.Write(buffer, 0, n);
            outputPipe.Flush();
        }
    }
    catch (IOException) { }
    finally
    {
        outputDone.Set();
    }
});

// Forward input pipe → ConPTY input (background thread)
var inputForwarder = Task.Run(() =>
{
    var buffer = new byte[256];
    try
    {
        while (true)
        {
            int n;
            try
            {
                n = inputPipe.Read(buffer, 0, buffer.Length);
            }
            catch (IOException) { break; }
            catch (ObjectDisposedException) { break; }

            if (n == 0) break;

            try
            {
                pty.InputStream.Write(buffer, 0, n);
                pty.InputStream.Flush();
            }
            catch (IOException) { break; }
        }
    }
    catch (IOException) { }
});

// Wait for the target process to exit
var exitCode = await proc.WaitForExitAsync();

// Give ConPTY a moment to flush remaining output
await Task.Delay(200);

// Wait for output forwarding to complete
outputDone.Wait(TimeSpan.FromSeconds(2));

return exitCode;
