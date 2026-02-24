-- Script de criação do esquema do banco de dados AutoLCPR (SQLite)
-- Data: 24/02/2026

-- Tabela de Produtores
CREATE TABLE Produtores (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Nome TEXT NOT NULL,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    UpdatedAt TEXT
);

-- Tabela de Rebanhos
CREATE TABLE Rebanhos (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IdRebanho TEXT NOT NULL,
    NomeRebanho TEXT NOT NULL,
    Mortes INTEGER NOT NULL DEFAULT 0,
    Nascimentos INTEGER NOT NULL DEFAULT 0,
    Entradas INTEGER NOT NULL DEFAULT 0,
    Saidas INTEGER NOT NULL DEFAULT 0,
    ProdutorId INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    UpdatedAt TEXT,
    FOREIGN KEY (ProdutorId) REFERENCES Produtores(Id) ON DELETE CASCADE
);

-- Tabela de Notas Fiscais
CREATE TABLE NotasFiscais (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ChaveAcesso TEXT NOT NULL UNIQUE,
    NumeroNotaFiscal TEXT NOT NULL,
    ValorNotaFiscal REAL NOT NULL,
    DataEmissao TEXT NOT NULL,
    NomeEmitente TEXT NOT NULL,
    NomeDestinatario TEXT NOT NULL,
    DocumentoEmitente TEXT NOT NULL,
    DocumentoDestinatario TEXT NOT NULL,
    TipoNota INTEGER NOT NULL CHECK (TipoNota IN (0, 1)), -- 0: Receita, 1: Despesa
    ProdutorId INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    UpdatedAt TEXT,
    FOREIGN KEY (ProdutorId) REFERENCES Produtores(Id) ON DELETE CASCADE
);

-- Índices para melhorar performance
CREATE INDEX IX_Rebanhos_ProdutorId ON Rebanhos(ProdutorId);
CREATE INDEX IX_NotasFiscais_ProdutorId ON NotasFiscais(ProdutorId);
CREATE INDEX IX_NotasFiscais_TipoNota ON NotasFiscais(TipoNota);
CREATE INDEX IX_NotasFiscais_DataEmissao ON NotasFiscais(DataEmissao);
CREATE INDEX IX_NotasFiscais_ChaveAcesso ON NotasFiscais(ChaveAcesso);
