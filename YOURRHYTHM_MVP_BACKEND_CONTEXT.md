# YourRhythm Studio - MVP Backend Context

## 1. Visao geral do backend atual

### Arquitetura encontrada

O projeto e um ASP.NET Core MVC com Razor Views, organizado em camadas simples:

- `Controllers`: entrada HTTP MVC.
- `Views`: telas Razor.
- `Domain`: entidades e constantes de dominio.
- `Application`: servicos de caso de uso/aplicacao.
- `Infrastructure`: configuracao de banco, Foundation e seed demo.
- `Foundation/src`: modulos reutilizaveis de acesso, assistant, secure links, freight e core.

O backend atual mistura duas bases:

- base propria do YourRhythm para escola/professor/aluno via EF Core/MySQL;
- modulo `Foundation.Access` para autenticacao SaaS, contas, tenants, assinaturas e sessoes, atualmente com stores in-memory.

### Principais pastas

- `Controllers/AuthController.cs`: login, logout e access denied.
- `Controllers/DashboardController.cs`: roteia dashboard conforme papel do usuario autenticado.
- `Controllers/HomeController.cs`: landing, privacy e erro.
- `Domain/Users`: entidades iniciais de escola, usuario escolar, professor e aluno.
- `Domain/YourRhythmRoles.cs`: papeis de dominio.
- `Domain/YourRhythmFeatures.cs`: features MVP e planejadas.
- `Application/Users`: servico de criacao/listagem de escolas, professores e alunos.
- `Infrastructure/Data`: `YourRhythmDbContext` e registro de MySQL/EF Core.
- `Infrastructure/Foundation`: injecao dos modulos Foundation e contas demo.

### Entidades existentes

Em `Domain/Users`:

- `School`
  - `Id`, `Name`, `Slug`, `PrimaryEmail`, `OwnerAccountId`, `IsActive`, `CreatedAtUtc`
  - navegacoes: `Users`, `Teachers`, `Students`
- `SchoolUser`
  - `Id`, `SchoolId`, `AccountId`, `DisplayName`, `Email`, `Role`, `IsActive`, `CreatedAtUtc`
- `TeacherProfile`
  - `Id`, `SchoolId`, `SchoolUserId`, `InstrumentFocus`, `Bio`, `CanManageStudents`
- `StudentProfile`
  - `Id`, `SchoolId`, `SchoolUserId`, `Instrument`, `Level`, `Notes`, `CurrentXp`, `CurrentLevel`

Constantes:

- `YourRhythmRoles`: `platform-admin`, `school-owner`, `school-admin`, `teacher`, `student`.
- `YourRhythmFeatures`: students, teachers, repertoire, materials, lessons, gamification, etc.

### Servicos existentes

Em `Application/Users`:

- `IUserDirectoryService`
  - criar escola;
  - criar professor;
  - criar aluno;
  - listar escolas;
  - listar professores por escola;
  - listar alunos por escola.

- `UserDirectoryService`
  - implementa os metodos acima usando `YourRhythmDbContext`.
  - valida existencia da escola ao criar professor/aluno.
  - normaliza email para uppercase.
  - gera slug de escola.

Em `Infrastructure/Foundation`:

- `FoundationServiceCollectionExtensions`
  - registra `Foundation.Core`, `Foundation.Access`, `Foundation.SecureLinks`, `Foundation.Assistant`.
  - usa stores in-memory para acesso, tenants, assinaturas, sessoes, codigos e registros.
  - registra `SaasAccessService`, `AccessService`, `AccessAuthorizationService`.

- `FoundationDemoSeeder`
  - cria admin demo.
  - cria contas demo para escola, professor e aluno.

- contas demo removidas; cleanup legado em `FoundationDemoSeeder`
  - resolve papel de dominio a partir do email demo.

### Controllers existentes

- `AuthController`
  - `GET /Auth/Login`
  - `POST /Auth/Login`
  - `POST /Auth/Logout`
  - `GET /Auth/AccessDenied`
  - cria cookie `YourRhythmCookie`.
  - adiciona claims de conta, tenant, plano, features e papel de dominio demo.

