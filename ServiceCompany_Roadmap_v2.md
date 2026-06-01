# ServiceCompany — Production Roadmap v2.0

> **Назначение документа:** пошаговый roadmap для создания информационной системы сервисной компании.
> Каждый чекпоинт самодостаточен: содержит цель, контекст, задачи, технические решения, структуру файлов, критерии готовности и типичные ошибки.
> Документ оптимизирован для использования языковой моделью при генерации и сопровождении кода.

---

## Глоссарий (Ubiquitous Language)

Единый словарь терминов. Использовать **только эти термины** в коде, БД, API, UI.

| Термин (рус) | Термин в коде | Определение |
|---|---|---|
| Клиент | `Client` | Юридическое или физическое лицо, с которым заключён договор обслуживания |
| Контактное лицо | `ContactPerson` | Представитель клиента для связи |
| Договор | `Contract` | Юридический документ, определяющий условия обслуживания и SLA |
| Объект обслуживания | `ServiceObject` | Физический адрес/локация, привязанный к клиенту, на котором выполняются работы |
| Оборудование | `Equipment` | Конкретная единица техники на объекте (котёл, кондиционер и т.д.) |
| Заявка | `Ticket` | Запрос на выполнение работ: аварийная, плановая или консультация |
| Плановое ТО | `MaintenancePlan` | Регламент периодического обслуживания оборудования |
| Акт выполненных работ | `WorkAct` | Документ, подтверждающий выполнение работ по заявке |
| Счёт | `Invoice` | Финансовый документ на оплату выполненных работ |
| Инженер | `Engineer` | Сотрудник, выполняющий работы на объекте (роль `Engineer`) |
| Менеджер | `Manager` | Сотрудник, управляющий заявками и клиентами (роль `Manager`) |
| SLA | `SlaPolicy` | Service Level Agreement — допустимые сроки реакции и решения |
| Приоритет | `TicketPriority` | Критичность заявки: `Critical`, `High`, `Normal`, `Low` |

---

## Нефункциональные требования (NFR)

Зафиксировать ДО начала разработки. Влияют на все архитектурные решения.

| Параметр | Значение для MVP |
|---|---|
| Одновременных пользователей | до 50 |
| Клиентов в базе | до 500 |
| Заявок в месяц | до 2 000 |
| Оборудования всего | до 10 000 |
| Время отклика API (p95) | < 500ms |
| Доступность | 99% (допустимо плановое обслуживание) |
| RTO (Recovery Time Objective) | 4 часа |
| RPO (Recovery Point Objective) | 1 час |
| Целевые браузеры | Chrome, Firefox, Safari (последние 2 версии) |
| Мобильная поддержка | Responsive web (PWA не в MVP) |
| Языки интерфейса | Русский (основной) |

---

## Архитектура

### Подход: Clean Architecture + Modular Monolith

```
ServiceCompany.sln
├── src/
│   ├── ServiceCompany.Domain            # Сущности, value objects, domain events, enums, interfaces
│   ├── ServiceCompany.Application       # Use cases (CQRS handlers), валидация, DTO, interfaces сервисов
│   ├── ServiceCompany.Infrastructure    # EF Core, репозитории, email, file storage, Hangfire, внешние сервисы
│   ├── ServiceCompany.Api              # Controllers, middleware, filters, DI composition root
│   └── ServiceCompany.Frontend         # React SPA (Vite + TypeScript)
├── tests/
│   ├── ServiceCompany.UnitTests        # Domain + Application logic
│   ├── ServiceCompany.IntegrationTests # API + Infrastructure (TestContainers)
│   └── ServiceCompany.E2ETests         # Playwright (добавить в CP4)
└── docker/
    └── docker-compose.yml
```

### Правила зависимостей (строго соблюдать)

```
Domain ← ничего не зависит ни от чего
Application ← зависит только от Domain
Infrastructure ← зависит от Domain и Application
Api ← зависит от Application и Infrastructure (composition root)
```

### Ключевые технологические решения

| Категория | Решение | Обоснование |
|---|---|---|
| ORM | EF Core 8+ | Стандарт для .NET, migrations, LINQ |
| CQRS | MediatR | Decoupling handlers, pipeline behaviors |
| Валидация | FluentValidation | MediatR pipeline behavior |
| Result pattern | собственный `Result<T>` | Минимум зависимостей, 1 файл в Domain |
| Domain Events | MediatR `INotification` | Уже есть MediatR, не нужна отдельная библиотека |
| Логирование | Serilog + Seq | Structured logging, correlation ID |
| Background Jobs | Hangfire + PostgreSQL storage | Dashboard из коробки, recurring jobs, проще Quartz |
| Auth | ASP.NET Core Identity + JWT | Стандарт, не изобретать |
| File Storage | Локальная файловая система → MinIO (production) | Простота для MVP, S3-совместимый API для миграции |
| PDF генерация | QuestPDF | Declarative API, бесплатный для малого бизнеса |
| Excel генерация | ClosedXML | Бесплатный, без лицензионных проблем EPPlus |
| State Machine | Stateless library | Явные transitions, guards, визуализация графа |
| Маппинг | Mapster | Быстрее AutoMapper, меньше конфигурации |
| API docs | Swagger / Swashbuckle | Стандарт |
| Тесты | xUnit + FluentAssertions + TestContainers | Стандартный стек |
| Frontend | React 18 + TypeScript + Vite | Быстрый dev server, строгая типизация |
| UI Kit | Ant Design | Готовые компоненты для enterprise (таблицы, формы, kanban) |
| HTTP client | Axios + React Query | Кэширование, retry, loading states |
| Карты | Leaflet + OpenStreetMap | Бесплатно, без ограничений API |

---

## Checkpoint 0 — Discovery и проектирование

### Цель
Зафиксировать границы системы, доменную модель и интеграции до написания кода.

### Контекст
Всё, что не определено здесь, станет source of bugs позже. Инвестиция в проектирование окупается 10x.

### Задачи

#### 0.1 Bounded Contexts (финальный список)
1. **Identity & Access** — пользователи, роли, аутентификация, аудит
2. **CRM** — клиенты, контактные лица, договоры
3. **Objects & Equipment** — объекты обслуживания, оборудование, статусы
4. **Service Desk** — заявки, жизненный цикл, SLA, назначения
5. **Maintenance** — планы ТО, расписания, автогенерация заявок
6. **Finance** — акты, счета, задолженности, экспорт
7. **Reporting** — дашборды, метрики, отчёты
8. **Geo** — карта объектов, маршруты (CP8, не MVP)
9. **Notifications** — email, in-app уведомления (сквозной модуль)

#### 0.2 ERD (ключевые сущности и связи)

```
Client 1──* ContactPerson
Client 1──* Contract
Client 1──* ServiceObject
ServiceObject 1──* Equipment
ServiceObject *──1 Contract (через Client)
ServiceObject имеет SlaPolicy (наследуется от Contract, переопределяется на объекте)

Ticket *──1 ServiceObject
Ticket *──1 Equipment (опционально)
Ticket *──1 User (assignedEngineer)
Ticket *──1 User (createdBy)
Ticket 1──* TicketComment
Ticket 1──* TicketAttachment
Ticket 1──* TicketStatusHistory
Ticket 1──1 WorkAct (после закрытия)

MaintenancePlan *──1 ServiceObject
MaintenancePlan *──1 Equipment (опционально)
MaintenancePlan генерирует Ticket (type = Scheduled)

WorkAct *──1 Ticket
WorkAct 1──* Invoice

User имеет Role: Admin, Manager, Engineer, Accountant
```

#### 0.3 SLA модель (критически важно определить сейчас)

```
SlaPolicy:
  - ResponseTimeMinutes: int       # время от создания до первой реакции
  - ResolutionTimeMinutes: int     # время от создания до закрытия
  - BusinessHoursOnly: bool        # считать только рабочие часы (9:00-18:00 пн-пт)
  - PausedOnStatuses: string[]     # SLA приостанавливается (напр. WaitingParts)

Иерархия SLA (от общего к частному, каждый уровень переопределяет предыдущий):
  1. Глобальный default (в настройках системы)
  2. Contract level (в договоре)
  3. ServiceObject level (на объекте)
  4. Per Priority override (Critical = 2h response, Normal = 8h response)
```

#### 0.4 Integration Map

