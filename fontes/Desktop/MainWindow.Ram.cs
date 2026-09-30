using System;

sealed partial class MainWindow
{
    /// <summary>Refresh eligibility without creating or modifying the selected RAM drive.</summary>
    void RefreshRamAvailability()
    {
        bool memoryKnown = MemoryStatus.TryRead(out var memory);
        bool enoughMemory = memoryKnown && memory.Eligible;
        bool validDrive = RamWorkspace.TryValidateDrive(ramDiskPath.Text.Trim(), out _, out _);
        ramExtraction.IsEnabled = !running && enoughMemory;
        string capacity = memoryKnown
            ? (Localization.IsPortuguese
                ? $"RAM: {memory.TotalBytes / 1073741824.0:N1} GiB total; {memory.AvailableBytes / 1073741824.0:N1} GiB livres. "
                : $"RAM: {memory.TotalBytes / 1073741824.0:N1} GiB total; {memory.AvailableBytes / 1073741824.0:N1} GiB free. ")
            : "";
        string requirement = Localization.IsPortuguese
            ? "Memória insuficiente: requer mais de 12 GiB totais e pelo menos 5 GiB livres."
            : "Insufficient memory: requires more than 12 GiB total and at least 5 GiB free.";
        string driveStatus = validDrive
            ? (Localization.IsPortuguese ? "Unidade RAM pronta; o espaço será verificado por jogo."
                : "RAM drive ready; space will be checked for each game.")
            : (Localization.IsPortuguese
                ? "Memória suficiente. Em Configurações, escolha uma unidade RAM já instalada. Sem ela, a extração usa disco."
                : "Enough memory. Select an already installed RAM drive in Settings. Without it, extraction uses disk.");
        ramStatus.Text = capacity + (enoughMemory ? driveStatus : requirement);
    }
}