- `DashboardController`
  - protegido por `[Authorize(AuthenticationSchemes = "YourRhythmCookie")]`.
  - `GET /Dashboard/Index`
  - escolhe view `Student`, `Teacher` ou `School` com base na claim `YourRhythmRole`.

- `HomeController`
  - landing, privacy e error.

Nao existem controllers backend para aluno, professor, aulas, repertorio, missoes, feedback ou progresso.

### DbContext/migrations

`YourRhythmDbContext` possui:

- `DbSet<School> Schools`
- `DbSet<SchoolUser> SchoolUsers`
- `DbSet<TeacherProfile> TeacherProfiles`
- `DbSet<StudentProfile> StudentProfiles`

Mapeamentos atuais:

- tabelas: `schools`, `school_users`, `teacher_profiles`, `student_profiles`;
- indices unicos:
  - `School.Slug`;
  - `(SchoolUser.SchoolId, SchoolUser.Email)`;
- relacionamentos com cascade delete entre escola, usuarios, professor e aluno.

Nao foram encontradas migrations no repositorio.

O banco esta configurado em `appsettings.json`:

```json
"DefaultConnection": "server=localhost;port=3306;database=yourrhythmstudio;user=yourrhythm_app;password=CHANGE_ME;"
```

### Autenticacao/autorizacao atual

- Autenticacao por cookie customizado: `YourRhythmCookie`.
- Login via `SaasAccessService.SignInWithPasswordAsync`.
- Logout com anti-forgery.
- Dashboard protegido por cookie.
- Claims relevantes:
  - `ClaimTypes.NameIdentifier`
  - `ClaimTypes.Name`
  - `ClaimTypes.Email`
  - `SessionId`
  - `PlatformRole`
  - `TenantId`
  - `TenantDisplayName`
  - `TenantRole`
  - `PlanCode`
  - `EnabledFeature`
  - `YourRhythmRole`

Limitacao importante: o papel `YourRhythmRole` hoje e resolvido pelas contas demo via email, nao por vinculo real persistido entre `Account`, `SchoolUser`, `TeacherProfile` e `StudentProfile`.

## 2. O que ja existe e pode ser reaproveitado para o MVP

- Estrutura ASP.NET Core MVC pronta.
- Autenticacao por cookie ja funcional.
- Login/logout com anti-forgery.
- Claims de usuario autenticado.
- Papeis base para professor e aluno.
- Entidades iniciais para escola, usuario escolar, professor e aluno.
- DbContext com MySQL/Pomelo configurado.
- Servico `UserDirectoryService` como base para cadastro/listagem de alunos.
- `StudentProfile.CurrentXp` e `StudentProfile.CurrentLevel` como ponto inicial de gamificacao simples.
- `SchoolId` em entidades atuais, util para tenant/escola.
- Dashboards Razor ja desenhados para professor e aluno, mesmo que com dados mockados.
- Modulo Foundation.Access pode continuar sendo usado para conta/sessao, desde que o vinculo com perfil YourRhythm seja consolidado.

## 3. O que ainda falta para Professor + Aluno funcionar de verdade

- Persistir e consultar vinculo real entre conta logada e `SchoolUser`.
- Criar fluxo real de cadastro/listagem/detalhe de alunos para professor.
- Definir relacao professor-aluno.
- Substituir dados mockados dos dashboards por consultas reais.
- Criar entidades para aulas.
- Criar entidades para repertorio/musicas.
- Criar entidades para missoes/tarefas.
- Criar entidades para feedback.
- Criar historico/eventos de progresso/XP.
- Criar controllers ou actions para professor gerenciar seus alunos.
- Criar controllers ou actions para aluno visualizar somente seus dados.
- Criar validacoes server-side de ownership em todos os detalhes/updates.
- Criar migrations depois da aprovacao do modelo.
- Definir se o MVP sera single-tenant simples ou se respeitara escola/tenant desde ja.
- Definir estrategia de criacao de conta do aluno quando professor cadastra aluno.
- Definir se senha inicial sera gerada, fixa temporaria ou convite por email/link.

## 4. Modelo de dominio recomendado

Reaproveitar `School`, `SchoolUser`, `TeacherProfile` e `StudentProfile`.

### Enums minimos