| Система | Направление | Формат | Когда |
|---|---|---|---|
| 1С Бухгалтерия | Экспорт | XML (CommerceML 2) файловый обмен | CP7 |
| Email (SMTP) | Отправка | SMTP через абстракцию `IEmailSender` | CP2 |
| Telegram Bot | Отправка уведомлений инженерам | HTTP API | Post-MVP |

#### 0.5 User Flows (основные)

**Flow 1 — Аварийная заявка:**
Клиент звонит → Менеджер создаёт Ticket (priority=Critical) → Система считает SLA deadline → Менеджер назначает Engineer → Engineer выполняет → Engineer закрывает → Система создаёт WorkAct → Accountant формирует Invoice

**Flow 2 — Плановое ТО:**
Hangfire trigger → Система создаёт Ticket (type=Scheduled) из MaintenancePlan → Manager видит в списке → назначает Engineer → Engineer выполняет чеклист → закрывает

**Flow 3 — Эскалация:**
Ticket SLA deadline приближается (80% времени) → Notification менеджеру → SLA нарушен → Notification руководителю → Ticket помечается как Overdue

#### 0.6 RBAC Matrix

| Действие | Admin | Manager | Engineer | Accountant |
|---|---|---|---|---|
| CRUD пользователей | ✅ | ❌ | ❌ | ❌ |
| CRUD клиентов | ✅ | ✅ | ❌ | Только просмотр |
| CRUD объектов | ✅ | ✅ | Только просмотр | ❌ |
| CRUD оборудования | ✅ | ✅ | Только просмотр | ❌ |
| Создание заявки | ✅ | ✅ | ❌ | ❌ |
| Назначение инженера | ✅ | ✅ | ❌ | ❌ |
| Выполнение заявки | ❌ | ❌ | ✅ (только свои) | ❌ |
| Комментарии к заявке | ✅ | ✅ | ✅ (только свои) | ❌ |
| Формирование акта | ✅ | ✅ | ❌ | ✅ |
| Формирование счёта | ❌ | ❌ | ❌ | ✅ |
| Просмотр отчётов | ✅ | ✅ | Только свои | Только финансовые |
| Настройки системы | ✅ | ❌ | ❌ | ❌ |
| Планы ТО | ✅ | ✅ | Только просмотр | ❌ |

### Артефакты CP0
- [ ] ERD диаграмма (PostgreSQL)
- [ ] Глоссарий (этот документ)
- [ ] RBAC matrix (этот документ)
- [ ] SLA model definition (этот документ)
- [ ] Integration map (этот документ)
- [ ] User flow диаграммы (sequence diagrams)
- [ ] Список API endpoints v1 (OpenAPI draft)
- [ ] NFR документ (этот документ)

### Критерий готовности CP0
Вся команда может ответить на вопрос: «Что делает система, для кого, какие данные хранит, с чем интегрируется» — без разногласий.

---

## Checkpoint 1 — Solution skeleton и инфраструктурный фундамент

### Цель
Создать рабочий каркас, в котором невозможно (или сложно) нарушить архитектурные правила.

### Контекст
Этот чекпоинт определяет качество всего проекта. Каждый последующий модуль строится на этом фундаменте. Ошибки здесь = рефакторинг везде.

### 1.1 Создание solution structure

```bash
dotnet new sln -n ServiceCompany
dotnet new webapi -n ServiceCompany.Api -o src/ServiceCompany.Api
dotnet new classlib -n ServiceCompany.Domain -o src/ServiceCompany.Domain
dotnet new classlib -n ServiceCompany.Application -o src/ServiceCompany.Application
dotnet new classlib -n ServiceCompany.Infrastructure -o src/ServiceCompany.Infrastructure
dotnet new xunit -n ServiceCompany.UnitTests -o tests/ServiceCompany.UnitTests
dotnet new xunit -n ServiceCompany.IntegrationTests -o tests/ServiceCompany.IntegrationTests
```

Добавить все проекты в sln. Настроить references согласно правилам зависимостей (см. раздел Архитектура).

### 1.2 Domain project

#### Base Entity и Audit

```csharp
// Domain/Common/BaseEntity.cs
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } // soft delete

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void AddDomainEvent(IDomainEvent e) => _domainEvents.Add(e);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

#### Domain Events interface

```csharp
// Domain/Common/IDomainEvent.cs
public interface IDomainEvent : MediatR.INotification
{
    DateTime OccurredAt { get; }
}
```

#### Result pattern

```csharp
// Domain/Common/Result.cs
public class Result
{
    public bool IsSuccess { get; }
    public string Error { get; }
    protected Result(bool isSuccess, string error) { IsSuccess = isSuccess; Error = error; }
    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string error) => new(false, error);
    public static Result<T> Success<T>(T value) => new(value, true, string.Empty);
    public static Result<T> Failure<T>(string error) => new(default, false, error);
}

public class Result<T> : Result
{
    public T? Value { get; }
    protected internal Result(T? value, bool isSuccess, string error) : base(isSuccess, error) { Value = value; }
}
```

#### Enums (определить сразу все)

```csharp
// Domain/Enums/
public enum TicketStatus { New, Assigned, InProgress, WaitingParts, Done, Closed, Cancelled }
public enum TicketPriority { Critical, High, Normal, Low }
public enum TicketType { Emergency, Scheduled, Consultation }
public enum EquipmentStatus { Active, UnderRepair, Decommissioned, Replaced }
public enum ContractStatus { Draft, Active, Suspended, Expired, Terminated }
public enum InvoiceStatus { Draft, Sent, Paid, Overdue, Cancelled }
public enum PaymentStatus { Unpaid, PartiallyPaid, Paid }
```

#### Сущности (объявить все как stub с ключевыми свойствами)

Полная реализация сущностей будет в соответствующих чекпоинтах. Здесь — skeleton.

```csharp
// Domain/Entities/ — по файлу на каждую сущность
public class Client : BaseEntity { }
public class ContactPerson : BaseEntity { }
public class Contract : BaseEntity { }
public class ServiceObject : BaseEntity { }
public class Equipment : BaseEntity { }
public class Ticket : BaseEntity { }
public class TicketComment : BaseEntity { }
public class TicketAttachment : BaseEntity { }
public class TicketStatusHistory : BaseEntity { }
public class MaintenancePlan : BaseEntity { }
public class WorkAct : BaseEntity { }
public class Invoice : BaseEntity { }
public class SlaPolicy : BaseEntity { }
public class Notification : BaseEntity { }
```

### 1.3 Application project

#### MediatR + FluentValidation pipeline

```csharp
// Application/Common/Behaviors/ValidationBehavior.cs
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // Inject IEnumerable<IValidator<TRequest>>, validate before handler
}

// Application/Common/Behaviors/LoggingBehavior.cs
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    // Log request name, user, duration
}
```

#### Interfaces (в Application, реализация в Infrastructure)

```csharp
// Application/Common/Interfaces/
public interface IApplicationDbContext { /* DbSet<T> properties */ }
public interface ICurrentUserService { string UserId { get; } string Role { get; } }
public interface IEmailSender { Task SendAsync(string to, string subject, string body, CancellationToken ct); }
public interface IFileStorageService { Task<string> UploadAsync(Stream file, string fileName, CancellationToken ct); }
public interface IDateTimeService { DateTime UtcNow { get; } }
```

### 1.4 Infrastructure project

#### EF Core DbContext

```csharp
// Infrastructure/Persistence/AppDbContext.cs
public class AppDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    // DbSets for all entities
    // Override SaveChangesAsync: auto-fill audit fields, dispatch domain events, apply soft delete filter
}
```

#### Global Query Filter для soft delete

```csharp
// В OnModelCreating:
builder.Entity<Client>().HasQueryFilter(x => !x.IsDeleted);
// ... для всех сущностей
```

#### SaveChanges interceptor для audit

```csharp
// Автоматически заполнять CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
// Автоматически dispatch domain events через MediatR после SaveChanges
```

### 1.5 Api project

#### Global exception middleware

```csharp
// Api/Middleware/GlobalExceptionMiddleware.cs
// Ловит все exceptions, логирует через Serilog, возвращает ProblemDetails
// Маппинг: ValidationException → 400, NotFoundException → 404, UnauthorizedAccessException → 403, остальное → 500
```

#### Serilog с correlation ID

```csharp
// Program.cs
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.WithCorrelationId()
    .WriteTo.Console()
    .WriteTo.Seq("http://seq:5341"));

