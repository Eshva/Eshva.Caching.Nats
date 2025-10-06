NATS-based distributed caches

[NATS](https://nats.io) is a wonderful communication platform. It's started as a message bus but the authors constantly expand its functionality. Currently, it provides for instance [key-value store](https://docs.nats.io/nats-concepts/jetstream/key-value-store) and [object store](https://docs.nats.io/nats-concepts/jetstream/obj_store) that could be used as backend for distributed cache storage.

This package provides implementation of .NET's [IDistributedCache](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.distributed.idistributedcache?view=net-9.0-pp) and
[IBufferDistributedCache](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.distributed.ibufferdistributedcache?view=net-9.0-pp) contracts using NATS object store and (not yet) key-value store.

# Installation
TBD

# Usage
TBD
