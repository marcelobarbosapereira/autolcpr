# Sistema de Gestão Fiscal e Rebanho Rural

Software desktop para produtores rurais, focado no controle de:
- Notas fiscais (entradas e saídas)
- Receitas e despesas
- Controle de rebanho por inscrição estadual
- Relatórios mensais e anuais
- Integração automatizada com SEFAZ-MS e DF-e

---

## 🎯 Objetivo

Centralizar a gestão fiscal e zootécnica do produtor rural em um sistema:
- Offline-first
- Seguro
- Auditável
- Compatível com obrigações fiscais brasileiras

---

## 🧱 Arquitetura

- Plataforma: **.NET 8**
- Tipo: **Desktop**
- Banco de dados: **SQLite embutido**
- ORM: **Entity Framework Core**
- Automação:
  - Playwright (.NET)
  - Webservice DF-e (oficial)
- Relatórios: PDF / Excel

Arquitetura em camadas:
UI → Application → Domain → Infrastructure → SQLite

---

## 🐄 Funcionalidades

### Rebanhos
- Cadastro manual de rebanhos
- Controle de entradas, saídas, nascimentos e mortes
- Saldo automático

### Fiscal / Financeiro
- Cadastro manual de notas fiscais
- Receitas e despesas
- Relatórios mensais e anuais
- Exportação de dados

### Automação Fiscal
- Coleta de chaves de acesso via SEFAZ-MS
- Integração oficial DF-e
- Download e armazenamento de XML
- Consulta por NSU

---

## 🔐 Segurança

- Certificado digital A1 ou A3
- Banco local criptografado
- Backup automático
- Logs fiscais

---

## 📦 Banco de Dados

- SQLite local
- Offline-first
- Estrutura versionada
- Backup manual e automático

---
## 🧱 Diagrama de Classes

```mermaid
classDiagram
    class Produtor {
        +int Id
        +String Nome
        +List~Rebanho~ rebanho
        +List~NotaFiscal~ receitas
        +List~NotaFiscal~ despesas

    }

    class NotaFiscal {
        +String numero
        +String chaveAcesso
        +double valor
        +Date dataEmissao
        +String nomeEmitente
        +String nomeDestinatario
        +String documentoEmitente
        +String documentoDestinatario
    }

    class Rebanho {
        +String inscricao
        +int nascimentos
        +int mortes
        +int entradas
        +int saidas
    }

    class tipoNota {
        <<<enumeration>>>
        RECEITA
        DESPESA
    }
```
## ↔️ Diagrama ER

```mermaid
erDiagram
    PRODUTOR ||--o{ REBANHO : "possui"
    PRODUTOR ||--o{ NOTA_FISCAL : "gera"
    TIPO_NOTA ||--o{ NOTA_FISCAL : "classifica"

    PRODUTOR {
        int id PK
        string nome
    }

    NOTA_FISCAL {
        string chave_acesso PK
        string numero
        double valor
        date data_emissao
        string nome_emitente
        string nome_destinatario
        string documento_emitente
        string documento_destinatario
        int id_produtor FK
        int id_tipo_nota FK
    }

    REBANHO {
        string inscricao PK
        int nascimentos
        int mortes
        int entradas
        int saidas
        int id_produtor FK
    }

    TIPO_NOTA {
        int id PK
        string descricao "RECEITA ou DESPESA"
    }
```

## 🚀 Status do Projeto

🟡 Em desenvolvimento  
🔜 Primeira versão funcional prevista após Sprint 3

---

## 📄 Licença

Projeto de uso restrito e privado.