// Middleware: добавить X-Correlation-Id в каждый request/response
```

#### API Versioning (определить сразу)

```csharp
// URL path versioning: /api/v1/tickets
builder.Services.AddApiVersioning(o => {
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.ApiVersionReader = new UrlSegmentApiVersionReader();
});
```

#### Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString)
    .AddHangfire();
```

#### Pagination (базовый класс, использовать везде)

```csharp
// Application/Common/Models/
public class PaginatedRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    // Max 100, валидация
}

public class PaginatedResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
```

### 1.6 Frontend skeleton

```bash
npm create vite@latest ServiceCompany.Frontend -- --template react-ts
cd ServiceCompany.Frontend
npm install antd @ant-design/icons axios @tanstack/react-query react-router-dom dayjs
```

Структура:
```
src/
├── api/          # axios instance, endpoints
├── components/   # shared UI components
├── features/     # по модулям: clients/, tickets/, maintenance/...
├── hooks/        # custom hooks
├── layouts/      # MainLayout, AuthLayout
├── pages/        # роутинг
├── store/        # auth context
├── types/        # TypeScript interfaces (сгенерированные из API)
└── utils/        # helpers
```

### 1.7 Docker Compose

```yaml
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: servicecompany
      POSTGRES_USER: sc_user
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    ports: ["5432:5432"]
    volumes: ["pgdata:/var/lib/postgresql/data"]

  seq:
    image: datalust/seq
    environment:
      ACCEPT_EULA: "Y"
    ports: ["5341:5341", "8081:80"]

  mailhog:
    image: mailhog/mailhog
    ports: ["1025:1025", "8025:8025"]

  api:
    build: ./src/ServiceCompany.Api
    depends_on: [postgres, seq]
    environment:
      ConnectionStrings__Default: "Host=postgres;Database=servicecompany;Username=sc_user;Password=${DB_PASSWORD}"
    ports: ["5000:8080"]

  frontend:
    build: ./src/ServiceCompany.Frontend
    ports: ["3000:3000"]

volumes:
  pgdata:
```

### 1.8 Тестовый фундамент

```csharp
// tests/ServiceCompany.UnitTests/Domain/ — тесты domain logic
// tests/ServiceCompany.IntegrationTests/
//   CustomWebApplicationFactory.cs — TestContainers для PostgreSQL
//   базовый IntegrationTest base class
```

Правило: каждый чекпоинт включает тесты. Минимальный порог:
- Domain logic: 80% покрытие
- Application handlers: happy path + основные ошибки
- API: integration test на каждый endpoint

### Критерий готовности CP1
- [ ] `dotnet build` — без ошибок и warnings
- [ ] `dotnet ef database update` — миграции применяются
- [ ] `docker-compose up` — все контейнеры стартуют
- [ ] Swagger UI доступен на `/swagger`
- [ ] Health check endpoint возвращает `Healthy`
- [ ] Frontend запускается, показывает пустую страницу с layout
- [ ] Serilog пишет structured logs в Seq
- [ ] Unit test runner работает, есть хотя бы 1 проходящий тест
- [ ] Correlation ID проходит через request → logs → response header

### Типичные ошибки CP1
- Не настроить global query filter для soft delete → потом будут показываться удалённые записи
- Забыть dispatch domain events в SaveChanges → события не работают в CP4
- Не определить API versioning → потом ломать клиентов
- Pagination не заложить → переделывать каждый endpoint

---

## Checkpoint 2 — Identity, JWT, роли, безопасность

### Цель
Полная система аутентификации и авторизации. После CP2 каждый endpoint защищён.

### Контекст
Безопасность до бизнес-логики. Если добавлять позже, половина endpoints будут без защиты.

### 2.1 Backend — ASP.NET Core Identity + JWT

#### Модель пользователя

```csharp
// Infrastructure/Identity/ApplicationUser.cs
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
}
```

#### Роли (seed при старте)

```csharp
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Engineer = "Engineer";
    public const string Accountant = "Accountant";
}
```

#### JWT Configuration

```csharp
// appsettings.json
"JwtSettings": {
    "Secret": "from-environment-variable",  // минимум 256 бит
    "Issuer": "ServiceCompany",
    "Audience": "ServiceCompanyApp",
    "AccessTokenExpirationMinutes": 30,
    "RefreshTokenExpirationDays": 7
}
```

#### Auth endpoints

```
POST /api/v1/auth/login         → { accessToken, refreshToken, user }
POST /api/v1/auth/refresh        → { accessToken, refreshToken }
POST /api/v1/auth/logout         → revoke refresh token
POST /api/v1/auth/forgot-password → send email via IEmailSender
POST /api/v1/auth/reset-password  → set new password by token
```

#### Policy-based authorization

```csharp
// Policies определяют ЧТО можно делать, а не КТО
builder.Services.AddAuthorization(o => {
    o.AddPolicy("CanManageClients", p => p.RequireRole(AppRoles.Admin, AppRoles.Manager));
    o.AddPolicy("CanManageTickets", p => p.RequireRole(AppRoles.Admin, AppRoles.Manager));
    o.AddPolicy("CanExecuteTickets", p => p.RequireRole(AppRoles.Engineer));
    o.AddPolicy("CanManageFinance", p => p.RequireRole(AppRoles.Admin, AppRoles.Accountant));
    o.AddPolicy("CanViewReports", p => p.RequireRole(AppRoles.Admin, AppRoles.Manager));
});
```

### 2.2 Audit Trail Framework

Реализуется ЗДЕСЬ, используется во всех последующих чекпоинтах.

```csharp
// Domain/Entities/AuditEntry.cs
public class AuditEntry : BaseEntity
{
    public string EntityName { get; set; }
    public string EntityId { get; set; }
    public string Action { get; set; }     // Create, Update, Delete, Login, Logout, FailedLogin
    public string? OldValues { get; set; }  // JSON
    public string? NewValues { get; set; }  // JSON
    public string UserId { get; set; }
    public string IpAddress { get; set; }
}

// Infrastructure/Persistence/AuditInterceptor.cs
// EF Core SaveChanges interceptor: автоматически записывает изменения в AuditEntry
```

### 2.3 Notification Framework (базовый, расширяется в CP4+)

```csharp
// Application/Common/Interfaces/INotificationService.cs
public interface INotificationService
{
    Task SendAsync(string userId, string title, string message, CancellationToken ct);
}

// Domain/Entities/Notification.cs
public class Notification : BaseEntity
{
    public string UserId { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}

// Infrastructure реализация: сохранение в БД + email (через IEmailSender)
```

### 2.4 Frontend

- Login page (email + password)
- Protected routes (React Router + auth context)
- Axios interceptor: attach JWT, auto-refresh on 401
- Role-based route guards: `<RoleGuard roles={["Manager", "Admin"]}>`
- Layout per role: sidebar menu зависит от роли
- Logout, профиль пользователя (read-only)

### 2.5 Тесты CP2
- Unit: JWT token generation/validation
- Integration: login → получить token → вызвать protected endpoint
- Integration: refresh token flow
- Integration: доступ без роли → 403
- Integration: expired token → 401

### Критерий готовности CP2
- [ ] Login/logout работает end-to-end
- [ ] JWT access token + refresh token flow
- [ ] Каждый endpoint имеет `[Authorize]` с policy
- [ ] Manager не видит admin endpoints → 403
- [ ] Engineer не видит finance endpoints → 403
- [ ] Audit log записывает: login, failed login, logout
- [ ] Password reset flow через email (MailHog в dev)
- [ ] Frontend: protected routes redirect на login
- [ ] Frontend: menu отображается по роли

### Типичные ошибки CP2
- Хранить JWT secret в appsettings.json → использовать environment variable
- Не ограничить failed login attempts → brute force (добавить lockout: 5 попыток = блокировка 15 мин)
- Refresh token без revocation → при logout старый refresh token работает
- Не логировать failed auth attempts → не видно атак

---

## Checkpoint 3 — Клиенты + Объекты + Оборудование (Master Data)

### Цель
Построить фундамент данных. Без master data невозможен Service Desk.

### Контекст
Это «справочники» системы. Данные меняются редко, читаются часто. Важна целостность связей.

