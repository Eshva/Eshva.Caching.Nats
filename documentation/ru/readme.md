# Eshva.Caching.Nats

NATS — замечательная платформа для организации взаимодействия приложений. В начале это была просто шина сообщений, но её авторы постоянно расширяют её функциональность. В данный момент в составе NATS уже есть key/value и объектное хранилища, построенные на всё той же инфраструктуре обмена сообщениями. Эти хранилища можно использовать для построения распределённого кэша.

Данный пакет предоставляет две реализации контракта [IDistributedCache](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.distributed.idistributedcache?view=net-9.0-pp) (и его развития [IBufferDistributedCache](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.distributed.ibufferdistributedcache?view=net-9.0-pp)):

- `NatsKeyValueStoreBasedCache` — реализация, основанная на key/value хранилище NATS.
- `NatsObjectStoreBasedCache` — реализация, основанная на объектном хранилище NATS.

## Инсталляция

Как обычно, или подключайте пакет `Eshva.Caching.Nats` в вашей любимой среде разработки, или с помощью командной строки:

```bash
dotnet add package Eshva.Caching.Nats
```

## Использование

Обычно в приложении используется сразу несколько кэшей, по одному на каждый тип кэшируемых объектов. Это важно, так как для разных данных различаются время жизни объектов до устаревания, хранилища данных, размеры объектов и т.д. Поэтому важно иметь возможность добавить в DI-контейнер приложения несколько кэшей с разными настройками. Я выбрал использование так называемых keyed-сервисов при регистрации в DI-контейнере. Вам доступны следующие методы расширения:

```csharp
// Добавить кэш, основанный на объектном хранилище NATS. Клиент NATS будет получен
// из DI-контейнера по ключу natsServerKey, настройки кэша будут получены по ключу
// serviceKey. Сам кэш будет зарегистрирован с ключом serviceKey.
public static IServiceCollection AddKeyedNatsObjectStoreBasedCache(
    this IServiceCollection services,
    string serviceKey,
    string natsServerKey)

// Добавить кэш, основанный на объектном хранилище NATS. Клиент NATS будет получен
// из DI-контейнера без использования клюа, настройки кэша будут получены по ключу
// serviceKey. Сам кэш будет зарегистрирован по ключу serviceKey.
public static IServiceCollection AddKeyedNatsObjectStoreBasedCache(
		this IServiceCollection services,
		string serviceKey)

// Добавить кэш, основанный на объектном хранилище NATS. Клиент NATS будет получен
// из DI-контейнера по ключу natsServerKey, настройки кэша будут получены без ключа.
// Сам кэш будет зарегистрирован без использования ключа.
public static IServiceCollection AddNatsObjectStoreBasedCache(
		this IServiceCollection services,
		string natsServerKey)

// Добавить кэш, основанный на объектном хранилище NATS. Клиент NATS будет получен
// из DI-контейнера без ключа, настройки кэша будут получены без ключа.
// Сам кэш будет зарегистрирован без использования ключа.
public static IServiceCollection AddNatsObjectStoreBasedCache(
		this IServiceCollection services)

// Добавить кэш, основанный на key/value хранилище NATS. Клиент NATS будет получен
// из DI-контейнера по ключу natsServerKey, настройки кэша будут получены по ключу
// serviceKey. Сам кэш будет зарегистрирован с ключом serviceKey.
public static IServiceCollection AddKeyedNatsKeyValueBasedCache(
    this IServiceCollection services,
    string serviceKey,
    string natsServerKey)

// Добавить кэш, основанный на key/value хранилище NATS. Клиент NATS будет получен
// из DI-контейнера без использования клюа, настройки кэша будут получены по ключу
// serviceKey. Сам кэш будет зарегистрирован по ключу serviceKey.
public static IServiceCollection AddKeyedNatsKeyValueBasedCache(
    this IServiceCollection services,
    string serviceKey)

// Добавить кэш, основанный на key/value хранилище NATS. Клиент NATS будет получен
// из DI-контейнера по ключу natsServerKey, настройки кэша будут получены без ключа.
// Сам кэш будет зарегистрирован без использования ключа.
public static IServiceCollection AddNatsKeyValueBasedCache(
		this IServiceCollection services,
		string natsServerKey)

// Добавить кэш, основанный на key/value хранилище NATS. Клиент NATS будет получен
// из DI-контейнера по ключу natsServerKey, настройки кэша будут получены по ключу
// serviceKey. Сам кэш будет зарегистрирован с ключом serviceKey.
public static IServiceCollection AddNatsKeyValueBasedCache(
		this IServiceCollection services)
```

Для обоих типов кэша в DI-контейнере должен быть зарегистрирован клиент NATS `INatsConnection`.

Для кэша, основанного на объектном хранилище в DI-контейнере должен быть зарегистрирован экземпляр настроек типа `ObjectStoreBasedCacheSettings`.

Для кэша, основанного на key/value хранилище в DI-контейнере должен быть зарегистрирован экземпляр настроек типа `KeyValueBasedCacheSettings`.

Поскольку кэш регистрируется как singleton, все сервисы, от которых он зависит, также должны быть зарегистрированы как singleton. В зависимости от ваших потребностей используйте один из четырёх методов расширения. Методы, принимающие `serviceKey`, рассчитаны на случай, когда у вас есть несколько кэшей одного типа. Методы, принимающие `natsServerKey`, рассчитаны на случай, когда в вашем приложении используется несколько экземпляров NATS и, соответственно клиентов для подключения к ним (обычно для одного экземпляра NATS в приложении только один клиент).

