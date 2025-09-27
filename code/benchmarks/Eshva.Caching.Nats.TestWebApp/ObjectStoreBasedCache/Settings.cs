using System.ComponentModel.DataAnnotations;

namespace Eshva.Caching.Nats.TestWebApp.ObjectStoreBasedCache;

public record Settings([Required] string NatsServerConnectionString);
