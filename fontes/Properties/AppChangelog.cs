using System;

static class AppChangelog
{
    // Histórico visível ao usuário: somente funções e correções, desde a primeira versão C#.
    public static readonly string Text = String.Join("\r\n", new[]
    {
        "2.1.0 — 26/09/2026",
        "",
        "• Nova interface com seções, cartões e ajustes avançados recolhíveis.",
        "• Balões de ajuda explicam cada opção de conversão.",
        "• Identidade DiscForge CHD, ícone próprio e data de compilação em destaque.",
        "• Pacote organizado em aplicativo, documentação e licenças.",
        "• Layout adapta o painel do jogo à largura da janela.",
        "",
        "2.0.0 — 25/09/2026",
        "",
        "• Nova interface WinUI 3 com painel de jogos e capas em WebView2.",
        "• Extração com SharpCompress e 7-Zip de reserva para casos incompatíveis.",
        "• Distribuição portátil com os componentes necessários à execução.",
        "• Mantidas retomada, verificação, telemetria e exclusão opcional após sucesso.",
        "",
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