[Пример регистрации](https://github.com/Eshva/Eshva.Caching.Nats/blob/master/code/benchmarks/Eshva.Caching.Nats.TestWebApp/Bootstrapping/ApplicationBootstrapping.cs) можно посмотреть в ASP.NET приложении, расположенном в этом репозитории. Выглядит это примерно следующим образом:

```csharp
private static void AddSharedServices(this IServiceCollection services) {
    services.AddSingleton(TimeProvider.System);
    services.AddKeyedSingleton<INatsConnection>(
      CacheNatsServerKey,
      (diContainer, key) => {
        var settings = diContainer.GetRequiredKeyedService<NatsServerSettings>(CacheNatsServerKey);
        var connectionName = settings.NatsConnectionName;
        return new NatsConnection(
          new NatsOpts {
            Url = settings.NatsServerConnectionString,
            Name = !string.IsNullOrWhiteSpace(connectionName) ? connectionName : $"Connection for {key}"
          });
      });
  }

  public static void AddConfiguration(IServiceCollection services) {
    services.AddOptions<ObjectStoreBasedCacheSettings>()
      .BindConfiguration(ObjectStoreCacheConfigurationSectionPath)
      .ValidateDataAnnotations()
      .ValidateOnStart();
    services.AddKeyedSingleton<ObjectStoreBasedCacheSettings>(
      ObjectStoreCacheKey,
      (diContainer, _) => diContainer.GetRequiredService<IOptions<ObjectStoreBasedCacheSettings>>().Value);
    services.AddOptions<KeyValueBasedCacheSettings>()
      .BindConfiguration(KeyValueCacheConfigurationSectionPath)
      .ValidateDataAnnotations()
      .ValidateOnStart();
    services.AddKeyedSingleton<KeyValueBasedCacheSettings>(
      KeyValueCacheKey,
      (diContainer, _) => diContainer.GetRequiredService<IOptions<KeyValueBasedCacheSettings>>().Value);
  }

  public static void AddServices(IServiceCollection services, string natsServerKey) {
    services.AddKeyedNatsObjectStoreBasedCache(ObjectStoreCacheKey, natsServerKey);
    services.AddKeyedNatsKeyValueBasedCache(KeyValueCacheKey, natsServerKey);
  }

  private const string CacheNatsServerKey = "CacheNatsServer";
  private const string ObjectStoreCacheKey = "ObjectStoreBasedCache";
  private const string ObjectStoreCacheConfigurationSectionPath = "ImageCache:ObjectStoreBasedCache";
  private const string KeyValueCacheKey = "KeyValueBasedCache";
  private const string KeyValueCacheConfigurationSectionPath = "ImageCache:KeyValueBasedCache";

```

Для того, чтобы использовать keyed-кэш в ваших обработчиках HTTP-запросов (дан пример использования Minimal API) их необходимо зарегистрировать следующим образом:

```csharp
  public static void AddHttpHandlers(IServiceCollection services, string natsServerKey) {
    services.AddKeyedTransient<GetImageWithObjectStoreTryGetAsyncHttpRequestHandler>(
      ObjectStoreCacheKey,
      (diContainer, key) => new GetImageWithObjectStoreTryGetAsyncHttpRequestHandler(
        diContainer.GetRequiredKeyedService<IBufferDistributedCache>(key),
        diContainer.GetRequiredService<ILogger<GetImageWithObjectStoreTryGetAsyncHttpRequestHandler>>()));

    services.AddKeyedTransient<GetImageWithKeyValueTryGetAsyncHttpRequestHandler>(
      KeyValueCacheKey,
      (diContainer, key) => new GetImageWithKeyValueTryGetAsyncHttpRequestHandler(
        diContainer.GetRequiredKeyedService<IBufferDistributedCache>(key),
        diContainer.GetRequiredService<ILogger<GetImageWithKeyValueTryGetAsyncHttpRequestHandler>>()));
  }

  public static void MapEndpoints(IEndpointRouteBuilder endpoints) {
    endpoints.MapGet(
      "/object-store/try-get-async/{name}",
      async (
        [FromKeyedServices(ObjectStoreCacheKey)]
        GetImageWithObjectStoreTryGetAsyncHttpRequestHandler handler,
        string name) => await handler.Handle(name));

    endpoints.MapGet(
      "/key-value/try-get-async/{name}",
      async (
        [FromKeyedServices(KeyValueCacheKey)] GetImageWithKeyValueTryGetAsyncHttpRequestHandler handler,
        string name) => await handler.Handle(name));
  }
```

Файл конфигурации при этом выглядит следующим образом (конечно, вы можете смаппить `ObjectStoreBasedCacheSettings` и `KeyValueBasedCacheSettings` как вам удобнее):

```json
{
  "CacheNatsServer": {
    "NatsServerConnectionString": "NOT SPECIFIED",
    "NatsConnectionName": "TestWebApp"
  },
  "ImageCache": {
    "ObjectStoreBasedCache": {
      "BucketName": "images-object",
      "DefaultSlidingExpirationInterval": "00:05:00",
      "ExpiredEntriesPurgingInterval": "00:05:00",
      "MaximalCacheInvalidationDuration": "00:03:00"
    },
    "KeyValueBasedCache": {
      "BucketName": "images-key-value",
      "DefaultSlidingExpirationInterval": "00:05:00",
      "ExpiredEntriesPurgingInterval": "00:05:00",
      "MaximalCacheInvalidationDuration": "00:03:00"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```