### 3.1 Клиенты (Client)

#### Domain

```csharp
public class Client : BaseEntity
{
    public string Name { get; set; }                    // название организации
    public string? Inn { get; set; }                    // ИНН (nullable для физлиц)
    public string? LegalAddress { get; set; }
    public string? ActualAddress { get; set; }
    public string Phone { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public List<ContactPerson> ContactPersons { get; set; } = new();
    public List<Contract> Contracts { get; set; } = new();
    public List<ServiceObject> ServiceObjects { get; set; } = new();
}

public class ContactPerson : BaseEntity
{
    public Guid ClientId { get; set; }
    public string FullName { get; set; }
    public string Phone { get; set; }
    public string? Email { get; set; }
    public string? Position { get; set; }
    public bool IsPrimary { get; set; }
}

public class Contract : BaseEntity
{
    public Guid ClientId { get; set; }
    public string Number { get; set; }              // номер договора
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ContractStatus Status { get; set; }
    public decimal? Amount { get; set; }
    public string? Description { get; set; }

    // SLA по умолчанию для этого договора
    public int DefaultResponseTimeMinutes { get; set; } = 480;     // 8 часов
    public int DefaultResolutionTimeMinutes { get; set; } = 2880;  // 2 рабочих дня
    public bool SlaBusinessHoursOnly { get; set; } = true;
}
```

#### API endpoints

```
GET    /api/v1/clients                  → PaginatedResult<ClientListDto>
GET    /api/v1/clients/{id}             → ClientDetailDto (с contacts, contracts)
POST   /api/v1/clients                  → CreateClientCommand
PUT    /api/v1/clients/{id}             → UpdateClientCommand
DELETE /api/v1/clients/{id}             → soft delete

GET    /api/v1/clients/{id}/contacts    → List<ContactPersonDto>
POST   /api/v1/clients/{id}/contacts    → CreateContactPersonCommand
PUT    /api/v1/contacts/{id}            → UpdateContactPersonCommand
DELETE /api/v1/contacts/{id}            → soft delete

GET    /api/v1/clients/{id}/contracts   → List<ContractDto>
POST   /api/v1/clients/{id}/contracts   → CreateContractCommand
PUT    /api/v1/contracts/{id}           → UpdateContractCommand
```

#### Frontend
- Таблица клиентов с поиском, фильтрами, пагинацией (Ant Design Table)
- Карточка клиента: табы — Информация, Контакты, Договоры, Объекты, История заявок
- Формы создания/редактирования (Ant Design Form)

### 3.2 Объекты обслуживания (ServiceObject)

#### Domain

```csharp
public class ServiceObject : BaseEntity
{
    public Guid ClientId { get; set; }
    public string Name { get; set; }                // "Офис на Абая 1" 
    public string Address { get; set; }
    public double? Latitude { get; set; }           // простой double, не PostGIS в MVP
    public double? Longitude { get; set; }
    public string? Description { get; set; }

    // SLA override (если null — берётся из Contract)
    public int? ResponseTimeMinutesOverride { get; set; }
    public int? ResolutionTimeMinutesOverride { get; set; }

    // Navigation
    public Client Client { get; set; }
    public List<Equipment> Equipment { get; set; } = new();
    public List<Ticket> Tickets { get; set; } = new();
}
```

**Решение по geo:** В MVP координаты хранятся как `double Latitude/Longitude`. PostGIS добавляется только в CP8, когда нужны пространственные запросы. Миграция: `ALTER TABLE` + `UPDATE ... SET geom = ST_MakePoint(longitude, latitude)`.

#### API endpoints

```
GET    /api/v1/clients/{clientId}/objects    → PaginatedResult<ServiceObjectListDto>
GET    /api/v1/objects/{id}                  → ServiceObjectDetailDto
POST   /api/v1/clients/{clientId}/objects    → CreateServiceObjectCommand
PUT    /api/v1/objects/{id}                  → UpdateServiceObjectCommand
DELETE /api/v1/objects/{id}                  → soft delete (только если нет активных заявок)
```

#### Frontend
- Список объектов клиента
- Карточка объекта: адрес, карта (Leaflet pin), оборудование, история заявок

### 3.3 Оборудование (Equipment)

#### Domain

```csharp
public class Equipment : BaseEntity
{
    public Guid ServiceObjectId { get; set; }
    public string Name { get; set; }                // "Котёл Viessmann Vitodens 100"
    public string? SerialNumber { get; set; }       // unique per ServiceObject
    public string EquipmentType { get; set; }       // справочник: "Котёл", "Кондиционер", etc.
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public DateTime? InstallDate { get; set; }
    public DateTime? WarrantyEndDate { get; set; }
    public EquipmentStatus Status { get; set; } = EquipmentStatus.Active;
    public string? Notes { get; set; }

    // Domain method
    public Result ChangeStatus(EquipmentStatus newStatus)
    {
        // валидация допустимых переходов:
        // Active → UnderRepair, Decommissioned
        // UnderRepair → Active, Decommissioned
        // Decommissioned → Replaced
        // Replaced → ничего (terminal state)
    }
}
```

#### Unique constraint
```sql
-- В EF Core migration
CREATE UNIQUE INDEX IX_Equipment_SerialNumber_ServiceObjectId
ON "Equipment" ("SerialNumber", "ServiceObjectId")
WHERE "SerialNumber" IS NOT NULL AND "IsDeleted" = false;
```

#### API endpoints

```
GET    /api/v1/objects/{objectId}/equipment  → PaginatedResult<EquipmentListDto>
GET    /api/v1/equipment/{id}                → EquipmentDetailDto
POST   /api/v1/objects/{objectId}/equipment  → CreateEquipmentCommand
PUT    /api/v1/equipment/{id}                → UpdateEquipmentCommand
PATCH  /api/v1/equipment/{id}/status         → ChangeEquipmentStatusCommand
```

#### Frontend
- Список оборудования объекта с фильтрацией по типу и статусу
- Карточка единицы оборудования: история заявок, история замен

### 3.4 Справочники (Dictionaries)

Вынести в отдельные endpoints, чтобы frontend мог заполнять dropdowns:

```
GET /api/v1/dictionaries/equipment-types     → ["Котёл", "Кондиционер", "Вентиляция", ...]
GET /api/v1/dictionaries/ticket-priorities    → enum values
GET /api/v1/dictionaries/ticket-statuses      → enum values
```

### 3.5 Тесты CP3
- Unit: Equipment.ChangeStatus — все допустимые и недопустимые переходы
- Unit: soft delete не удаляет физически
- Integration: CRUD clients end-to-end
- Integration: unique serial number constraint
- Integration: cascade: нельзя удалить клиента с активными объектами

### Критерий готовности CP3
- [ ] Менеджер может: создать клиента → добавить договор → создать объект → добавить оборудование
- [ ] Навигация: клиент → его объекты → оборудование объекта
- [ ] Поиск и фильтрация работают на всех таблицах
- [ ] Soft delete работает (удалённые не показываются, но есть в БД)
- [ ] SLA иерархия: contract → object override → работает в коде
- [ ] Serial number уникален в рамках объекта
- [ ] Equipment status transitions валидируются

### Типичные ошибки CP3
- Не сделать soft delete → потеря данных, нарушение связей
- Хранить equipment type как free text → дубликаты, опечатки. Использовать справочник
- Не валидировать cascade delete → удалён клиент, а заявки висят
- Забыть SLA hierarchy → потом жёсткий рефакторинг в CP4

---

## Checkpoint 4 — Service Desk (основной MVP модуль)

### Цель
Реализовать главную бизнес-ценность: полный цикл работы с заявками.

### Контекст
Это ядро системы. Здесь больше всего бизнес-логики, edge cases и domain events. Заложить правильно = система работает. Заложить неправильно = бесконечный рефакторинг.

### 4.1 Domain — Ticket