```csharp
public enum LessonStatus
{
    Planned,
    Completed,
    Cancelled
}

public enum AssignmentStatus
{
    Pending,
    InProgress,
    Completed,
    Skipped
}

public enum RepertoireStatus
{
    NotStarted,
    Practicing,
    Learned,
    Archived
}

public enum XpEventType
{
    LessonCompleted,
    AssignmentCompleted,
    PracticeLogged,
    ManualAdjustment
}
```

### Professor-aluno

Entidade recomendada: `TeacherStudent`

Campos minimos:

- `Id`
- `SchoolId`
- `TeacherProfileId`
- `StudentProfileId`
- `CreatedAtUtc`
- `IsActive`

Motivo: evita assumir que todo professor da escola pode acessar todo aluno.

### Aula

Entidade recomendada: `Lesson`

Campos minimos:

- `Id`
- `SchoolId`
- `TeacherProfileId`
- `StudentProfileId`
- `Title`
- `ScheduledForUtc`
- `CompletedAtUtc`
- `Status`
- `Notes`
- `CreatedAtUtc`
- `UpdatedAtUtc`

### Repertorio/musica

Entidade recomendada: `RepertoireItem`

Campos minimos:

- `Id`
- `SchoolId`
- `StudentProfileId`
- `TeacherProfileId`
- `Title`
- `ComposerOrArtist`
- `Instrument`
- `Level`
- `Status`
- `ProgressPercent`
- `Notes`
- `CreatedAtUtc`
- `UpdatedAtUtc`

Opcional futuro:

- `ResourceUrl`
- `SheetMusicUrl`
- `AudioReferenceUrl`

### Missao/tarefa

Entidade recomendada: `Assignment`

Campos minimos:

- `Id`
- `SchoolId`
- `TeacherProfileId`
- `StudentProfileId`
- `LessonId` opcional
- `RepertoireItemId` opcional
- `Title`
- `Description`
- `DueAtUtc`
- `Status`
- `TargetMinutes`
- `CompletedAtUtc`
- `XpReward`
- `CreatedAtUtc`
- `UpdatedAtUtc`

### Feedback

Entidade recomendada: `FeedbackEntry`

Campos minimos:

- `Id`
- `SchoolId`
- `TeacherProfileId`
- `StudentProfileId`
- `LessonId` opcional
- `AssignmentId` opcional
- `RepertoireItemId` opcional
- `Message`
- `CreatedAtUtc`
- `VisibleToStudent`

### Progresso/XP

Entidade recomendada: `XpEvent`

Campos minimos:

- `Id`
- `SchoolId`
- `StudentProfileId`
- `TeacherProfileId` opcional
- `Type`
- `SourceId` opcional
- `Points`
- `Description`
- `CreatedAtUtc`

Manter em `StudentProfile`:

- `CurrentXp`
- `CurrentLevel`

Regra simples para MVP:

- `CurrentXp` e `CurrentLevel` podem ser campos denormalizados atualizados quando uma missao/aula gera XP.
- `XpEvent` guarda o historico auditavel.

## 5. Controllers/rotas necessarios ou ajustes nos existentes

### Ajustes em `AuthController`

- Resolver `YourRhythmRole` a partir de `SchoolUser.Role`; fallback demo foi removido.
- Adicionar claims persistidas:
  - `SchoolUserId`
  - `TeacherProfileId` quando professor
  - `StudentProfileId` quando aluno
  - `SchoolId`
- Manter fallback demo apenas em Development, se necessario.

### Ajustes em `DashboardController`

- Carregar dados reais conforme perfil:
  - professor: resumo dos alunos, aulas proximas, missoes pendentes;
  - aluno: missoes, repertorio, feedback e progresso.
- Nao aceitar IDs de aluno vindos do frontend sem validar vinculo.

### Novo `TeacherStudentsController`

Rotas sugeridas:

- `GET /Teacher/Students`
- `GET /Teacher/Students/Create`
- `POST /Teacher/Students/Create`
- `GET /Teacher/Students/{studentId}`

Responsabilidade:

- listar apenas alunos vinculados ao professor logado;
- cadastrar aluno;
- abrir detalhe consolidado do aluno.

### Novo `LessonsController`

Rotas sugeridas:

