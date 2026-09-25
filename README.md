# Analysis API

Тестовое задание: REST API на .NET 10 для разбора HTML-страницы.

Единственный метод `POST /api/analysis` принимает JSON c base64-параметрами и по очереди:

- валидирует входящий объект (FluentValidation);
- декодирует из Base64 URL и код страницы;
- через AngleSharp парсит HTML, выбирает элементы по CSS-селектору и собирает значения заданного атрибута;
- пишет найденные элементы в PostgreSQL через Dapper (таблица `elements`: id, значение атрибута, полный HTML элемента);
- вытаскивает все email-адреса со страницы скомпилированным регулярным выражением (`GeneratedRegex`);
- расшифровывает текст AES-256 ECB с `PaddingMode.None`;
- возвращает всё одним JSON-ответом с отступами: `is_error`, `error_code`, `error_message`, `elements_count`,
  `emails_count`, `url`, `decrypted_plain_text`, `elements_attr_list`, `emails_list`.

Ошибки не меняют HTTP-статус — ответ всегда 200, признак ошибки в `is_error`, текстовый код в `error_code`
(полный список кодов — `src/AnalysisApi/Models.cs`, класс `ErrorCodes`).

## Запуск

```
docker compose up -d
```

Поднимаются три контейнера:

- **api** — http://localhost:8090, Swagger: http://localhost:8090/api/swagger
- **pgadmin** — http://localhost:8080, сервер `analysis-db` уже настроен, пароль вводить не нужно
- **db** — PostgreSQL 18, порт 5432 проброшен на хост — можно запускать приложение локально из IDE (строка подключения в `appsettings.json` уже смотрит на `localhost:5432`)

Исходники монтируются в контейнер api, приложение билдится при каждом запуске (`dotnet run`).
Состояние базы живёт в volume `pgdata` и переживает перезапуск проекта.

## Проверка

1. Откройте http://localhost:8090/api/swagger, метод `POST /api/analysis` → Try it out.
2. Вставьте тело из `json_payload_1.txt` (или `json_payload_2.txt`) и выполните запрос.
3. Ответ должен совпадать с `json_result_1.txt` / `json_result_2.txt` в корне репозитория.
4. Данные смотрите в pgAdmin: Servers → analysis-db → test_task → Schemas → public → таблица `elements`.
   Таблица не чистится между запросами — записи копятся, id автоинкрементный.

## Структура

- `compose.yml` — db + pgadmin + api
- `src/AnalysisApi/AnalysisController.cs` — контроллер
- `src/AnalysisApi/AnalysisService.cs` — сервис с бизнес-логикой
- `src/AnalysisApi/Models.cs` — модели и коды ошибок
- `src/AnalysisApi/AnalysisRequestValidator.cs` — валидация входящего объекта
- `src/AnalysisApi/Program.cs` — DI, настройки JSON, Swagger, создание таблицы

## Заметки по реализации

- Асинхронно всё, что ждёт I/O: валидация, парсинг AngleSharp, запись в Postgres, чтение потока расшифровки.
  Регулярное выражение и AES работают синхронно — это CPU-операции над данными в памяти, асинхронных
  аналогов в .NET у них нет, а `Task.Run` внутри обработки запроса только занимал бы лишний поток из пула.
- AES в режиме ECB показан, потому что этого требует задание; в реальных системах ECB не используют
  (одинаковые блоки открытого текста дают одинаковые блоки шифротекста) — нужен GCM либо CBC + HMAC.
- `PaddingMode.None` не убирает выравнивание блока, поэтому хвостовые нули в расшифрованном тексте срезаются.
