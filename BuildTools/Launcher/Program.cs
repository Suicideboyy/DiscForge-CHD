using System.Diagnostics;
using System.Runtime.InteropServices;

// O WinUI precisa iniciar com seu executável junto das dependências nativas.
// Este ponto de entrada mantém a raiz limpa e compartilha o runtime .NET em app/.
try
{
    var start = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "DiscForge-CHD.exe"))
    {
        UseShellExecute = false,
        WorkingDirectory = AppContext.BaseDirectory
    };
    foreach (string argument in args)
        start.ArgumentList.Add(argument);

    using var application = Process.Start(start)
        ?? throw new IOException("Não foi possível iniciar o aplicativo.");
    if (args.Length == 0)
        return 0;

    // Os comandos de diagnóstico precisam devolver o código de saída real.
    application.WaitForExit();
    return application.ExitCode;
}
catch (Exception error)
{
    if (args.Length == 0)
        NativeError.Show(IntPtr.Zero, "Extraia o pacote inteiro antes de abrir.\n\n" + error.Message,
            "DiscForge CHD", 0x10);
    return 1;
}

static class NativeError
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "MessageBoxW")]
    public static extern int Show(IntPtr window, string text, string caption, uint type);
}
