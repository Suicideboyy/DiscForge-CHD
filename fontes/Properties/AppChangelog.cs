using System;

static class AppChangelog
{
    // Histórico visível ao usuário: somente funções e correções, desde a primeira versão C#.
    public static readonly string Text = String.Join("\r\n", new[]
    {
        "1.3.0 — 21/09/2026",
        "",
        "• Capas carregadas durante a identificação, inclusive para jogos já reconhecidos.",
        "• Nova tentativa de capa após falhas temporárias de leitura ou conexão.",
        "• Contador de tempo da entrada atual, incluindo extração e verificação.",
        "• Uso da CPU e leitura, escrita e atividade dos discos em tempo real.",
        "• Quantidade de threads preenchida automaticamente conforme o computador.",
        "• Interface com novas cores, cartões e controles arredondados.",
        "",
        "1.2.0 — 20/09/2026",
        "",
        "• Conversão de imagens PS1/PS2 e arquivos compactados para CHD.",
        "• Seleção de pastas, plataforma, codecs, hunks e threads.",
        "• Consulta por serial, nomes de jogos e capas PS2.",
        "• Retomada sem descompactar novamente jogos já convertidos.",
        "• Proteção contra conversão incompleta de faixas BIN sem CUE.",
        "• Verificação do CHD e exclusão opcional do compactado após sucesso."
    });
}
