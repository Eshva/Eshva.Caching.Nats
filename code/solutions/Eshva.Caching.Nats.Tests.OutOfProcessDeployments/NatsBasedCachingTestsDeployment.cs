using JetBrains.Annotations;

namespace Eshva.Caching.Nats.Tests.OutOfProcessDeployments;

[PublicAPI]
public partial class NatsBasedCachingTestsDeployment(NatsBasedCachingTestsDeployment.Configuration configuration)
  : IOutOfProcessDeployment {
  public NatsServerDeployment NatsServer { get; private set; } = null!;

  public async Task Build() {
    NatsServer = configuration.NatsServer;
    await NatsServer.Build();
  }

  public async Task Start() {
    if (NatsServer is null) throw new InvalidOperationException("NATS server deployment is not initialized.");

    await NatsServer.Start();
  }

  /// <summary>
  /// Implicit conversion operator form <see cref="Configuration"/> to this deployment object type.
  /// </summary>
  /// <remarks>
  /// It used in the end of configuration chain in place of 'build()' method which placed not in
  /// configuration but deployment type. It's more natural to place factory method in product-class itself
  /// than in some nonstatic class. Same pattern is used for all other deployments and their configurations.
  /// </remarks>
  /// <param name="configuration">Deployment configuration.</param>
  /// <returns>
  /// A new deployment created with provided <paramref name="configuration"/>.
  /// </returns>
  public static implicit operator NatsBasedCachingTestsDeployment(Configuration configuration) => new(configuration);

  /// <summary>
  /// A factory method of a new configuration of the deployment.
  /// </summary>
  /// <remarks>
  /// Note, this factory method produce not a deployment object but its settings. Later implicit conversion
  /// operator is used to create a deployment from its settings. It's done to make fluent language more
  /// natural using less approaches to structure fluent configuration code. This way we don't need in
  /// a 'build()' method in the end of configuration chain and the chain itself looks more like configuring
  /// the final object itself. Same pattern is used for all other deployments and their configurations.
  /// </remarks>
  /// <param name="name">The name of the deployment.</param>
  /// <returns>
  /// A new deployment configuration named <paramref name="name"/>.
  /// </returns>
  public static Configuration Named(string name) => new() { Name = name };

  public async ValueTask DisposeAsync() {
    if (NatsServer is null) throw new InvalidOperationException("NATS server deployment is not initialized.");

    await NatsServer.DisposeAsync();
  }
}
