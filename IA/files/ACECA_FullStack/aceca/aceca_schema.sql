-- ══════════════════════════════════════════════════════════
--  ACECA — Schema MySQL
--  mysql -u root -p < aceca_schema.sql
-- ══════════════════════════════════════════════════════════
CREATE DATABASE IF NOT EXISTS aceca
  CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE USER IF NOT EXISTS 'aceca_user'@'localhost'
  IDENTIFIED BY 'aceca_pass';
GRANT ALL ON aceca.* TO 'aceca_user'@'localhost';
FLUSH PRIVILEGES;
USE aceca;

CREATE TABLE IF NOT EXISTS socios (
  id         INT          PRIMARY KEY AUTO_INCREMENT,
  nome       VARCHAR(120) NOT NULL,
  email      VARCHAR(180) UNIQUE NOT NULL,
  senha_hash VARCHAR(255) NOT NULL,
  cargo      VARCHAR(40)  NOT NULL DEFAULT 'socio',
  associado  DATE,
  foto       VARCHAR(512),
  ativo      TINYINT(1)   NOT NULL DEFAULT 1,
  criado_em  DATETIME     NOT NULL DEFAULT NOW()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS eventos (
  id          INT          PRIMARY KEY AUTO_INCREMENT,
  titulo      VARCHAR(200) NOT NULL,
  descricao   TEXT,
  data        DATE         NOT NULL,
  hora_inicio TIME,
  hora_fim    TIME,
  local       VARCHAR(255),
  tipo        VARCHAR(20)  NOT NULL DEFAULT 'proximo',
  cover_url   VARCHAR(512),
  criado_em   DATETIME     NOT NULL DEFAULT NOW()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS fotos_evento (
  id         INT          PRIMARY KEY AUTO_INCREMENT,
  evento_id  INT          NOT NULL REFERENCES eventos(id) ON DELETE CASCADE,
  url        VARCHAR(512) NOT NULL,
  legenda    VARCHAR(255),
  criado_em  DATETIME     NOT NULL DEFAULT NOW()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS colecoes (
  id        INT          PRIMARY KEY AUTO_INCREMENT,
  socio_id  INT          NOT NULL REFERENCES socios(id) ON DELETE CASCADE,
  nome      VARCHAR(120) NOT NULL,
  tipo      VARCHAR(80),
  descricao TEXT,
  itens     INT          NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS cargos_diretoria (
  id       INT         PRIMARY KEY AUTO_INCREMENT,
  socio_id INT         NOT NULL REFERENCES socios(id) ON DELETE CASCADE,
  cargo    VARCHAR(80) NOT NULL,
  inicio   DATE,
  fim      DATE,
  ativo    TINYINT(1)  NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS contatos (
  id        INT          PRIMARY KEY AUTO_INCREMENT,
  nome      VARCHAR(120) NOT NULL,
  email     VARCHAR(180) NOT NULL,
  telefone  VARCHAR(30),
  assunto   VARCHAR(40)  NOT NULL,
  mensagem  TEXT         NOT NULL,
  anexos    JSON,
  status    VARCHAR(20)  NOT NULL DEFAULT 'novo',
  criado_em DATETIME     NOT NULL DEFAULT NOW()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ── Seed (BCrypt das senhas ─ rode dotnet ef database update para semear) ──
-- Senhas em texto para referência:
--  admin@aceca.com.br   → Aceca@2025!
--  alberto@aceca.com.br → Alberto@01
--  daniel@aceca.com.br  → Daniel@321  (etc.)
