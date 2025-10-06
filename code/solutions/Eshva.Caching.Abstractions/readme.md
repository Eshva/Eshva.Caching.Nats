# Abstractions for different cache implementations

Here I collect abstractions required for my different cache implementations. In the future this project should
become a separate package.

Why this package is required:
- In the future I plan to add a NATS key-value store cache. Currently only NATS object-store based cache is implemented.
- Here will be placed an abstract cache class that will implement behavioral contract I test in BDD-tests. Behavior of the cache should be implemented once and inherited realizations should adjust only storage-related aspects.
- Stored/distributed cache has more responsibilities than just store and retrieve cache entries. According to SOLID they should be separated into different classes.

