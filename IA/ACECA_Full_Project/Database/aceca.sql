CREATE DATABASE aceca;
USE aceca;

CREATE TABLE Usuarios (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(150),
    Email VARCHAR(150),
    SenhaHash TEXT,
    DataCadastro DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE Produtos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(150),
    Codigo VARCHAR(100),
    Tipo VARCHAR(100),
    Dimensao VARCHAR(100),
    Descricao TEXT,
    Preco DECIMAL(10,2),
    Estoque INT,
    UsuarioId INT,
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id)
);

CREATE TABLE Vendas (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ProdutoId INT,
    CompradorId INT,
    DataVenda DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (ProdutoId) REFERENCES Produtos(Id),
    FOREIGN KEY (CompradorId) REFERENCES Usuarios(Id)
);
