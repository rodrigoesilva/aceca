-- ══════════════════════════════════════════════════════════
--  ACECA — Full Schema (run ONCE before dotnet run)
--  mysql -u root -p < aceca_schema.sql
-- ══════════════════════════════════════════════════════════
CREATE DATABASE IF NOT EXISTS aceca CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER  IF NOT EXISTS 'aceca_user'@'localhost' IDENTIFIED BY 'aceca_pass';
GRANT ALL ON aceca.* TO 'aceca_user'@'localhost';
FLUSH PRIVILEGES;
USE aceca;

CREATE TABLE IF NOT EXISTS socios (
  id int PRIMARY KEY AUTO_INCREMENT,
  nome varchar(120) NOT NULL,
  email varchar(180) UNIQUE NOT NULL,
  senha_hash varchar(255) NOT NULL,
  cargo varchar(40) NOT NULL DEFAULT 'socio',
  associado date,
  foto varchar(512),
  ativo tinyint(1) NOT NULL DEFAULT 1,
  criado_em datetime NOT NULL DEFAULT NOW()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS agendas (
  id int PRIMARY KEY AUTO_INCREMENT,
  titulo varchar(200) NOT NULL,
  descricao text,
  data date NOT NULL,
  hora_inicio time,
  hora_fim time,
  local varchar(255),
  tipo varchar(30) NOT NULL DEFAULT 'proximo',
  cover_url varchar(512),
  criado_em datetime NOT NULL DEFAULT NOW()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS paises (
  id int PRIMARY KEY AUTO_INCREMENT,
  nome varchar(80) NOT NULL,
  sigla varchar(5) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS fabricas (
  id int PRIMARY KEY AUTO_INCREMENT,
  nome varchar(120) NOT NULL,
  cidade varchar(80),
  pais_id int REFERENCES paises(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS dimensoes (
  id int PRIMARY KEY AUTO_INCREMENT,
  nome varchar(80) NOT NULL,
  largura decimal(8,2),
  altura decimal(8,2)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS fases (
  id int PRIMARY KEY AUTO_INCREMENT,
  nome varchar(80) NOT NULL,
  descricao text
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS impressoras (
  id int PRIMARY KEY AUTO_INCREMENT,
  nome varchar(80) NOT NULL,
  modelo varchar(80)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS tipos (
  id int PRIMARY KEY AUTO_INCREMENT,
  nome varchar(80) NOT NULL,
  descricao text
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS subtipos (
  id int PRIMARY KEY AUTO_INCREMENT,
  nome varchar(80) NOT NULL,
  tipo_id int REFERENCES tipos(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS marcas (
  id int PRIMARY KEY AUTO_INCREMENT,
  nome varchar(120) NOT NULL,
  codigo_aceca varchar(30),
  fase_id int REFERENCES fases(id),
  fabrica_id int REFERENCES fabricas(id),
  tipo_id int REFERENCES tipos(id),
  descricao text,
  imagem_url varchar(512),
  imagem_detalhe_url varchar(512),
  incluido_por varchar(120),
  criado_em datetime NOT NULL DEFAULT NOW(),
  atualizado_em datetime NOT NULL DEFAULT NOW()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS colecoes (
  id int PRIMARY KEY AUTO_INCREMENT,
  socio_id int NOT NULL REFERENCES socios(id),
  nome varchar(120) NOT NULL,
  tipo varchar(80),
  descricao text,
  itens int NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS contatos (
  id int PRIMARY KEY AUTO_INCREMENT,
  nome varchar(120) NOT NULL,
  email varchar(180) NOT NULL,
  telefone varchar(30),
  assunto varchar(40) NOT NULL,
  mensagem text NOT NULL,
  anexos json,
  status varchar(20) NOT NULL DEFAULT 'novo',
  criado_em datetime NOT NULL DEFAULT NOW()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Entity Framework Migrations tracking
CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
  `MigrationId` varchar(150) NOT NULL,
  `ProductVersion` varchar(32) NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
