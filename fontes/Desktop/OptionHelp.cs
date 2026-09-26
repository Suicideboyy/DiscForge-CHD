/// <summary>Textos de ajuda do usuário, separados da construção dos controles.</summary>
static class OptionHelp
{
    public const string Input = "Onde estão os jogos. PS2 aceita imagens e compactados; PS1 processa CHDs. "
        + "Jogos já convertidos são conferidos antes de uma nova extração.";
    public const string Output = "Pasta onde os CHDs verificados serão salvos. Deve ser diferente da entrada "
        + "e não pode ficar dentro da pasta temporária. Os arquivos originais ficam preservados por padrão.";
    public const string Platform = "PS1: otimiza CHDs existentes e pode manter o original se ele for menor. "
        + "PS2: aceita compactados, ISO, BIN/CUE e CHD; usa createcd para CD e createdvd para DVD.";
    public const string Threads = "Preenchido com os processadores lógicos desta máquina. Mais threads permitem "
        + "maior paralelismo do chdman, mas o ganho depende da CPU, dos codecs e do disco. "
        + "Reduza para deixar o computador mais disponível durante a conversão.";
    public const string CdHunk = "Tamanho do bloco de CD, em bytes. Deve ser múltiplo de 2448. "
        + "2448 usa blocos pequenos; blocos maiores podem comprimir melhor, mas aumentam o trabalho "
        + "para acessar um trecho pequeno. O melhor tamanho depende do conteúdo.";
    public const string DvdHunk = "Tamanho do bloco de DVD, em bytes. Deve ser múltiplo de 2048. "
        + "2048 corresponde a um setor de dados. Blocos maiores podem melhorar a compressão, "
        + "com maior custo de leitura/descompressão por bloco. Não há garantia de arquivo menor.";
    public const string CdCodecs = "Até quatro codecs, separados por vírgula: cdlz (LZMA), cdzs (Zstandard), "
        + "cdzl (zlib) e cdfl (FLAC para áudio), próprios para CD. O chdman escolhe a opção menor por bloco. "
        + "Mais codecs podem aumentar o tempo de codificação.";
    public const string DvdCodecs = "Até quatro codecs distintos: lzma, zstd, zlib, flac ou huff. "
        + "LZMA costuma priorizar tamanho; Zstandard busca equilíbrio entre velocidade e tamanho. "
        + "zlib é uma opção geral, FLAC é voltado a áudio e Huffman depende do padrão dos dados. "
        + "O chdman escolhe o menor resultado por bloco; nem todo codec ajuda em todo DVD.";
    public const string Lookup = "Consulta o serial para obter nome e tipo de mídia. Um resultado confirmado "
        + "gera Nome [SERIAL].chd. Sem correspondência, usa a identificação local. "
        + "A busca da capa funciona separadamente, mesmo com a mídia já reconhecida.";
    public const string Delete = "Só remove o compactado e seus volumes quando todos os discos da entrada "
        + "forem convertidos nesta execução e os CHDs forem verificados. Não apaga imagens avulsas, "
        + "entradas com erro ou compactados ignorados porque o CHD já existia.";
    public const string Stop = "Solicita uma parada segura: a entrada atual termina, incluindo a verificação. "
        + "Os jogos seguintes não são iniciados.";
    public const string Cpu = "Porcentagem de CPU usada por todo o sistema, incluindo outros programas. "
        + "Atualizada aproximadamente a cada segundo. N/D indica contador indisponível.";
    public const string Disk = "Leitura e escrita somadas dos discos físicos, em MiB/s; atividade média em %. "
        + "Inclui outros aplicativos. Estes valores não representam apenas a velocidade do encoder.";
    public const string Time = "Tempo da entrada atual: identificação, extração, compressão e verificação. "
        + "Reinicia ao começar o próximo arquivo e para quando a entrada termina.";
}
