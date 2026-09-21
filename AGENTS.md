# CHD Optimizer

O usuário autorizou publicar automaticamente as atualizações deste projeto.
Depois de implementar e validar uma atualização, incrementar AssemblyVersion e
AssemblyFileVersion juntos, ajustar versão visível e changelog e executar
Publicar.ps1. Usar o remoto origin já configurado; não inventar destino ou
alterar a visibilidade do repositório. Se origin não existir, solicitar a URL.
Não publicar alterações que ainda falhem nas verificações apropriadas.
Não incluir jogos, arquivos temporários, credenciais ou outros projetos.
Não desativar o antivírus nem restaurar arquivos em quarentena.
Comunicar de forma concisa, sem reler o projeto inteiro a cada atualização.

Informar ao usuário quando houver alterações de código. O changelog do aplicativo
(Properties/AppChangelog.cs) contém apenas novas funções e correções desde 1.2.0;
não registrar ali refatoração, formatação ou outras mudanças internas.
Cada versão local fica em versoes/X.Y.Z, com executável, checksum e pacote completo.
Manter os fontes organizados e ler/alterar somente os módulos necessários.
