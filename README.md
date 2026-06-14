# Microservices Pet Project

Учебный интернет-магазин на .NET с микросервисной архитектурой. В MVP войдут
каталог, корзина, оформление заказов и асинхронные уведомления.

Подробный roadmap находится в
[docs/implementation-plan.md](docs/implementation-plan.md).

## Текущее состояние

Создан базовый каркас:

- Aspire AppHost;
- Gateway;
- Catalog API;
- Cart API;
- Orders API;
- Notifications Worker;
- общие ServiceDefaults с OpenTelemetry, service discovery, resilience и
  health checks.

Бизнес-логика пока не подключена. AppHost уже описывает PostgreSQL с базами
`catalog` и `orders`, Redis и RabbitMQ с management UI.

## Требования

- .NET SDK `10.0.103` или совместимый patch-релиз;
- Aspire CLI `13.4.3`;
- Docker Desktop или другой Docker-compatible runtime версии `28.0.0` или
  новее.

Установка Aspire CLI:

```powershell
dotnet tool install --global Aspire.Cli
```

Проверка окружения:

```powershell
aspire doctor
```

## Сборка

```powershell
dotnet restore
dotnet build
dotnet test
```

## Запуск

```powershell
aspire run --apphost src/AppHost/AppHost.csproj
```

После запуска URL Aspire Dashboard выводится в терминал. Gateway и backend API
доступны через ссылки ресурсов в Dashboard.

## Проверка сервисов

Каждый API предоставляет:

- `GET /` — smoke endpoint;
- `GET /health` — readiness;
- `GET /alive` — liveness;
- `GET /openapi/v1.json` — OpenAPI в Development.

Интеграционные smoke-тесты Catalog, Cart и Orders проверяют эти endpoints через
`WebApplicationFactory` без необходимости запуска контейнеров.

## Локальные секреты

Пароли и connection strings не должны попадать в `appsettings*.json`. Для
локальных секретов используются Aspire parameters, user secrets или переменные
окружения. Локальные `appsettings.Local.json` и `.env` игнорируются Git.

## Известная проблема окружения

На текущей машине установлен Docker client `26.1.4`, тогда как Aspire `13.4.3`
требует минимум `28.0.0`. Доступ к Docker daemon также закрыт. Smoke-запуск
AppHost из автоматизированной сессии завершается ошибкой TLS-соединения с
локальным DCP. Solution при этом восстанавливается и собирается без
предупреждений и ошибок.