- `POST /Teacher/Students/{studentId}/Lessons`
- `GET /Teacher/Students/{studentId}/Lessons/{lessonId}`
- `POST /Teacher/Students/{studentId}/Lessons/{lessonId}/Complete`

### Novo `RepertoireController`

Rotas sugeridas:

- `POST /Teacher/Students/{studentId}/Repertoire`
- `POST /Teacher/Students/{studentId}/Repertoire/{itemId}/Update`
- `GET /Student/Repertoire`

### Novo `AssignmentsController`

Rotas sugeridas:

- `POST /Teacher/Students/{studentId}/Assignments`
- `POST /Teacher/Students/{studentId}/Assignments/{assignmentId}/Update`
- `GET /Student/Assignments`
- `POST /Student/Assignments/{assignmentId}/Start`
- `POST /Student/Assignments/{assignmentId}/Complete`

### Novo `FeedbackController`

Rotas sugeridas:

- `POST /Teacher/Students/{studentId}/Feedback`
- `GET /Student/Feedback`

### Novo `ProgressController`

Rotas sugeridas:

- `GET /Teacher/Students/{studentId}/Progress`
- `GET /Student/Progress`

Para MVC Razor, esses controllers podem retornar views/partials. Para um backend mais limpo, tambem pode ser separado em application services antes de ligar as views.

## 6. Regras de seguranca obrigatorias

- Professor so pode acessar alunos vinculados a ele via `TeacherStudent`.
- Aluno so pode acessar `StudentProfileId` associado a propria conta logada.
- Toda action que recebe `studentId`, `lessonId`, `assignmentId`, `repertoireItemId` ou `feedbackId` deve validar ownership no backend.
- Evitar IDOR: nunca buscar entidade por ID isolado; sempre filtrar por `SchoolId` e pelo perfil logado.
- Nao confiar em IDs vindos do frontend para definir professor/aluno dono do recurso.
- O professor logado deve ser resolvido pelas claims/sessao e pelo banco.
- O aluno logado deve ser resolvido pelas claims/sessao e pelo banco.
- Se `SchoolId`/tenant existir, toda query deve filtrar por ele.
- POST/PUT/DELETE em Razor MVC devem usar anti-forgery.
- Validacao server-side obrigatoria para todos os view models.
- Nao permitir que aluno altere XP, nivel, feedback ou tarefas diretamente sem regra controlada.
- Nao permitir que professor altere dados de aluno fora do vinculo dele.
- Usar view models especificos; nao bindar entidades EF diretamente em forms.
- Validar status transitions:
  - missao concluida nao deve gerar XP duplicado;
  - aula concluida nao deve ser concluida duas vezes;
  - feedback invisivel nao aparece para aluno.

## 7. Ordem recomendada de implementacao

### Etapa 1 - Identidade e vinculo real

- Definir como conta Foundation se conecta a `SchoolUser`.
- Adicionar claims persistidas: `SchoolId`, `SchoolUserId`, `TeacherProfileId`, `StudentProfileId`.
- Criar/ajustar seed real para professor e aluno demo com perfis no banco.

### Etapa 2 - Modelo minimo e migrations

- Aprovar entidades: `TeacherStudent`, `Lesson`, `RepertoireItem`, `Assignment`, `FeedbackEntry`, `XpEvent`.
- Criar DbSets e mapeamentos.
- Criar primeira migration real do MVP.

### Etapa 3 - Application services

- Criar servicos:
  - `TeacherStudentService`
  - `LessonService`
  - `RepertoireService`
  - `AssignmentService`
  - `FeedbackService`
  - `ProgressService`
- Centralizar validacao de ownership nesses servicos.

### Etapa 4 - Professor MVP

- Listar alunos reais do professor.
- Cadastrar aluno.
- Abrir detalhe do aluno.
- Registrar aula.
- Adicionar repertorio.
- Criar missao semanal.
- Adicionar feedback.
- Ver progresso simples.

### Etapa 5 - Aluno MVP

- Dashboard do aluno com dados reais.
- Ver missoes.
- Marcar missao como iniciada/concluida.
- Ver repertorio.
- Ver feedback.
- Ver XP/nivel.

