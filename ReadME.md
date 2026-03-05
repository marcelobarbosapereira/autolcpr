# AutoLCPR

Sistema desktop (WPF e .NET) para gestao fiscal, financeira e de rebanho para produtor rural.

## Visao Geral

O AutoLCPR centraliza em uma unica aplicacao:

- Cadastro de produtores.
- Controle de notas fiscais (entrada e saida).
- Controle de movimentacoes de rebanho.
- Captura e importacao de NF-e via consulta SEFAZ-MS.
- Geracao de relatorios em PDF (anual, rebanho e financeiro por periodo).

Projeto organizado em arquitetura em camadas:

- `UI (WPF)` -> `Application` -> `Domain` -> `Infrastructure (EF Core + SQLite)`

## Stack Tecnica

- `.NET 8`
- `WPF (Windows)`
- `Entity Framework Core 8`
- `SQLite`
- `CommunityToolkit.Mvvm`
- `Microsoft.Web.WebView2`
- `Microsoft.Playwright`
- `itext7` (geracao de PDF)

## Estrutura do Projeto

```text
autolcpr/
    AutoLCPR.sln
    ReadME.md
    nfe_config.json
    src/
        AutoLCPR.Domain/          # Entidades e contratos de repositorio
        AutoLCPR.Application/     # Regras de negocio, servicos e relatorios
        AutoLCPR.Infrastructure/  # EF Core, DbContext, migrations, repositorios
        AutoLCPR.UI.WPF/          # Interface desktop (WPF)
```

## Requisitos

- Windows 10/11.
- .NET SDK 8.0+.
- Runtime do WebView2 instalado no sistema.
- Acesso a internet para fluxo de captura automatizada da SEFAZ.

## Como Executar

Na raiz do repositorio (`autolcpr`):

```powershell
dotnet restore
dotnet build AutoLCPR.sln
dotnet run --project src/AutoLCPR.UI.WPF/AutoLCPR.UI.WPF.csproj
```

## Configuracao

### 1) Banco de dados e app settings

Arquivo: `src/AutoLCPR.UI.WPF/appsettings.Development.json`

- `ConnectionStrings:DefaultConnection`: usa `Data Source={DatabasePath}/autolcpr.db`.
- `DatabasePath`: por padrao `%AppData%/AutoLCPR/data`.

Na inicializacao, a aplicacao:

- Resolve variaveis de ambiente do caminho.
- Cria a pasta de dados automaticamente.
- Testa conexao com o banco.
- Aplica migrations automaticamente.

### 2) Configuracao de importacao NFe

Arquivo: `nfe_config.json`

Campos principais:

- `pastaHtml`: pasta base para armazenar HTML de consulta por produtor.
- `ignorarCFOP` e `ignorarNatureza`: filtros de descarte de notas.
- `cfopReceita` e `cfopDespesa`: regras de classificacao por CFOP.
- `naturezaReceita` e `naturezaDespesa`: regras de classificacao por natureza.

Se o arquivo de configuracao nao existir na pasta de execucao, ele e criado automaticamente com valores padrao.

## Fluxos Principais

### Cadastro e operacao manual

- Produtores.
- Notas fiscais.
- Rebanhos.

### Importacao fiscal (SEFAZ-MS)

- Tela `Importar` usa WebView2 para navegacao.
- Captura chaves de acesso e detalhes da consulta.
- Salva HTML por produtor (`{pastaHtml}/{cpf}/`).
- Importa e converte para lancamentos/notas no banco.

### Relatorios

- Relatorio anual consolidado.
- Relatorio de movimentacao de rebanho.
- Relatorio financeiro por periodo e tipo (Receita/Despesa).
- Saida em PDF com escolha de destino pelo usuario.

## Banco de Dados e Migrations

As migrations estao em `src/AutoLCPR.Infrastructure/Migrations`.

A aplicacao aplica migrations no startup, mas se voce quiser operar manualmente via CLI:

```powershell
dotnet tool install --global dotnet-ef
dotnet ef migrations list --project src/AutoLCPR.Infrastructure/AutoLCPR.Infrastructure.csproj --startup-project src/AutoLCPR.UI.WPF/AutoLCPR.UI.WPF.csproj
dotnet ef database update --project src/AutoLCPR.Infrastructure/AutoLCPR.Infrastructure.csproj --startup-project src/AutoLCPR.UI.WPF/AutoLCPR.UI.WPF.csproj
```

## Troubleshooting

- Erro `MC3000` em `ImportarView.xaml`:
    verifique se ha marcadores de conflito Git (`<<<<<<<`, `=======`, `>>>>>>>`) em XAML/ViewModel.
- Falha ao abrir WebView2:
    confirme o runtime do WebView2 instalado no Windows.
- Falha no banco ao iniciar:
    verifique permissao de escrita em `%AppData%/AutoLCPR/data`.
- Falha no fluxo Playwright:
    valide conectividade de rede e mudancas na pagina da SEFAZ.

## Status

Projeto em desenvolvimento ativo.

## Licenca

Uso restrito/privado.
