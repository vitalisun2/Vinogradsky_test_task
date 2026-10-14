# TaskService

API управления задачами на .NET 9, ASP.NET Core Minimal API,
PostgreSQL / EF Core и RabbitMQ.

## Требования

- .NET 9 SDK.
- Visual Studio с поддержкой ASP.NET Core и Docker Compose.
- Docker Desktop в режиме Linux-контейнеров.

## Запуск

1. Запустить Docker Desktop.
2. Открыть `TaskService.slnx` в Visual Studio.
3. Выбрать `docker-compose` запускаемым проектом и нажать F5.
4. Дождаться запуска PostgreSQL, RabbitMQ и API.
5. В консоли диспетчера пакетов применить миграции:

```powershell
Update-Database -Project TaskApi -StartupProject TaskApi -Connection "Host=localhost;Port=5432;Database=taskapi;Username=taskapi;Password=taskapi_dev_password"
```

Миграции нужно применять при первом запуске и после изменения схемы.

- API: https://localhost:5081
- Панель RabbitMQ: http://localhost:15672
- Логин RabbitMQ: `taskapi`
- Пароль: `taskapi_dev_password`

Указанные учётные данные предназначены для локального запуска.
Порты 5080, 5081, 5432, 5672 и 15672 должны быть свободны.

## API

| Метод | Путь | Назначение |
|---|---|---|
| POST | `/tasks` | Создать задачу |
| GET | `/tasks` | Получить все задачи |
| PUT | `/tasks/{id}/complete` | Завершить задачу |
| DELETE | `/tasks/{id}` | Удалить задачу |

Пример тела POST:

```json
{
  "title": "Buy milk",
  "priority": 1
}
```

Приоритеты: `0` — Low, `1` — Medium, `2` — High.
По умолчанию используется Medium.

Завершение возвращает `204`, отсутствие задачи — `404`,
повторное или конфликтующее завершение — `409`.

## Тесты

При работающем Docker Desktop выполнить из корня решения:

```powershell
dotnet test TaskApi.Tests/TaskApi.Tests.csproj
```

Тесты сами запускают отдельный PostgreSQL и подменяют издателя событий.
Проверяются создание и завершение задачи, содержимое события,
а также результаты двух одновременных запросов завершения.

## RabbitMQ и упрощения

После сохранения завершения в БД событие публикуется в exchange
`task.events` типа `direct` с routing key `task.completed`.
Приоритет в событии передаётся строкой.

При ошибке публикации событие записывается в лог, задача остаётся
завершённой. Автоматического повторения и Outbox нет.
Publisher confirms не используются, гарантированная доставка не заявляется.

Приложение не создаёт очередь и consumer. Для просмотра сообщений
нужно заранее создать очередь и привязать её к указанному exchange
с routing key `task.completed`.

Публикация ограничена таймаутом 3 секунды. При штатной остановке
издатель ожидает освобождения канала и закрывает ресурсы RabbitMQ;
для StopAsync установлен таймаут 5 секунд.