```csharp
public class Ticket : BaseEntity
{
    public string Number { get; private set; }          // auto-generated: "TK-2026-00001"
    public Guid ServiceObjectId { get; set; }
    public Guid? EquipmentId { get; set; }              // nullable: может быть не привязана к оборудованию
    public Guid? AssignedEngineerId { get; set; }

    public TicketType Type { get; set; }
    public TicketPriority Priority { get; set; }
    public TicketStatus Status { get; private set; } = TicketStatus.New;

    public string Title { get; set; }
    public string Description { get; set; }

    // SLA
    public DateTime? SlaResponseDeadline { get; set; }
    public DateTime? SlaResolutionDeadline { get; set; }
    public DateTime? FirstResponseAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public bool IsSlaResponseBreached { get; set; }
    public bool IsSlaResolutionBreached { get; set; }

    // Navigation
    public ServiceObject ServiceObject { get; set; }
    public Equipment? Equipment { get; set; }
    public List<TicketComment> Comments { get; set; } = new();
    public List<TicketAttachment> Attachments { get; set; } = new();
    public List<TicketStatusHistory> StatusHistory { get; set; } = new();
}
```

### 4.2 State Machine (Stateless library)

```csharp
// Domain/StateMachines/TicketStateMachine.cs
public class TicketStateMachine
{
    private readonly StateMachine<TicketStatus, TicketTrigger> _machine;

    public enum TicketTrigger { Assign, StartWork, WaitForParts, Complete, Close, Cancel, Reopen }

    public TicketStateMachine(TicketStatus initialState)
    {
        _machine = new StateMachine<TicketStatus, TicketTrigger>(initialState);

        _machine.Configure(TicketStatus.New)
            .Permit(TicketTrigger.Assign, TicketStatus.Assigned)
            .Permit(TicketTrigger.Cancel, TicketStatus.Cancelled);

        _machine.Configure(TicketStatus.Assigned)
            .Permit(TicketTrigger.StartWork, TicketStatus.InProgress)
            .Permit(TicketTrigger.Cancel, TicketStatus.Cancelled);

        _machine.Configure(TicketStatus.InProgress)
            .Permit(TicketTrigger.WaitForParts, TicketStatus.WaitingParts)
            .Permit(TicketTrigger.Complete, TicketStatus.Done)
            .Permit(TicketTrigger.Cancel, TicketStatus.Cancelled);

        _machine.Configure(TicketStatus.WaitingParts)
            .Permit(TicketTrigger.StartWork, TicketStatus.InProgress)
            .Permit(TicketTrigger.Cancel, TicketStatus.Cancelled);

        _machine.Configure(TicketStatus.Done)
            .Permit(TicketTrigger.Close, TicketStatus.Closed)
            .Permit(TicketTrigger.Reopen, TicketStatus.InProgress);

        _machine.Configure(TicketStatus.Closed)
            .Permit(TicketTrigger.Reopen, TicketStatus.InProgress);

        // Cancelled — terminal state, no transitions
    }

    public bool CanFire(TicketTrigger trigger) => _machine.CanFire(trigger);
    public void Fire(TicketTrigger trigger) => _machine.Fire(trigger);
    public IEnumerable<TicketTrigger> GetPermittedTriggers() => _machine.PermittedTriggers;
}
```

### 4.3 SLA Engine (Application layer)

```csharp
// Application/ServiceDesk/Services/SlaCalculator.cs
public class SlaCalculator
{
    public SlaDeadlines Calculate(TicketPriority priority, SlaPolicy policy, DateTime createdAt)
    {
        // 1. Определить ResponseTime и ResolutionTime по priority
        //    Critical: response = policy.ResponseTime / 4, resolution = policy.ResolutionTime / 4
        //    High: response = policy.ResponseTime / 2, resolution = policy.ResolutionTime / 2
        //    Normal: response = policy.ResponseTime, resolution = policy.ResolutionTime
        //    Low: response = policy.ResponseTime * 2, resolution = policy.ResolutionTime * 2
        //
        // 2. Если BusinessHoursOnly:
        //    считать только 9:00-18:00 пн-пт
        //    использовать helper для подсчёта business minutes
        //
        // 3. Вернуть SlaDeadlines { ResponseDeadline, ResolutionDeadline }
    }

    public SlaDeadlines Recalculate(SlaDeadlines current, TicketStatus newStatus, DateTime timestamp)
    {
        // Если newStatus входит в PausedOnStatuses — зафиксировать оставшееся время
        // Если выходит из паузы — пересчитать deadline от текущего момента
    }
}
```

### 4.4 Domain Events

```csharp
// Domain/Events/
public record TicketCreatedEvent(Guid TicketId, TicketPriority Priority) : IDomainEvent { public DateTime OccurredAt => DateTime.UtcNow; }
public record TicketAssignedEvent(Guid TicketId, Guid EngineerId) : IDomainEvent { ... }
public record TicketStatusChangedEvent(Guid TicketId, TicketStatus OldStatus, TicketStatus NewStatus) : IDomainEvent { ... }
public record TicketSlaBreachedEvent(Guid TicketId, string SlaType) : IDomainEvent { ... }

// Application/ServiceDesk/EventHandlers/
// TicketAssignedEventHandler → отправить notification инженеру
// TicketSlaBreachedEventHandler → отправить notification менеджеру и руководителю
// TicketStatusChangedEventHandler → записать в TicketStatusHistory
```

### 4.5 Escalation (Background Job)

```csharp
// Infrastructure/Jobs/SlaCheckJob.cs
// Hangfire Recurring Job: каждые 5 минут
// 1. Найти все открытые tickets
// 2. Проверить SLA deadlines
// 3. Если 80% времени прошло и ещё нет уведомления → TicketSlaWarningEvent → notify manager
// 4. Если deadline пройден → TicketSlaBreachedEvent → notify manager + admin
```

### 4.6 File Attachments

```csharp
// Стратегия хранения:
// Dev: /app/uploads/{ticketId}/{filename}
// Production: MinIO bucket "ticket-attachments"
// Абстракция IFileStorageService уже определена в CP1

// TicketAttachment entity:
public class TicketAttachment : BaseEntity
{
    public Guid TicketId { get; set; }
    public string FileName { get; set; }
    public string StoragePath { get; set; }
    public long FileSize { get; set; }
    public string ContentType { get; set; }
}

// Ограничения: max 10MB на файл, max 10 файлов на заявку
// Допустимые типы: jpg, png, pdf, doc, docx, xls, xlsx
```

### 4.7 API endpoints

```
POST   /api/v1/tickets                           → CreateTicketCommand
GET    /api/v1/tickets                           → PaginatedResult (filters: status, priority, engineer, object, date range, overdue)
GET    /api/v1/tickets/{id}                      → TicketDetailDto
PUT    /api/v1/tickets/{id}                      → UpdateTicketCommand
PATCH  /api/v1/tickets/{id}/assign               → AssignTicketCommand { engineerId }
PATCH  /api/v1/tickets/{id}/status               → ChangeTicketStatusCommand { trigger }
GET    /api/v1/tickets/{id}/transitions          → GetPermittedTransitions (для UI)
POST   /api/v1/tickets/{id}/comments             → AddCommentCommand
GET    /api/v1/tickets/{id}/comments             → List<TicketCommentDto>
POST   /api/v1/tickets/{id}/attachments          → UploadAttachmentCommand (multipart)
GET    /api/v1/tickets/{id}/attachments           → List<TicketAttachmentDto>
GET    /api/v1/tickets/{id}/history              → List<TicketStatusHistoryDto>

GET    /api/v1/engineers/workload                → List<EngineerWorkloadDto> (active tickets per engineer)
```

### 4.8 Numbering System

```csharp
// Application/Common/Services/DocumentNumberGenerator.cs
// Формат: {PREFIX}-{YEAR}-{SEQUENTIAL_NUMBER:D5}
// Tickets: "TK-2026-00001"
// WorkActs: "WA-2026-00001"
// Invoices: "INV-2026-00001"
//
// Реализация: таблица DocumentCounters { Prefix, Year, LastNumber }
// UPDATE с optimistic concurrency (ConcurrencyToken) для thread safety
```

### 4.9 Frontend

- **Список заявок** — Ant Design Table с фильтрами, подсветка overdue (красный), warning (жёлтый)
- **Kanban board** — столбцы по статусам, drag-and-drop через `@hello-pangea/dnd` (форк react-beautiful-dnd)
- **Карточка заявки** — вся информация, комментарии (чат-стиль), вложения, история статусов (timeline)
- **Форма создания заявки** — выбор клиента → объекта → оборудования (cascading selects)
- **Панель нагрузки инженеров** — таблица: инженер, активных заявок, просроченных, загрузка %

### 4.10 Optimistic Concurrency (реализовать ЗДЕСЬ, не в CP9)

