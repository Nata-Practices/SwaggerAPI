# SwaggerAPI

## Описание
`SwaggerAPI` — это веб-приложение, построенное с использованием ASP.NET Core. Оно включает в себя функционал для управления событиями (Events), билетами (Tickets) и объектами (Objects). Реализация основана на использовании MongoDB для хранения данных и Redis для кэширования. Так-же присутсвует Kafka для выполнения запросов с подтверждением в другой микросервис.

Приложение также предоставляет удобный Swagger UI для взаимодействия с API.

Советую к посещению: [2-ой микросервис](https://github.com/Nata-Practices/UserService)
---

## Структура проекта

```plaintext
SwaggerAPI/
├── Dependencies/                      # Зависимости проекта, подключенные через NuGet
├── Properties/                        # Конфигурационные файлы и параметры запуска
│   ├── appsettings.json               # Основные настройки приложения (MongoDB, Redis, Kafka, логирование)
│   ├── appsettings.Development.json   # Настройки для режима разработки, аналогичны верхним
│   └── launchSettings.json            # Настройки запуска приложения
├── Controllers/                       # Контроллеры API
│   ├── EventsController.cs            # Обработчик запросов для работы с событиями
│   ├── ObjectsController.cs           # Обработчик запросов для работы с объектами
│   └── TicketsController.cs           # Обработчик запросов для работы с билетами
├── Models/                            # Модели данных приложения
│   ├── ApiResponse.cs                 # Модель данных для ответа от API
│   ├── EventModel.cs                  # Модель данных для событий
│   ├── ObjectModel.cs                 # Модель данных для объектов
│   ├── MongoDBSettings.cs             # Модель для конфигурации подключения к MongoDB
│   └── TicketModel.cs                 # Модель данных для билетов
├── Services/                          # Сервисы и обработка данных
│   ├── EventService.cs                # Сервис для работы с событиями
│   ├── ObjectService.cs               # Сервис для работы с объектами
│   └── TicketService.cs               # Сервис для работы с билетами
├── Tests/                             # Тесты для проверки функциональности приложения
│   ├── KafkaProducer.cs               # Реализация Kafka Producer для отправки сообщений
│   ├── KafkaConsumer.cs               # Реализация Kafka Consumer для обработки сообщений
│   └── ApiTests.cs                    # Тесты API для контроля корректности работы
├── Utils/                             # Утилиты приложения
│   └── ValidationFilter.cs            # Свой валидатор ошибок при выполнении запросов
└── Program.cs                         # Точка входа в приложение
```

---

## Установка и запуск

### Требования:
- .NET 8.0 (или новее)
- IDE с поддержкой .NET
- MongoDB (локальная или удалённая база данных)
- Redis (локальный или удалённый сервер)
- Kafka (локальный или удалённый сервер)

### Шаги установки:
1. Склонируй репозиторий:
   ```bash
   git clone https://github.com/Nata-Practices/SwaggerAPI.git
   ```
2. Перейди в папку проекта:
   ```bash
   cd SwaggerAPI
   ```
3. Установи зависимости:
   ```bash
   dotnet restore
   ```

### Запуск приложения:
1. Собери проект:
   ```bash
   dotnet build
   ```
2. Запусти сервер:
   ```bash
   dotnet run
   ```
3. Приложение будет доступно по адресу `http://localhost:5294/swagger/index.html`.

### Запуск тестов:
1. Собери проект (если ещё этого не сделала):
   ```bash
   dotnet build
   ```
2. Запусти тесты:
   ```bash
   dotnet test --logger "console;verbosity=detailed"
   ```
   Команда выполнит все тесты, определённые в проекте, и выведет результаты в консоль.

---

## API Роуты

### Events
- `GET /api/events` — получить все события
- `POST /api/events` — создать новое событие
- `GET /api/events/{id}` — получить конкретное событие по id
- `PUT /api/events/{id}` — обновить конкретное событие по id
- `DELETE /api/events/{id}` — удалить конкретное событие по id
- `DELETE /api/events/all` — удалить все события

### Tickets
- `GET /api/tickets` — получить все билеты
- `POST /api/tickets` — создать новый билет
- `GET /api/tickets/{id}` — получить конкретный билет по id
- `PUT /api/tickets/{id}` — обновить конкретный билет по id
- `DELETE /api/tickets/{id}` — удалить конкретный билет по id
- `DELETE /api/tickets/all` — удалить все события

### Objects
- `GET /api/objects` — получить все объекты
- `POST /api/objects` — создать новый объект
- `GET /api/objects/{id}` — получить конкретный объект по id
- `DELETE /api/objects/{id}` — удалить конкретный объект по id

---

## Kafka

- **Producer**:
  - Отправляет запрос юзеру в тему `ObjectRequests`.

- **Consumer**:
  - Принимает сообщения из темы `ObjectConfirmations`:
    - Сообщение содержит `Id` объекта (ключ) и `timestamp` (значение).
    - Обновляет поле `ConfirmationTimestamp` для соответствующего объекта.

---
