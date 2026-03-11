# ACECA — Admin Dashboard

## Stack
- **Frontend:** HTML5 + CSS3 + JavaScript (SPA Vanilla)
- **Backend:** C# ASP.NET Core 10 Web API
- **Database:** MySQL 8+
- **Auth:** JWT Bearer + BCrypt

## Como executar

### Pré-requisitos
- .NET 10 SDK  
- MySQL 8+

### 1. Banco de dados
```bash
mysql -u root -p < aceca_schema.sql
```

### 2. Rodar o projeto
```bash
dotnet run
```
Acesse: **http://localhost:5000**

## Páginas
| URL | Descrição |
|-----|-----------|
| `/` ou `/index.html` | Site público (landing page) |
| `/login.html` | Login de sócio |
| `/dashboard.html` | Admin Dashboard (requer login) |

## Credenciais
| E-mail | Senha | Cargo |
|--------|-------|-------|
| admin@aceca.com.br | Aceca@2025! | Admin |
| alberto@aceca.com.br | Alberto@01 | Presidente |
| carlos@aceca.com.br | Carlos@02 | Vice-Presidente |
| daniel@aceca.com.br | Daniel@321 | Sócio |
| julia@aceca.com.br | Julia@789 | Sócio |

## API Endpoints
| Método | Rota | Descrição |
|--------|------|-----------|
| POST | /api/auth/login | Login → JWT |
| GET/POST | /api/marcas | Listar / Criar marca |
| PUT/DELETE | /api/marcas/{id} | Editar / Excluir marca |
| GET/POST/PUT/DELETE | /api/agendas | CRUD Agenda |
| GET/POST/PUT/DELETE | /api/socios | CRUD Sócios |
| GET/POST/PUT/DELETE | /api/paises | CRUD Países |
| GET/POST/PUT/DELETE | /api/fabricas | CRUD Fábricas |
| GET/POST/PUT/DELETE | /api/dimensoes | CRUD Dimensão |
| GET/POST/PUT/DELETE | /api/fases | CRUD Fases |
| GET/POST/PUT/DELETE | /api/impressoras | CRUD Impressoras |
| GET/POST/PUT/DELETE | /api/tipos | CRUD Tipos |
| GET/POST/PUT/DELETE | /api/subtipos | CRUD Sub-Tipos |
| POST | /api/contato | Formulário de contato |

## Estrutura
```
Aceca.Api/
├── wwwroot/
│   ├── index.html          ← Landing page
│   ├── login.html          ← Login
│   ├── dashboard.html      ← Admin Dashboard SPA
│   ├── css/style.css       ← Estilos landing/login
│   ├── css/dash.css        ← Estilos dashboard
│   ├── js/login.js
│   ├── js/main.js
│   ├── js/dash.js          ← Dashboard SPA engine
│   └── uploads/            ← Imagens enviadas
├── Controllers/
│   ├── AuthController.cs
│   ├── MarcasController.cs
│   ├── CrudControllers.cs  ← Todos os CRUDs em um arquivo
│   └── ContatoController.cs
├── Data/AppDbContext.cs
├── Models/Models.cs
├── Program.cs
├── appsettings.json
├── Aceca.Api.csproj
└── aceca_schema.sql
```