```csharp
// На Ticket entity:
public uint RowVersion { get; set; }

// EF Core:
builder.Entity<Ticket>().Property(t => t.RowVersion).IsRowVersion();

// При update: если RowVersion не совпадает → DbUpdateConcurrencyException → вернуть 409 Conflict
```

### 4.11 Тесты CP4
- Unit: TicketStateMachine — все допустимые переходы
- Unit: TicketStateMachine — недопустимые переходы бросают exception
- Unit: SlaCalculator — business hours, pauses
- Unit: SlaCalculator — priority multipliers
- Integration: полный flow: create → assign → start → complete → close
- Integration: concurrency conflict → 409
- Integration: SLA breach detection
- Integration: file upload/download

### Критерий готовности CP4
- [ ] Полный бизнес-процесс: создание → назначение → выполнение → закрытие
- [ ] SLA рассчитывается при создании, пересчитывается при паузе/возобновлении
- [ ] Уведомления: инженеру при назначении, менеджеру при SLA warning/breach
- [ ] Kanban board с drag-and-drop
- [ ] Фильтрация: по статусу, приоритету, инженеру, объекту, дате, overdue
- [ ] Вложения загружаются и скачиваются
- [ ] История статусов отображается как timeline
- [ ] Optimistic concurrency работает
- [ ] Numbering: TK-2026-XXXXX автоинкремент

### Типичные ошибки CP4
- Status transition через `ticket.Status = newStatus` без валидации → сломанные workflow
- SLA без учёта business hours → неправильные дедлайны
- SLA без паузы на WaitingParts → несправедливые breach
- Без optimistic concurrency → два менеджера перезатирают данные
- Kanban без debounce → N запросов за секунду при перетаскивании
- Attachments без валидации размера/типа → security risk

---

## Checkpoint 5 — Плановое обслуживание (ТО)

### Цель
Автоматизировать регламентные работы: система сама создаёт заявки по расписанию.

### Контекст
Зависит от CP3 (оборудование) и CP4 (заявки). Использует Hangfire recurring jobs.

### 5.1 Domain

```csharp
public class MaintenancePlan : BaseEntity
{
    public Guid ServiceObjectId { get; set; }
    public Guid? EquipmentId { get; set; }              // nullable: может быть ТО всего объекта
    public string Title { get; set; }                    // "Ежемесячное ТО кондиционеров"
    public string? Description { get; set; }

    // Расписание (cron expression для простоты и читаемости)
    public string CronExpression { get; set; }           // "0 9 1 * *" = 1-го числа каждого месяца в 9:00
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Последняя генерация
    public DateTime? LastGeneratedDate { get; set; }
    public Guid? LastGeneratedTicketId { get; set; }

    // Чеклист (JSON в MVP, отдельная таблица — если понадобится конструктор)
    public string? ChecklistTemplateJson { get; set; }
    // Формат: [{"item": "Проверить давление", "required": true}, ...]

    // Назначение по умолчанию
    public Guid? DefaultEngineerId { get; set; }
    public TicketPriority DefaultPriority { get; set; } = TicketPriority.Normal;
}
```

### 5.2 Auto-generation Job

```csharp
// Infrastructure/Jobs/MaintenanceGenerationJob.cs
// Hangfire Recurring: каждый час проверять все активные MaintenancePlans
//
// Логика:
// 1. Для каждого активного плана:
//    a. Вычислить следующую дату по CronExpression (библиотека Cronos)
//    b. Если следующая дата <= сейчас И LastGeneratedDate < следующая дата:
//       - Проверить: нет ли НЕзакрытой заявки от этого плана
//         (если есть → пропустить, создать Notification менеджеру "ТО не выполнено в срок")
//       - Создать Ticket (type=Scheduled, title из плана, priority из плана)
//       - Обновить LastGeneratedDate и LastGeneratedTicketId
//       - Если DefaultEngineerId указан → автоназначить
```

### 5.3 Checklist Execution

```csharp
// При выполнении заявки типа Scheduled инженер заполняет чеклист:
// Domain/ValueObjects/ChecklistItem.cs
public record ChecklistItem(string Title, bool IsRequired, bool IsCompleted, string? Note);

// В Ticket: public string? ChecklistResultJson { get; set; }
// Валидация при закрытии: все required items должны быть completed
```

### 5.4 API endpoints

```
GET    /api/v1/maintenance-plans                       → PaginatedResult
POST   /api/v1/maintenance-plans                       → CreateMaintenancePlanCommand
PUT    /api/v1/maintenance-plans/{id}                  → UpdateMaintenancePlanCommand
DELETE /api/v1/maintenance-plans/{id}                  → soft delete
PATCH  /api/v1/maintenance-plans/{id}/toggle           → activate/deactivate

GET    /api/v1/maintenance-plans/calendar?month=&year= → List<CalendarItemDto>
GET    /api/v1/maintenance-plans/upcoming?days=30      → List<UpcomingMaintenanceDto>
```

### 5.5 Frontend
- Список планов ТО с CRUD
- Календарь ТО (Ant Design Calendar или simple grid) — месячный вид
- Upcoming tasks: список ближайших ТО
- Checklist form: при выполнении заявки типа Scheduled — чеклист с галочками

### 5.6 Тесты CP5
- Unit: cron expression parsing + next occurrence calculation
- Unit: конфликт — пропуск при открытой заявке
- Integration: job создаёт ticket корректно
- Integration: checklist validation при закрытии

### Критерий готовности CP5
- [ ] Hangfire job работает, создаёт заявки по расписанию
- [ ] Менеджеру не нужно создавать плановые заявки вручную
- [ ] Если предыдущее ТО не закрыто → новая заявка НЕ создаётся, менеджер уведомлён
- [ ] Календарь показывает план на месяц
- [ ] Чеклист заполняется инженером при выполнении

### Типичные ошибки CP5
- Cron без timezone → заявки создаются в UTC, а не в локальном времени
- Нет проверки на дубликат → 100 заявок за день от одного плана
- Чеклист как free text → нет валидации, бухгалтерия не примет акт

---

## Checkpoint 6 — Отчёты и аналитика

### Цель
Дать руководителю управленческую ценность: видеть проблемные объекты, загрузку команды, качество сервиса.

### Контекст
Зависит от CP3 + CP4. Данные уже есть в БД, нужно агрегировать и визуализировать.

### 6.1 Метрики (определения)

| Метрика | Формула | Источник |
|---|---|---|
| MTTR (Mean Time To Repair) | avg(ResolvedAt - CreatedAt) по закрытым заявкам за период | Ticket |
| SLA Response Rate | count(FirstResponseAt <= SlaResponseDeadline) / count(all) * 100% | Ticket |
| SLA Resolution Rate | count(ResolvedAt <= SlaResolutionDeadline) / count(all) * 100% | Ticket |
| Engineer Workload | count(active tickets) per engineer | Ticket |
| ТО Completion Rate | count(Scheduled tickets closed in time) / count(Scheduled tickets) * 100% | Ticket (type=Scheduled) |
| Top Problematic Equipment | count(tickets) per equipment, sorted desc | Ticket + Equipment |
| Tickets by Object | count(tickets) per ServiceObject | Ticket + ServiceObject |
| Avg Resolution by Priority | avg(ResolvedAt - CreatedAt) grouped by Priority | Ticket |

### 6.2 Реализация

**Подход: CQRS read-side queries напрямую к БД (EF Core LINQ → SQL).** Без materialized views в MVP. Если тормозит (>500ms) — добавить индексы, потом кэш.

```csharp
// Application/Reporting/Queries/
GetDashboardQuery → DashboardDto {
    TotalOpenTickets, OverdueTickets, AvgResolutionTimeHours,
    SlaResponseRate, SlaResolutionRate,
    TicketsByStatusChart, TicketsByPriorityChart,
    TopProblematicEquipment (top 10),
    EngineerWorkload
}

GetReportQuery → ReportDto {
    DateRange, Filters (client, object, engineer),
    DetailedMetrics, TicketsList
}
```

### 6.3 Export

```csharp
// Excel: ClosedXML
// PDF: QuestPDF

// API:
GET /api/v1/reports/dashboard                → DashboardDto (JSON для charts)
GET /api/v1/reports/tickets?format=xlsx      → файл Excel
GET /api/v1/reports/tickets?format=pdf       → файл PDF
GET /api/v1/reports/sla?from=&to=            → SLA report
GET /api/v1/reports/engineers?from=&to=      → Engineer workload report
```

