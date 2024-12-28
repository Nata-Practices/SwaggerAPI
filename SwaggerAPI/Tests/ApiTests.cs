using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SwaggerAPI.Models;
using Xunit;


namespace SwaggerAPI.Tests
{
    public class ApiTests
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _options;

        private const string SwaggerApiBaseUrl = "http://localhost:5294";     // Там, где работает SwaggerAPI
        private const string GatewayBaseUrl = "http://localhost:5285";        // Там, где работает ApiGateway

        public ApiTests()
        {
            Console.OutputEncoding = Encoding.UTF8;
            _client = new HttpClient();
            _options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        private void PrintMessage(string message, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        /// <summary>
        /// Получение JWT-токена с сервиса SwaggerAPI
        /// </summary>
        private async Task<string> GetJwtToken()
        {
            var tokenUrl = $"{SwaggerApiBaseUrl}/api/auth/token";
            var tokenResponse = await _client.PostAsync(tokenUrl, content: null);
            tokenResponse.EnsureSuccessStatusCode();

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<Dictionary<string, string>>(tokenJson, _options);

            Assert.NotNull(tokenData);
            Assert.True(tokenData.ContainsKey("token"), "Ответ не содержит ключ 'token'.");

            return tokenData["token"];
        }

        [Fact]
        public async Task Add100EventsTest()
        {
            var stopwatch = Stopwatch.StartNew();
            PrintMessage("=== Тест: Добавление 100 событий ===", ConsoleColor.Cyan);

            // 1. Получаем токен
            var token = await GetJwtToken();

            // 2. Устанавливаем заголовок Authorization
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // 3. Удаляем все события
            var deleteUrl = $"{SwaggerApiBaseUrl}/api/events/all";
            var deleteResponse = await _client.DeleteAsync(deleteUrl);
            deleteResponse.EnsureSuccessStatusCode();
            PrintMessage("Удалены все события перед началом теста.", ConsoleColor.Yellow);

            // 4. Создаём список событий
            var events = new List<EventModel>();
            for (int i = 0; i < 100; i++)
            {
                events.Add(new EventModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"Event {i}",
                    Description = $"Description {i}",
                    Date = DateTime.UtcNow.AddDays(i),
                    Price = i + 10
                });
            }

            // 5. Отправляем их по одному в SwaggerAPI
            foreach (var evnt in events)
            {
                var content = new StringContent(JsonSerializer.Serialize(evnt), Encoding.UTF8, "application/json");
                var postUrl = $"{SwaggerApiBaseUrl}/api/events";
                var response = await _client.PostAsync(postUrl, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var postApiResponse = JsonSerializer.Deserialize<ApiResponse<EventModel>>(responseContent, _options);

                Assert.NotNull(postApiResponse);
                Assert.True(postApiResponse.Success);
            }

            PrintMessage("Все 100 событий успешно добавлены.", ConsoleColor.Green);

            // 6. Проверяем, что в итоге 100
            var getUrl = $"{SwaggerApiBaseUrl}/api/events";
            var getResponse = await _client.GetAsync(getUrl);
            getResponse.EnsureSuccessStatusCode();

            var responseContentFinal = await getResponse.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<EventModel>>>(responseContentFinal, _options);

            Assert.NotNull(apiResponse);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
            Assert.Equal(100, apiResponse.Data.Count);

            stopwatch.Stop();
            PrintMessage($"Тест успешно завершён. Всего событий: {apiResponse.Data.Count}. Время выполнения: {stopwatch.ElapsedMilliseconds} мс.", ConsoleColor.Magenta);
        }

        [Fact]
        public async Task Add100000EventsTest()
        {
            var stopwatch = Stopwatch.StartNew();
            PrintMessage("=== Тест: Добавление 1000 событий ===", ConsoleColor.Cyan);

            // 1. Получаем токен
            var token = await GetJwtToken();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // 2. Удаляем все события
            var deleteUrl = $"{SwaggerApiBaseUrl}/api/events/all";
            var deleteResponse = await _client.DeleteAsync(deleteUrl);
            deleteResponse.EnsureSuccessStatusCode();
            PrintMessage("Удалены все события перед началом теста.", ConsoleColor.Yellow);

            // 3. Формируем 1000 событий
            var events = new List<EventModel>();
            for (int i = 0; i < 1000; i++)
            {
                events.Add(new EventModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"Event {i}",
                    Description = $"Description {i}",
                    Date = DateTime.UtcNow.AddDays(i),
                    Price = i + 10
                });
            }

            // 4. Отправляем
            var postUrl = $"{SwaggerApiBaseUrl}/api/events";
            foreach (var evnt in events)
            {
                var content = new StringContent(JsonSerializer.Serialize(evnt), Encoding.UTF8, "application/json");
                var response = await _client.PostAsync(postUrl, content);
                response.EnsureSuccessStatusCode();
            }

            PrintMessage("Все 1000 событий успешно добавлены.", ConsoleColor.Green);

            // 5. Проверяем
            var getUrl = $"{SwaggerApiBaseUrl}/api/events";
            var getResponse = await _client.GetAsync(getUrl);
            getResponse.EnsureSuccessStatusCode();

            var responseContent = await getResponse.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<EventModel>>>(responseContent, _options);

            Assert.NotNull(apiResponse);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
            Assert.Equal(1000, apiResponse.Data.Count);

            stopwatch.Stop();
            PrintMessage($"Тест успешно завершён. Всего событий: {apiResponse.Data.Count}. Время выполнения: {stopwatch.ElapsedMilliseconds} мс.", ConsoleColor.Magenta);
        }

        [Fact]
        public async Task DeleteAllEventsTest()
        {
            var stopwatch = Stopwatch.StartNew();
            PrintMessage("=== Тест: Удаление всех событий ===", ConsoleColor.Cyan);

            // 1. Получаем токен
            var token = await GetJwtToken();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // 2. Удаляем
            var deleteUrl = $"{SwaggerApiBaseUrl}/api/events/all";
            var deleteResponse = await _client.DeleteAsync(deleteUrl);
            deleteResponse.EnsureSuccessStatusCode();

            var deleteContent = await deleteResponse.Content.ReadAsStringAsync();
            var deleteApiResponse = JsonSerializer.Deserialize<ApiResponse<string>>(deleteContent, _options);

            Assert.NotNull(deleteApiResponse);
            Assert.True(deleteApiResponse.Success);
            PrintMessage("Все события успешно удалены.", ConsoleColor.Green);

            // 3. Проверяем, что теперь пусто
            var getUrl = $"{SwaggerApiBaseUrl}/api/events";
            var finalGetResponse = await _client.GetAsync(getUrl);
            finalGetResponse.EnsureSuccessStatusCode();

            var finalResponseContent = await finalGetResponse.Content.ReadAsStringAsync();
            var finalApiResponse = JsonSerializer.Deserialize<ApiResponse<List<EventModel>>>(finalResponseContent, _options);

            Assert.NotNull(finalApiResponse);
            Assert.True(finalApiResponse.Success);
            Assert.NotNull(finalApiResponse.Data);
            Assert.Empty(finalApiResponse.Data);

            stopwatch.Stop();
            PrintMessage($"Тест успешно завершён. Время выполнения: {stopwatch.ElapsedMilliseconds} мс.", ConsoleColor.Magenta);
        }

        [Fact]
        public async Task SendObjectToKafkaTest()
        {
            var stopwatch = Stopwatch.StartNew();
            PrintMessage("=== Тест: Отправка объекта в Kafka ===", ConsoleColor.Cyan);

            // 1. Получаем токен
            var token = await GetJwtToken();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // 2. Удаляем все объекты
            var deleteUrl = $"{SwaggerApiBaseUrl}/api/objects/all";
            var deleteResponse = await _client.DeleteAsync(deleteUrl);
            deleteResponse.EnsureSuccessStatusCode();
            PrintMessage("Удалены все объекты перед началом теста.", ConsoleColor.Yellow);

            // 3. Создаём объект
            var newObject = new ObjectModel
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Object",
                UserId = "1"
            };

            // 4. Отправляем
            var postUrl = $"{SwaggerApiBaseUrl}/api/objects";
            var content = new StringContent(JsonSerializer.Serialize(newObject), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(postUrl, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var postApiResponse = JsonSerializer.Deserialize<ApiResponse<ObjectModel>>(responseString, _options);

            Assert.NotNull(postApiResponse);
            Assert.True(postApiResponse.Success);
            PrintMessage("Объект успешно отправлен в Kafka.", ConsoleColor.Green);

            stopwatch.Stop();
            PrintMessage($"Тест завершён. Время выполнения: {stopwatch.ElapsedMilliseconds} мс.", ConsoleColor.Magenta);
        }

        [Fact]
        public async Task Gateway_ForwardsRequestToEventsService()
        {
            PrintMessage("=== Тест: Перенаправление запроса через API Gateway ===", ConsoleColor.Cyan);
            var token = await GetJwtToken();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Просто GET запрос к ApiGateway
            var gatewayUrl = $"{GatewayBaseUrl}/gateway/events";
            var response = await _client.GetAsync(gatewayUrl);

            Assert.True(response.IsSuccessStatusCode, "Запрос через Gateway вернулся с ошибкой.");

            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<EventModel>>>(responseContent, _options);

            Assert.NotNull(apiResponse);
            Assert.True(apiResponse.Success, "Gateway вернул success=false.");
            PrintMessage($"Тест успешно завершён. Событий получено: {apiResponse.Data?.Count ?? 0}", ConsoleColor.Green);
        }

        [Fact]
        public async Task GenerateJwtTokenTest()
        {
            PrintMessage("=== Тест: Генерация JWT токена ===", ConsoleColor.Cyan);

            // Запрос на SwaggerAPI, чтобы сгенерировать токен
            var tokenUrl = $"{SwaggerApiBaseUrl}/api/auth/token";
            var response = await _client.PostAsync(tokenUrl, null);

            PrintMessage($"Статус ответа: {response.StatusCode}", ConsoleColor.Yellow);
            var responseContent = await response.Content.ReadAsStringAsync();
            PrintMessage($"Содержимое ответа: {responseContent}", ConsoleColor.Yellow);

            Assert.True(response.IsSuccessStatusCode, "Запрос завершился с ошибкой.");

            var tokenData = JsonSerializer.Deserialize<Dictionary<string, string>>(responseContent, _options);
            Assert.NotNull(tokenData);
            Assert.True(tokenData.ContainsKey("token"), "Ответ не содержит ключ 'token'.");

            var token = tokenData["token"];
            Assert.False(string.IsNullOrEmpty(token), "Токен пустой или отсутствует.");

            PrintMessage($"Тест успешно завершён. Сгенерированный токен: {token}", ConsoleColor.Green);
        }

        [Fact]
        public async Task AccessProtectedResourceWithJwtTest()
        {
            PrintMessage("=== Тест: Доступ к защищённому ресурсу с JWT токеном ===", ConsoleColor.Cyan);

            // Генерация токена (через реальный SwaggerAPI)
            var token = await GetJwtToken();
            PrintMessage($"Используемый токен: {token}", ConsoleColor.Yellow);

            // Устанавливаем его в заголовок
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Запрос к защищённому ресурсу через Gateway
            var gatewayUrl = $"{GatewayBaseUrl}/gateway/events";
            var response = await _client.GetAsync(gatewayUrl);
            var protectedResponseContent = await response.Content.ReadAsStringAsync();

            PrintMessage($"Заголовок Authorization: {_client.DefaultRequestHeaders.Authorization}", ConsoleColor.Yellow);
            PrintMessage($"Статус ответа: {response.StatusCode}", ConsoleColor.Yellow);
            PrintMessage($"Содержимое ответа: {protectedResponseContent}", ConsoleColor.Yellow);

            // Проверяем, что запрос успешен
            Assert.True(response.IsSuccessStatusCode, "Доступ к защищённому ресурсу не выполнен.");
        }

        [Fact]
        public async Task AccessProtectedResourceWithoutJwtTest()
        {
            PrintMessage("=== Тест: Попытка доступа к защищённому ресурсу без JWT токена ===", ConsoleColor.Cyan);

            // Убираем заголовок Authorization
            _client.DefaultRequestHeaders.Authorization = null;

            // Делаем запрос через Gateway
            var gatewayUrl = $"{GatewayBaseUrl}/gateway/events";
            var response = await _client.GetAsync(gatewayUrl);
            var responseContent = await response.Content.ReadAsStringAsync();

            PrintMessage($"Статус ответа: {response.StatusCode}", ConsoleColor.Yellow);
            PrintMessage($"Содержимое ответа: {responseContent}", ConsoleColor.Yellow);

            // Ожидаем, что без токена будет 401 Unauthorized
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}