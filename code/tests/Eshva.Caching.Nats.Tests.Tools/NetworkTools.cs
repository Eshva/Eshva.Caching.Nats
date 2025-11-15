using System.Net.NetworkInformation;

namespace Eshva.Caching.Nats.Benchmarks.Tools;

/// <summary>
/// Network tools for integration tests.
/// </summary>
public static class NetworkTools {
  /// <summary>
  /// Gets a currently free TCP port.
  /// </summary>
  /// <remarks>
  /// Useful in integration tests when required a random port for a service.
  /// </remarks>
  /// <param name="rangeStart">
  /// The lookup range start value. Inclusively. Default value is 49152.
  /// </param>
  /// <param name="rangeFinish">
  /// The lookup range finish value. Inclusively. Default value is 65535.
  /// </param>
  /// <returns>
  /// The first free TCP port from the lookup range.
  /// </returns>
  /// <exception cref="ArgumentException">
  /// Unable to find a free port in range specified.
  /// </exception>
  public static ushort GetFreeTcpPort(ushort rangeStart = DynamicTcpPortRangeStart, ushort rangeFinish = DynamicTcpPortRangeFinish) {
    var fromPort = Math.Min(rangeStart, rangeFinish);
    var toPort = Math.Max(rangeStart, rangeFinish);

    var properties = IPGlobalProperties.GetIPGlobalProperties();
    var listeners = properties.GetActiveTcpListeners();
    var openPorts = listeners.Select(item => item.Port).ToArray();

    for (var port = fromPort; port <= toPort; port++) {
      if (openPorts.All(openPort => openPort != port)) return port;
    }

    throw new ArgumentException($"Unable to find a free port in range from {fromPort} to {toPort}.");
  }

  /// <summary>
  /// The start of dynamic TCP port range.
  /// </summary>
  // ReSharper disable once MemberCanBePrivate.Global
  public const int DynamicTcpPortRangeStart = 49152;

  /// <summary>
  /// The finish of dynamic TCP port range.
  /// </summary>
  // ReSharper disable once MemberCanBePrivate.Global
  public const int DynamicTcpPortRangeFinish = 65535;
}