### 6.4 Frontend
- Dashboard page: карточки с KPI (Ant Design Statistic), графики (Recharts)
- Фильтры по периоду, клиенту, объекту, инженеру
- Export кнопки: Excel, PDF

### 6.5 Индексы (добавить в миграцию)

```sql
-- Для быстрых агрегаций
CREATE INDEX IX_Tickets_Status_CreatedAt ON "Tickets" ("Status", "CreatedAt") WHERE "IsDeleted" = false;
CREATE INDEX IX_Tickets_AssignedEngineerId ON "Tickets" ("AssignedEngineerId") WHERE "IsDeleted" = false;
CREATE INDEX IX_Tickets_ServiceObjectId ON "Tickets" ("ServiceObjectId") WHERE "IsDeleted" = false;
CREATE INDEX IX_Tickets_EquipmentId ON "Tickets" ("EquipmentId") WHERE "IsDeleted" = false;
```

### Критерий готовности CP6
- [ ] Dashboard загружается < 2 секунд
- [ ] Все метрики из таблицы рассчитываются корректно
- [ ] Excel export генерируется и открывается без ошибок
- [ ] PDF export генерируется
- [ ] Фильтры по дате, клиенту, инженеру работают

### Типичные ошибки CP6
- Считать SLA rate без фильтра по статусу → включаются открытые заявки
- MTTR в минутах вместо часов → нечитаемо для руководителя
- N+1 queries в агрегациях → медленно
- Нет индексов → full table scan на каждый запрос

---

## Checkpoint 7 — Бухгалтерия и документы

### Цель
Завершить бизнес-процесс: после закрытия заявки бухгалтер формирует акт и счёт.

### Контекст
Зависит от CP4. Требует нумерации документов (заложена в CP4).

### 7.1 Domain

```csharp
public class WorkAct : BaseEntity
{
    public string Number { get; set; }          // "WA-2026-00001"
    public Guid TicketId { get; set; }
    public DateTime ActDate { get; set; }
    public string Description { get; set; }     // описание выполненных работ
    public decimal LaborCost { get; set; }
    public decimal MaterialsCost { get; set; }
    public decimal TotalCost => LaborCost + MaterialsCost;
    public bool IsSignedByClient { get; set; }
    public DateTime? SignedAt { get; set; }

    // Navigation
    public Ticket Ticket { get; set; }
    public List<Invoice> Invoices { get; set; } = new();
}

public class Invoice : BaseEntity
{
    public string Number { get; set; }          // "INV-2026-00001"
    public Guid WorkActId { get; set; }
    public Guid ClientId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public decimal PaidAmount { get; set; }
    public DateTime? PaidAt { get; set; }

    // Navigation
    public WorkAct WorkAct { get; set; }
    public Client Client { get; set; }
}
```

### 7.2 Aging (возрастная структура задолженности)

```csharp
// Application/Finance/Queries/GetDebtAgingQuery.cs
// Результат: per client
// {
//   ClientName, TotalDebt,
//   Current (0-30 дней), Overdue30 (31-60), Overdue60 (61-90), Overdue90 (90+)
// }
```

### 7.3 PDF Generation (QuestPDF)

```csharp
// Infrastructure/Documents/WorkActPdfGenerator.cs
// Infrastructure/Documents/InvoicePdfGenerator.cs
// Шаблоны: реквизиты компании, реквизиты клиента, таблица работ, итого, подписи
```

### 7.4 1C Export

```csharp
// Infrastructure/Export/OneCExportService.cs
// Формат: CommerceML 2.x XML
// Выгрузка: акты и счета за период → XML файл → скачивание
// API: GET /api/v1/export/1c?from=&to= → XML file
```

### 7.5 API endpoints

```
POST   /api/v1/tickets/{ticketId}/work-act        → CreateWorkActCommand
GET    /api/v1/work-acts/{id}                      → WorkActDetailDto
PUT    /api/v1/work-acts/{id}                      → UpdateWorkActCommand
GET    /api/v1/work-acts/{id}/pdf                  → PDF file

POST   /api/v1/work-acts/{workActId}/invoice       → CreateInvoiceCommand
GET    /api/v1/invoices                            → PaginatedResult (filters: status, client, date)
PUT    /api/v1/invoices/{id}                       → UpdateInvoiceCommand
PATCH  /api/v1/invoices/{id}/payment               → RecordPaymentCommand
GET    /api/v1/invoices/{id}/pdf                   → PDF file

GET    /api/v1/finance/debt-aging                  → List<DebtAgingDto>
GET    /api/v1/finance/export/csv                  → CSV file
GET    /api/v1/finance/export/xlsx                 → XLSX file
GET    /api/v1/finance/export/1c                   → XML file (CommerceML)
```

### 7.6 Frontend
- Вкладка «Акт» в карточке заявки (после статуса Done/Closed)
- Список актов, генерация PDF
- Список счетов с фильтрами по статусу оплаты
- Карточка клиента: вкладка «Задолженности» с aging таблицей
- Export кнопки: CSV, XLSX, 1С

### Критерий готовности CP7
- [ ] После закрытия заявки бухгалтер формирует акт за 1 клик
- [ ] Из акта формируется счёт за 1 клик
- [ ] PDF генерируется для акта и счёта
- [ ] Нумерация документов: сквозная, по году, без пропусков
- [ ] Aging задолженности: 0-30, 31-60, 61-90, 90+ дней
- [ ] Экспорт в 1С (CommerceML XML) работает

### Типичные ошибки CP7
- Нумерация без блокировки → дубликаты при параллельных запросах
- Счёт без привязки к акту → бухгалтерский ад
- 1С export без тестирования импорта на стороне 1С → формат не принимается
- PDF без правильной кодировки → кракозябры в кириллице (QuestPDF решает это из коробки)

---

## Checkpoint 8 — Карта и логистика инженеров

### Цель
Визуализация объектов на карте, оптимизация назначения инженеров.

### Контекст
Post-MVP модуль. Требует CP3 (объекты с координатами) и CP4 (заявки). Добавляет пространственные запросы.

### 8.1 Backend — PostGIS

```sql
-- Миграция: добавить расширение и geography column
CREATE EXTENSION IF NOT EXISTS postgis;
ALTER TABLE "ServiceObjects" ADD COLUMN "Location" geography(POINT, 4326);
UPDATE "ServiceObjects" SET "Location" = ST_MakePoint("Longitude", "Latitude") WHERE "Latitude" IS NOT NULL;
CREATE INDEX IX_ServiceObjects_Location ON "ServiceObjects" USING GIST ("Location");
```

```csharp
// EF Core: Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite
// ServiceObject: public Point? Location { get; set; } // NetTopologySuite.Geometries.Point
```

### 8.2 Nearest Engineer (упрощённая версия для MVP)

```csharp
// Вместо real-time GPS — последний известный объект инженера
// (объект последней закрытой заявки)
//
// Application/Geo/Queries/GetNearestEngineersQuery.cs
// Input: targetObjectId
// 1. Получить координаты целевого объекта
// 2. Для каждого свободного инженера — координаты его последнего объекта
// 3. ORDER BY ST_Distance(engineer_last_location, target_location)
// 4. Вернуть top 5 с расстоянием
```

### 8.3 Routing

```
// Frontend: Leaflet Routing Machine + OSRM (open source routing)
// Показать маршрут от инженера до объекта
// OSRM public demo server для dev, self-hosted для production
```

### 8.4 API endpoints

```
GET /api/v1/geo/objects                     → List<MapObjectDto> { id, name, lat, lng, activeTickets }
GET /api/v1/geo/objects/emergency           → только объекты с Critical заявками
GET /api/v1/geo/objects/cluster?zoom=       → кластеризованные точки для zoom level
GET /api/v1/geo/nearest-engineers?objectId= → List<NearestEngineerDto> { name, distance, activeTickets }
```

### 8.5 Frontend
- Карта с объектами (Leaflet + MarkerCluster для кластеризации)
- Цветовая кодировка: красный = critical ticket, жёлтый = overdue, зелёный = нет проблем
- Popup при клике: название объекта, клиент, активные заявки
- Панель: nearest engineers для выбранного объекта
- Маршрут до объекта (Leaflet Routing Machine)

