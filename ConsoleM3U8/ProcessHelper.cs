using System.Diagnostics;

namespace ConsoleM3U8;
public sealed class ProcessHelper
{
    public static void Excute(string fileNamePath, string arguments = "")
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileNamePath,
            Arguments = arguments, 
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process
        {
            StartInfo = startInfo
        };

        process.OutputDataReceived += Process_OutputDataReceived;
        process.ErrorDataReceived += Process_ErrorDataReceived;

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.WaitForExit();
    }

    private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            Console.WriteLine(e.Data); 
        }
    }

    private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            Console.WriteLine($"Error: {e.Data}");
        }
    }
}
