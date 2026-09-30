using System;

sealed partial class MainWindow
{
    /// <summary>Refresh eligibility without creating or modifying the selected RAM drive.</summary>
    void RefreshRamAvailability()
    {
        bool memoryKnown = MemoryStatus.TryRead(out var memory);
        bool enoughMemory = memoryKnown && memory.Eligible;
        bool validDrive = RamWorkspace.TryValidateDrive(ramDiskPath.Text.Trim(), out _, out _);
        ramExtraction.IsEnabled = !running && enoughMemory && validDrive;
        string capacity = memoryKnown
            ? (Localization.IsPortuguese
                ? $"RAM: {memory.TotalBytes / 1073741824.0:N1} GiB total; {memory.AvailableBytes / 1073741824.0:N1} GiB livres. "
                : $"RAM: {memory.TotalBytes / 1073741824.0:N1} GiB total; {memory.AvailableBytes / 1073741824.0:N1} GiB free. ")
            : "";
        string requirement = Localization.IsPortuguese
            ? "Requer mais de 12 GiB totais, 5 GiB livres e uma unidade de RAM verificada."
            : "Requires more than 12 GiB total, 5 GiB free and a verified RAM drive.";
        ramStatus.Text = capacity + (enoughMemory && validDrive
            ? (Localization.IsPortuguese ? "RAM disponível; capacidade do jogo verificada antes da extração."
                : "RAM available; archive capacity checked before extraction.")
            : requirement);
    }
}