### Критерий готовности CP8
- [ ] Карта отображает все объекты с координатами
- [ ] Кластеризация при zoom out
- [ ] Аварийные заявки выделены на карте
- [ ] Nearest engineer suggestion работает
- [ ] Менеджер может визуально оценить логистику

---

## Checkpoint 9 — Production Hardening

### Цель
Довести систему до production-ready. Исправить то, что не было заложено ранее.

### Контекст
К этому моменту большинство hardening уже сделано в CP1–CP8 (pagination, concurrency, logging, auth). Здесь — финальный чеклист и то, что осталось.

### 9.1 Security Checklist

- [ ] HTTPS only (HSTS header)
- [ ] CORS: strict policy (только домен frontend)
- [ ] Rate limiting: `AspNetCoreRateLimit` (100 req/min per IP для auth endpoints, 1000 для остальных)
- [ ] Input validation: FluentValidation на каждый command/query (уже сделано через pipeline behavior)
- [ ] SQL injection: EF Core параметризованные запросы (по умолчанию)
- [ ] XSS: React экранирует по умолчанию, не использовать `dangerouslySetInnerHTML`
- [ ] File upload: валидация MIME type (не только расширение), антивирус (ClamAV для production)
- [ ] Secrets: environment variables или Azure Key Vault / HashiCorp Vault
- [ ] Brute force: Identity lockout уже настроен в CP2
- [ ] Dependency audit: `dotnet list package --vulnerable`

### 9.2 Performance

- [ ] Database indexes: проверить все запросы через `EXPLAIN ANALYZE`
- [ ] N+1: включить EF Core query warning в dev
- [ ] Caching: Response caching для справочников (5 мин TTL), Redis для sessions если нужно
- [ ] Compression: gzip/brotli на API и frontend static

### 9.3 Reliability

- [ ] Retries: Polly для email отправки и внешних HTTP вызовов
- [ ] Idempotency: для финансовых операций (создание счёта) — idempotency key в header
- [ ] Backup: PostgreSQL pg_dump cron job (ежедневно, хранить 30 дней)
- [ ] Monitoring: health check endpoint + Prometheus metrics (или simple uptime check)

### 9.4 Tests (финальный проход)

- [ ] Unit tests: coverage > 70% для Domain и Application
- [ ] Integration tests: каждый API endpoint
- [ ] E2E: Playwright — 3 critical flows (login → create ticket → close → create work act)
- [ ] Load test: k6 — 50 concurrent users, p95 < 500ms

### Критерий готовности CP9
- [ ] Все пункты security checklist пройдены
- [ ] Нет Critical/High vulnerabilities в dependency audit
- [ ] Load test пройден
- [ ] E2E тесты зелёные
- [ ] Backup/restore протестирован

---

## Checkpoint 10 — Deploy и Release

### Цель
Развернуть систему в production.

### 10.1 Build

```bash
# Backend
dotnet publish src/ServiceCompany.Api -c Release -o ./publish

# Frontend
cd src/ServiceCompany.Frontend
npm run build    # output: dist/
```

### 10.2 Docker Production Images

```dockerfile
# Backend: multi-stage build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
COPY . .
RUN dotnet publish src/ServiceCompany.Api -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ServiceCompany.Api.dll"]

# Frontend: Nginx serve static
FROM node:20 AS build
COPY src/ServiceCompany.Frontend .
RUN npm ci && npm run build

FROM nginx:alpine
COPY --from=build /dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
```

### 10.3 Production Stack

```yaml
# docker-compose.production.yml
services:
  nginx:          # reverse proxy, SSL termination
  api:            # ASP.NET Core (2 replicas recommended)
  postgres:       # PostgreSQL 16 with volume
  hangfire:       # same API image, different entrypoint or same process
  seq:            # log aggregation (or use cloud logging)
```

### 10.4 Migration Strategy

```bash
# Миграции применяются при deploy, ДО старта нового API:
dotnet ef database update --project src/ServiceCompany.Infrastructure --startup-project src/ServiceCompany.Api

# Rollback: каждая миграция должна иметь Down() method
# Тестировать rollback в staging
```

### 10.5 Seed Data (при первом deploy)

```csharp
// Infrastructure/Persistence/SeedData.cs
// 1. Admin user (credentials из environment variables)
// 2. Default roles: Admin, Manager, Engineer, Accountant
// 3. Справочники: equipment types, default SLA policy
// 4. System settings: company name, address, logo path
```

### 10.6 Data Migration (из существующих систем)

```csharp
// Infrastructure/Migration/DataImportService.cs
// CSV/Excel import для:
// 1. Clients (Name, INN, Address, Phone)
// 2. ServiceObjects (ClientName → lookup, Address)
// 3. Equipment (ObjectAddress → lookup, Name, SerialNumber)
//
// API:
// POST /api/v1/admin/import/clients    → upload CSV
// POST /api/v1/admin/import/objects    → upload CSV
// POST /api/v1/admin/import/equipment  → upload CSV
//
// Валидация: dry run mode → показать ошибки → confirm → import
```

### 10.7 Monitoring & Alerting

- Health check: `/health` → uptime monitor (UptimeRobot или аналог)
- Logs: Seq dashboard с alerts (5xx errors > 10/min → email)
- Database: pg_stat_statements для slow queries
- Disk space: alert при 80% заполнения

### 10.8 Deploy Guide (checklist)

```
1. Подготовка
   □ Staging environment развёрнут
   □ Production environment подготовлен (VPS/server)
   □ DNS настроен
   □ SSL сертификат (Let's Encrypt)
   □ Environment variables заполнены

2. Первый deploy
   □ docker-compose up -d postgres
   □ Применить миграции
   □ Seed data
   □ Import данных (если есть)
   □ docker-compose up -d api frontend nginx
   □ Smoke test: login → create client → create ticket
   □ Backup настроен и протестирован

3. Последующие deploy
   □ Backup БД перед обновлением
   □ docker-compose pull
   □ Применить миграции
   □ docker-compose up -d --no-deps api
   □ Smoke test
   □ Если ошибка → rollback migration → restart previous image

4. Rollback plan
   □ Previous Docker images tagged
   □ Migration Down() протестирован
   □ Restore from backup протестирован
```

### Критерий готовности CP10
- [ ] Production deploy выполнен
- [ ] SSL работает
- [ ] Smoke test пройден в production
- [ ] Backup настроен и протестирован
- [ ] Monitoring настроен
- [ ] Admin user создан
- [ ] Data migration выполнена (если нужна)
- [ ] Deploy guide написан
- [ ] Release tag создан в git

---

## Спринт-план (реалистичный)

Для команды 2–3 разработчика (1 backend, 1 frontend, 1 fullstack), спринт = 2 недели.

| Спринт | Чекпоинт | Длительность | Deliverable |
|---|---|---|---|
| 1 | CP0 Discovery | 2 недели | Документация, ERD, API contract |
| 2 | CP1 Skeleton | 2 недели | Работающий каркас, Docker, тесты |
| 3 | CP2 Auth | 2 недели | Login, roles, audit, notifications base |
| 4 | CP3 Master Data | 3 недели | Клиенты, объекты, оборудование |
| 5 | CP4 Service Desk (core) | 3 недели | Заявки, state machine, SLA |
| 6 | CP4 Service Desk (UI + polish) | 2 недели | Kanban, filters, attachments |
| 7 | CP5 ТО | 2 недели | Планы, auto-generation, calendar |
| 8 | CP6 Reports | 2 недели | Dashboard, metrics, export |
| 9 | CP7 Finance | 2 недели | Акты, счета, PDF, 1С |
| 10 | CP8 Map | 2 недели | Карта, nearest engineer |
| 11 | CP9 Hardening | 2 недели | Security, tests, load test |
| 12 | CP10 Deploy | 1 неделя | Production release |

**Итого: ~25 недель (~6 месяцев)**

### Порядок для MVP (если нужно выпустить быстрее)

Минимальный жизнеспособный продукт = CP0 + CP1 + CP2 + CP3 + CP4. Это ~12 недель. Система уже полезна: можно вести клиентов и управлять заявками.

Затем итеративно: CP5 → CP6 → CP7 → CP8 → CP9 → CP10.
