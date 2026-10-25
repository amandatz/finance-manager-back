# Finance Manager — Backend

API para o Finance Manager: um app de finanças para household, com múltiplos membros,
contas pessoais/compartilhadas, cartões de crédito e autenticação própria (login,
cadastro, refresh token).

## Stack

- .NET 10 / ASP.NET Core, Clean Architecture (`Api` → `Application` → `Domain`, `Infrastructure` implementando as abstrações do `Application`)
- EF Core + PostgreSQL
- Autenticação JWT com refresh token (30 dias, sliding, single-use)
- Documentação interativa via Scalar (`/scalar` em desenvolvimento)
- xUnit para testes unitários e de integração

## Rodando localmente

Pré-requisitos: .NET 10 SDK, Docker (para o Postgres).

```bash
docker compose up -d        # sobe o Postgres em 127.0.0.1:5432
dotnet run --project src/FinanceManager.Api
```

A API fica em `http://localhost:5228` (perfil `http` do `launchSettings.json`).

Configure a connection string e a chave de assinatura do JWT fora do controle de versão —
por exemplo em `src/FinanceManager.Api/appsettings.Development.local.json` (ignorado pelo
git) ou via `dotnet user-secrets`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=finance_manager;Username=finance_manager;Password=finance_manager_dev"
  },
  "Jwt": {
    "SigningKey": "<chave de pelo menos 32 caracteres>",
    "Issuer": "finance-manager"
  }
}
```

As migrações do EF Core são aplicadas automaticamente na inicialização em ambiente de
desenvolvimento.

## Testes

```bash
dotnet test
```

`tests/FinanceManager.UnitTests` cobre regras de domínio isoladas;
`tests/FinanceManager.IntegrationTests` sobe a API em memória (via `WebApplicationFactory`)
contra um banco real para testar os endpoints ponta a ponta.
