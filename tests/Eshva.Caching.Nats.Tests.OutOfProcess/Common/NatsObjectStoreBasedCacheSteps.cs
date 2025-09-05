using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Reqnroll;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Common;

[Binding]
public class NatsObjectStoreBasedCacheSteps {
  public NatsObjectStoreBasedCacheSteps(CachesContext cachesContext) {
    _cachesContext = cachesContext;
  }

  [Given("entry with key {string} and value {string} which never expires put into cache")]
  public async Task GivenEntryWithKeyStringAndValueStringWhichNeverExpiresPutIntoCache(string key, string value) =>
    await _cachesContext.CacheBucket.PutAsync(key, Encoding.UTF8.GetBytes(value));

  [Then("I should get value {string} as the requested entry")]
  public void ThenIShouldGetValueAsTheRequestedEntry(string value) =>
    Encoding.UTF8.GetBytes(value).Should().BeEquivalentTo(_cachesContext.GottenCacheEntryValue);

  [Then("I should get a null value as the requested entry")]
  public void ThenIShouldGetANullValueAsTheRequestedEntry() => _cachesContext.GottenCacheEntryValue.Should().BeNull();

  private readonly CachesContext _cachesContext;
}
