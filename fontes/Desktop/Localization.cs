using System;
using System.Collections.Generic;

/// <summary>Small, explicit UI dictionary; engine messages remain its stable report protocol.</summary>
static class Localization
{
    static readonly Dictionary<string, string> Portuguese = new(StringComparer.Ordinal)
    {
        ["Conversion"] = "Conversão", ["Settings"] = "Configurações", ["About"] = "Sobre",
        ["Library"] = "Biblioteca", ["Input"] = "Entrada", ["Output"] = "Saída",
        ["Conversion settings"] = "Configurações de conversão", ["Platform"] = "Plataforma",
        ["Threads · automatic"] = "Threads · automático", ["Encoding options"] = "Opções de codificação",
        ["CD hunk · bytes"] = "Hunk de CD · bytes", ["DVD hunk · bytes"] = "Hunk de DVD · bytes",
        ["Codecs CD"] = "Codecs de CD", ["Codecs DVD"] = "Codecs de DVD",
        ["Game lookup"] = "Consulta de jogos", ["Remove original"] = "Remover original",
        ["Auto-detect system"] = "Detectar sistema automaticamente",
        ["Use RAM extraction"] = "Descompactar em RAM",
        ["Compatibility with older devices"] = "Compatibilidade com dispositivos antigos",
        ["Existing RAM drive"] = "Unidade de RAM existente",
        ["Select an existing RAM drive"] = "Selecione uma unidade de RAM existente",
        ["CURRENT INPUT"] = "ENTRADA ATUAL", ["CPU · APP"] = "CPU · APP",
        ["DISK · APP"] = "DISCO · APP", ["Processing"] = "Processamento",
        ["Activity log"] = "Registro de atividades", ["Start conversion"] = "Iniciar conversão",
        ["Stop after current"] = "Parar após o atual", ["Stop now"] = "Parar agora",
        ["Open output"] = "Abrir saída", ["Browse…"] = "Procurar…",
        ["Reset defaults"] = "Restaurar padrões", ["Report issue"] = "Relatar problema",
        ["External tools"] = "Ferramentas externas", ["About DiscForge CHD"] = "Sobre o DiscForge CHD",
        ["Changelog"] = "Novidades", ["Language"] = "Idioma",
        ["VERSION "] = "VERSÃO ", ["Built on "] = "Compilado em ", ["Built: "] = "Compilado em: ",
        ["Your collection. Less space. Every experience."] = "Sua coleção. Menos espaço. Toda a experiência.",
        ["Folder containing your games"] = "Pasta dos seus jogos", ["CHD destination"] = "Destino dos CHDs",
        ["Automatic (bundled)"] = "Automático (incluído)", ["Identify games by serial"] = "Identificar jogos pelo serial",
        ["Remove archive after success"] = "Remover arquivo após sucesso",
        ["Ready to start"] = "Pronto para iniciar", ["Batch: 0%"] = "Lote: 0%",
        ["Waiting…"] = "Aguardando…", ["Read: "] = "Leitura: ", ["Write: "] = "Gravação: ",
        ["Conversion complete"] = "Conversão concluída",
        ["Stopped now; temporary files removed"] = "Parado; temporários removidos",
        ["Stopped after the current input"] = "Parado após a entrada atual",
        ["Completed with errors; see the log"] = "Concluído com erros; consulte o registro",
        ["Stop requested; finishing the current input…"] = "Parada solicitada; concluindo a entrada atual…",
        ["Canceling and removing temporary files…"] = "Cancelando e removendo temporários…",
        ["Wait for the current input before closing."] = "Aguarde a entrada atual antes de fechar.",
        ["Encoding"] = "Comprimindo", ["Extracting"] = "Descompactando",
        ["Verifying"] = "Verificando", ["Input "] = "Entrada ", ["Batch "] = "Lote ",
        ["ERROR:"] = "ERRO:", ["Already exists:"] = "Já existente:",
        ["Leave paths blank to use the tools included with this application."] =
            "Deixe os caminhos em branco para usar as ferramentas incluídas.",
        ["Convert PS1 and PS2 disc images to verified CHDs."] =
            "Converta imagens de PS1 e PS2 em CHDs verificados.",
        ["LZMA: favors size"] = "LZMA: prioriza tamanho", ["Zstandard: balanced"] = "Zstandard: equilibrado",
        ["zlib: general purpose"] = "zlib: uso geral", ["FLAC: CD audio"] = "FLAC: áudio de CD",
        ["FLAC: audio"] = "FLAC: áudio", ["Huffman: repeated patterns"] = "Huffman: padrões repetidos",
        ["Waiting for input"] = "Aguardando entrada", ["IDENTIFYING"] = "IDENTIFICANDO",
        ["PROCESSING"] = "PROCESSANDO", ["ALREADY EXISTS"] = "JÁ EXISTE",
        ["COMPLETED"] = "CONCLUÍDO", ["CANCELLED"] = "CANCELADO", ["ERROR"] = "ERRO",
        ["Original CUE descriptor"] = "Descritor CUE original", ["CHD metadata"] = "Metadados do CHD",
        ["PS1 disc header"] = "Cabeçalho de disco PS1",
        ["Conversion stopped; original files preserved."] = "Conversão interrompida; arquivos originais preservados.",
        ["Report to"] = "Enviar para", ["Attachment"] = "Anexo", ["Size"] = "Tamanho",
        ["Review the logs before sending."] = "Revise os registros antes de enviar.",
        ["Open email with attachment"] = "Abrir email com anexo",
        ["Open report folder"] = "Abrir pasta do relatório", ["Cancel"] = "Cancelar",
        ["Email client unavailable"] = "Cliente de email indisponível",
        ["The report was saved. Attach it manually to an email to"] =
            "O relatório foi salvo. Anexe-o manualmente em um email para"
    };

    public static bool IsPortuguese { get; private set; }
    public static void SetLanguage(string language) => IsPortuguese = language == "pt-BR";
    public static string Language => IsPortuguese ? "pt-BR" : "en";
    public static string T(string english) => IsPortuguese && Portuguese.TryGetValue(english, out var value)
        ? value : english;
}
