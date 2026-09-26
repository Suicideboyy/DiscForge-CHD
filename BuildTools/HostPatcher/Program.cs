using Microsoft.NET.HostModel.AppHost;

// Gera o host nativo oficial do .NET apontando para a DLL na subpasta app.
// Copia os recursos do executável, incluindo ícone e informações de versão.
if (args.Length != 4)
    throw new ArgumentException("Informe template, destino, DLL relativa e executável de recursos.");

HostWriter.CreateAppHost(
    appHostSourceFilePath: args[0],
    appHostDestinationFilePath: args[1],
    appBinaryFilePath: args[2],
    windowsGraphicalUserInterface: true,
    assemblyToCopyResourcesFrom: args[3]);