### Etapa 6 - Gamificacao simples

- Definir regra de XP por missao/aula.
- Gerar `XpEvent`.
- Atualizar `StudentProfile.CurrentXp` e `CurrentLevel`.
- Proteger contra duplicidade de XP.

### Etapa 7 - Hardening

- Revisar autorizacao por papel.
- Revisar queries contra IDOR.
- Validar anti-forgery em todos os POSTs.
- Adicionar logs basicos.
- Adicionar testes de regras criticas se houver tempo.

## 8. Arquivos que provavelmente serao alterados

### Backend existente

- `Program.cs`
- `Controllers/AuthController.cs`
- `Controllers/DashboardController.cs`
- `Infrastructure/Foundation/FoundationDemoSeeder.cs`
- `Infrastructure/Foundation/FoundationDemoSeeder.cs` apenas para limpeza de demos legados
- `Infrastructure/Data/YourRhythmDbContext.cs`
- `Infrastructure/Data/DatabaseServiceCollectionExtensions.cs`
- `Application/Users/UserDirectoryService.cs`
- `Application/Users/IUserDirectoryService.cs`
- `Domain/Users/School.cs`
- `Domain/Users/SchoolUser.cs`
- `Domain/Users/TeacherProfile.cs`
- `Domain/Users/StudentProfile.cs`
- `Domain/YourRhythmRoles.cs`

### Novos arquivos provaveis

- `Domain/Learning/TeacherStudent.cs`
- `Domain/Learning/Lesson.cs`
- `Domain/Learning/RepertoireItem.cs`
- `Domain/Learning/Assignment.cs`
- `Domain/Learning/FeedbackEntry.cs`
- `Domain/Learning/XpEvent.cs`
- `Domain/Learning/LessonStatus.cs`
- `Domain/Learning/AssignmentStatus.cs`
- `Domain/Learning/RepertoireStatus.cs`
- `Domain/Learning/XpEventType.cs`
- `Application/Learning/*`
- `Controllers/TeacherStudentsController.cs`
- `Controllers/LessonsController.cs`
- `Controllers/RepertoireController.cs`
- `Controllers/AssignmentsController.cs`
- `Controllers/FeedbackController.cs`
- `Controllers/ProgressController.cs`
- `ViewModels/Teacher/*`
- `ViewModels/Student/*`

### Views que provavelmente serao ligadas a dados reais

- `Views/Dashboard/Teacher.cshtml`
- `Views/Dashboard/Student.cshtml`
- `Views/Shared/_DashboardLayout.cshtml`
- novas views em `Views/TeacherStudents`, `Views/Lessons`, `Views/Repertoire`, `Views/Assignments`, `Views/Feedback`.

## 9. Duvidas/decisoes antes de codar

1. O MVP sera single-school/single-tenant por enquanto ou ja precisa respeitar multiplas escolas desde o primeiro deploy?
2. Quando professor cadastra aluno, deve criar uma conta de login automaticamente?
3. Como o aluno recebe acesso: senha inicial, link de convite, email manual ou codigo?
4. Professor pode ter todos os alunos da escola ou apenas alunos explicitamente vinculados a ele?
5. Um aluno pode ter mais de um professor no MVP?
6. Aula precisa ter data/hora obrigatoria ou pode ser apenas registro retroativo?
7. Missao semanal precisa aceitar recorrencia ou apenas tarefa avulsa com prazo?
8. Aluno pode marcar missao como concluida sozinho ou o professor precisa aprovar?
9. XP sera automatico por conclusao de missao/aula ou professor pode ajustar manualmente?
10. Nivel sera calculado por formula fixa ou salvo como campo editavel?
11. Feedback e sempre visivel ao aluno ou professor pode deixar anotacao privada?
12. Repertorio precisa de upload/anexo agora ou somente titulo, artista/compositor e progresso?
13. O MVP deve usar apenas Razor MVC ou preparar API JSON para futuro app/mobile?
14. Devemos manter Foundation.Access in-memory no MVP ou persistir contas/sessoes em banco antes de uso real?
15. Qual e o criterio minimo de "pronto para uso real" no primeiro aluno: apenas fluxo professor-aluno ou tambem recuperacao de senha, email e logs?
