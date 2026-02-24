-- Script de criação do esquema do banco de dados AutoLCPR
-- Data: 24/02/2026

-- Tabela de Produtores
CREATE TABLE Produtores (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Nome NVARCHAR(200) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NULL
);

-- Tabela de Rebanhos
CREATE TABLE Rebanhos (
    Id INT PRIMARY KEY IDENTITY(1,1),
    IdRebanho NVARCHAR(50) NOT NULL,
    NomeRebanho NVARCHAR(200) NOT NULL,
    Mortes INT NOT NULL DEFAULT 0,
    Nascimentos INT NOT NULL DEFAULT 0,
    Entradas INT NOT NULL DEFAULT 0,
    Saidas INT NOT NULL DEFAULT 0,
    ProdutorId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT FK_Rebanhos_Produtores FOREIGN KEY (ProdutorId) 
        REFERENCES Produtores(Id) ON DELETE CASCADE
);

-- Tabela de Notas Fiscais
CREATE TABLE NotasFiscais (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ChaveAcesso NVARCHAR(44) NOT NULL UNIQUE,
    NumeroNotaFiscal NVARCHAR(20) NOT NULL,
    ValorNotaFiscal DECIMAL(18, 2) NOT NULL,
    DataEmissao DATETIME2 NOT NULL,
    NomeEmitente NVARCHAR(200) NOT NULL,
    NomeDestinatario NVARCHAR(200) NOT NULL,
    DocumentoEmitente NVARCHAR(20) NOT NULL,
    DocumentoDestinatario NVARCHAR(20) NOT NULL,
    TipoNota INT NOT NULL CHECK (TipoNota IN (0, 1)), -- 0: Receita, 1: Despesa
    ProdutorId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT FK_NotasFiscais_Produtores FOREIGN KEY (ProdutorId) 
        REFERENCES Produtores(Id) ON DELETE CASCADE
);

-- Índices para melhorar performance
CREATE INDEX IX_Rebanhos_ProdutorId ON Rebanhos(ProdutorId);
CREATE INDEX IX_NotasFiscais_ProdutorId ON NotasFiscais(ProdutorId);
CREATE INDEX IX_NotasFiscais_TipoNota ON NotasFiscais(TipoNota);
CREATE INDEX IX_NotasFiscais_DataEmissao ON NotasFiscais(DataEmissao);
CREATE INDEX IX_NotasFiscais_ChaveAcesso ON NotasFiscais(ChaveAcesso);

-- Comentários das tabelas
EXEC sp_addextendedproperty 
    @name = N'MS_Description', @value = 'Tabela de produtores rurais',
    @level0type = N'SCHEMA', @level0name = 'dbo',
    @level1type = N'TABLE',  @level1name = 'Produtores';

EXEC sp_addextendedproperty 
    @name = N'MS_Description', @value = 'Tabela de rebanhos de cada produtor',
    @level0type = N'SCHEMA', @level0name = 'dbo',
    @level1type = N'TABLE',  @level1name = 'Rebanhos';

EXEC sp_addextendedproperty 
    @name = N'MS_Description', @value = 'Tabela de notas fiscais (receitas e despesas)',
    @level0type = N'SCHEMA', @level0name = 'dbo',
    @level1type = N'TABLE',  @level1name = 'NotasFiscais';

EXEC sp_addextendedproperty 