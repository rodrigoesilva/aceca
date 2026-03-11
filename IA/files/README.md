# ACECA — Full-Stack

## Tecnologias
- **Frontend:** HTML5 · CSS3 · JavaScript (Vanilla)
- **Backend:** C# ASP.NET Core 10 Web API
- **Banco de Dados:** MySQL 8+
- **Auth:** JWT + BCrypt

## Setup rápido

```bash
# 1. Banco de dados
mysql -u root -p < aceca_schema.sql

# 2. Backend
dotnet run
# → http://localhost:5000
```

## Credenciais de teste
| E-mail | Senha |
|--------|-------|
| admin@aceca.com.br | Aceca@2025! |
| alberto@aceca.com.br | Alberto@01 |
| carlos@aceca.com.br | Carlos@02 |
| daniel@aceca.com.br | Daniel@321 |
| julia@aceca.com.br | Julia@789 |

## Estrutura
```
Aceca.Api/
├── wwwroot/
│   ├── index.html       ← Landing page (segue mock)
│   ├── login.html       ← Página de login
│   ├── css/style.css
│   ├── js/main.js
│   ├── js/login.js
│   └── uploads/         ← Imagens de contato
├── Controllers/
│   ├── AuthController.cs      POST /api/auth/login
│   └── ContatoController.cs   POST /api/contato
├── Data/AppDbContext.cs
├── Models/Models.cs
├── Program.cs
├── appsettings.json
├── Aceca.Api.csproj
└── aceca_schema.sql
```

## API
| Método | Rota | Descrição |
|--------|------|-----------|
| POST | /api/auth/login | Login → JWT token |
| POST | /api/contato | Enviar mensagem + imagens |
