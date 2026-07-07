# YourRhythm Studio

YourRhythm Studio e um MVP Professor + Aluno para educacao musical. O foco atual e permitir que o professor acompanhe alunos, registre aulas, atribua repertorio, crie missoes, envie feedback e acompanhe progresso/XP. O aluno entra no app para ver missoes, repertorio, feedback e evolucao.

Escola/admin ainda nao e o foco do MVP; a estrutura existe apenas como base de isolamento por `SchoolId`.

## Stack

- ASP.NET Core MVC / Razor Views
- C#
- Entity Framework Core
- Pomelo.EntityFrameworkCore.MySql
- MySQL
- Bootstrap/jQuery assets existentes
- CSS e JavaScript customizados

## Como rodar localmente

1. Configure uma connection string local em `appsettings.Development.json` ou por User Secrets.
2. Garanta que o MySQL local esta acessivel.
3. Restaure e compile:

```bash
dotnet restore
dotnet build
```

4. Aplique as migrations:

```bash
dotnet ef database update
```

5. Rode o app:

```bash
dotnet run
```

## Migrations

Criar nova migration:

```bash
dotnet ef migrations add NomeDaMigration
```

Aplicar migrations:

```bash
dotnet ef database update
```

O projeto inclui uma factory design-time para permitir gerar migrations sem conectar no banco placeholder do `appsettings.json`.

## Contas demo

As contas demo existem apenas para ambiente `Development`.

- `professor@yourrhythm.local`
- `aluno@yourrhythm.local`
- `escola@yourrhythm.local`

A senha demo fica no seed de desenvolvimento e nao deve ser usada como senha de producao.

## Rotas principais

Professor:

- `GET /Teacher/Dashboard`
- `GET /Teacher/Students`
- `GET /Teacher/Students/Create`
- `POST /Teacher/Students/Create`
- `GET /Teacher/Students/{studentId}`
- `POST /Teacher/Students/{studentId}/Lessons`
- `POST /Teacher/Students/{studentId}/Repertoire`
- `POST /Teacher/Students/{studentId}/Assignments`
- `POST /Teacher/Students/{studentId}/Feedback`

Aluno:

- `GET /Student/Dashboard`
- `GET /Student/Assignments`
- `POST /Student/Assignments/{assignmentId}/Start`
- `POST /Student/Assignments/{assignmentId}/Complete`
- `GET /Student/Repertoire`
- `GET /Student/Feedback`
- `GET /Student/Progress`

## Fluxo MVP

1. Professor faz login.
2. Professor cadastra ou acessa um aluno vinculado.
3. Professor registra aula, adiciona repertorio, cria missao e envia feedback.
4. Aluno faz login.
5. Aluno ve missoes, repertorio e feedbacks visiveis.
6. Aluno conclui missao.
7. Sistema concede XP uma unica vez, cria `XpEvent` e recalcula o nivel.

## Segurança backend

- Professor so acessa alunos vinculados via `TeacherStudent`.
- Aluno so acessa os proprios dados.
- Queries filtram por `SchoolId` e perfil autenticado.
- POSTs MVC usam anti-forgery.
- Forms usam ViewModels/DTOs, nao entidades EF diretamente.

## License

Proprietary software. All Rights Reserved.
