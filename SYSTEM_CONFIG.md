# Sistema de Configuração de Importação de NFes

## Arquitetura

### 1. Arquivo de Configuração (nfe_config.json)
- **LocalizaçãoFisica**: Raiz do projeto ou AppDirectory da aplicação
- **Formato**: JSON
- **Carregamento**: Automático no startup, com cache em memória
- **Criação Automática**: Se não existir, cria configuração padrão

### 2. Classes Principais

#### NfeImportConfig
- `PastaHtml`: Caminho da pasta com arquivos HTML (suporta variáveis de ambiente como %AppData%)
- `ImagemCabecalho`: Caminho da imagem para cabeçalho dos relatórios
- `IgnorarCFOP`: Lista de CFOPs que devem ser ignorados
- `IgnorarNatureza`: Lista de Naturezas de Operação a ignorar
- `CFOPReceita`: CFOPs que indicam RECEITA
- `CFOPDespesa`: CFOPs que indicam DESPESA
- `NaturezaReceita`: Naturezas que indicam RECEITA
- `NaturezaDespesa`: Naturezas que indicam DESPESA

#### NotaFiscalDTO
- `Chave`: Chave de acesso (44 dígitos)
- `Natureza`: Natureza da operação
- `Descricao`: Primeiras duas palavras dos itens concatenadas
- `CFOP`: CFOPs concatenados
- `ValorTotal`: Soma dos valores
- `Tipo`: TipoLancamento (Receita ou Despesa)

### 3. Serviços

#### NfeConfigService
- Carrega/salva configurações do arquivo JSON
- Gerencia cache em memória
- Cria configuração padrão se não existir

#### NfeImportService
- Processa arquivos HTML da pasta configurada
- Extrai dados usando HtmlAgilityPack
- Aplica 6 regras de classificação
- Retorna lista de NotaFiscalDTO

### 4. Regras de Processamento

**REGRA 1**: Se CFOP está em `IgnorarCFOP`
→ NÃO inserir a nota

**REGRA 2**: Se Natureza está em `IgnorarNatureza`
→ NÃO inserir a nota

**REGRA 3**: Se CFOP está em `CFOPReceita`
→ Classificar como RECEITA (ignora emitente)

**REGRA 4**: Se CFOP está em `CFOPDespesa`
→ Classificar como DESPESA (ignora emitente)

**REGRA 5**: Se Natureza está em `NaturezaReceita`
→ Classificar como RECEITA (ignora emitente)

**REGRA 6**: Se Natureza está em `NaturezaDespesa`
→ Classificar como DESPESA (ignora emitente)

### 5. Lógica Padrão

Se nenhuma das regras for atendida:
- Se CNPJ do emitente == produtor cadastrado → RECEITA
- Caso contrário → DESPESA

### 6. Interface de Configuração

Tela visual com campos para:
- Pasta de HTML
- Imagem do cabeçalho
- CFOPs a ignorar
- Naturezas a ignorar
- CFOPs de Receita/Despesa
- Naturezas de Receita/Despesa

Todos os campos de lista aceitam separadores (vírgula ou ponto e vírgula)

### 7. Características de Resiliência

- ✅ Suporta notas sem itens (descartadas silenciosamente)
- ✅ CFOP repetidos (deduplica)
- ✅ Variação de texto em natureza (usa Contains, case-insensitive)
- ✅ Arquivo HTML malformado (continua processando outros)
- ✅ Múltiplos formatos de separador

## Exemplo de nfe_config.json

```json
{
  "pastaHtml": "%AppData%/AutoLCPR/html",
  "imagemCabecalho": "C:/caminho/logo.png",
  "ignorarCFOP": ["5910", "6910"],
  "ignorarNatureza": ["DEVOLUÇÃO", "BONIFICAÇÃO"],
  "cfopReceita": ["5102", "5104", "5410"],
  "cfopDespesa": ["1102", "1104", "7102"],
  "naturezaReceita": ["VENDA", "RECEITA"],
  "naturezaDespesa": ["COMPRA", "DESPESA"]
}
```

## Uso

### 1. Configurar
Abra a tela "⚙️ Configurações" e preencha os campos desejados

### 2. Salvar
Clique em "💾 Salvar Configurações"
- Arquivo JSON é atualizado
- Cache é limpo
- Próximos carregamentos usarão nova configuração

### 3. Importar Notas
```csharp
var service = serviceProvider.GetService<NfeImportService>();
var notas = await service.ImportarNotasAsync(produtorId);
```

## Dependências

- HtmlAgilityPack >= 1.12.4 (parsing de HTML)
- System.Text.Json (serialização JSON)
