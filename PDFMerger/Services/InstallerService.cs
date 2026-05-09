using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PDFMerger.Services;

public static class InstallerService
{
    public const string AppDisplayName = "PDF Merger";
    public const string ExeFileName = "PDFMerger.exe";

    public static string DefaultInstallDir =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs", "PDFMerger");

    public static string CurrentExePath =>
        Process.GetCurrentProcess().MainModule?.FileName
        ?? Environment.ProcessPath
        ?? throw new InvalidOperationException("Cannot determine current exe path.");

    /// <summary>
    /// True if the app is already installed at the default location, OR the
    /// user has been prompted before (declined or installed). In either case
    /// we don't show the install dialog.
    /// </summary>
    public static bool ShouldShowInstallPrompt()
    {
        if (SettingsService.Current.InstallPromptShown)
            return false;

        try
        {
            var exeDir = Path.GetDirectoryName(CurrentExePath);
            if (!string.IsNullOrEmpty(exeDir) &&
                exeDir.StartsWith(DefaultInstallDir, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        catch
        {
            // If we can't resolve, default to not showing the prompt.
            return false;
        }

        return true;
    }

    /// <summary>
    /// Copies the current exe into installDir, creates optional shortcuts,
    /// then launches the installed copy. The current process should exit
    /// after this returns successfully.
    /// </summary>
    public static string Install(string installDir, bool desktopShortcut, bool startMenuShortcut)
    {
        Directory.CreateDirectory(installDir);

        var sourceExe = CurrentExePath;
        var targetExe = Path.Combine(installDir, ExeFileName);

        if (!string.Equals(
                Path.GetFullPath(sourceExe),
                Path.GetFullPath(targetExe),
                StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(sourceExe, targetExe, overwrite: true);
        }

        if (desktopShortcut)
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            CreateShortcut(
                Path.Combine(desktop, $"{AppDisplayName}.lnk"),
                targetExe,
                $"{AppDisplayName} - merge, split and convert PDFs");
        }

        if (startMenuShortcut)
        {
            var startMenu = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft", "Windows", "Start Menu", "Programs");
            CreateShortcut(
                Path.Combine(startMenu, $"{AppDisplayName}.lnk"),
                targetExe,
                $"{AppDisplayName} - merge, split and convert PDFs");
        }

        // Launch the installed copy and let the caller exit the current process.
        Process.Start(new ProcessStartInfo(targetExe) { UseShellExecute = true });

        return targetExe;
    }

    private static void CreateShortcut(string shortcutPath, string targetExe, string description)
    {
        var dir = Path.GetDirectoryName(shortcutPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        // WScript.Shell COM avoids needing a third-party dependency.
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("WScript.Shell COM type not available.");

        object? shell = Activator.CreateInstance(shellType);
        if (shell == null) throw new InvalidOperationException("Failed to create WScript.Shell.");

        object? shortcut = null;
        try
        {
            shortcut = shellType.InvokeMember(
                "CreateShortcut", BindingFlags.InvokeMethod, null, shell,
                new object[] { shortcutPath });
            if (shortcut == null) throw new InvalidOperationException("CreateShortcut returned null.");

            var sType = shortcut.GetType();
            sType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut,
                new object[] { targetExe });
            sType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut,
                new object?[] { Path.GetDirectoryName(targetExe) ?? "" });
            sType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut,
                new object[] { description });
            // Use the exe itself as the icon source - its embedded ApplicationIcon
            // resource is what Explorer uses, so the shortcut matches.
            sType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut,
                new object[] { targetExe + ",0" });
            sType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
        }
        finally
        {
            if (shortcut != null) Marshal.FinalReleaseComObject(shortcut);
            Marshal.FinalReleaseComObject(shell);
        }
    }
}
