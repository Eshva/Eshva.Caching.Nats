Feature: Key/value store based in DI container registration

  Background:
    Given service collection
    And time provider registered in DI-container
    And cache entry expiry calculator registered in DI-container
    And cache logger is registered in DI-container
    And cache invalidation logger is registered in DI-container
    And key-value store bucket created

  Scenario: 01. Register keyed key/value store based cache with keyed NATS client and keyed settings
    Given key-value store based cache settings registered in DI-container with key 'cache-service'
    And NATS client registered in DI-container with key 'nats-client'
    When I register key-value store based cache in DI-container with key 'cache-service' and NATS client key 'nats-client'
    Then service provided created from service collection
    And it should be possible to get cache instance with key 'cache-service'

  Scenario: 02. Register keyed key/value store based cache with normal NATS client and keyed settings
    Given key-value store based cache settings registered in DI-container with key 'cache-service'
    And NATS client registered in DI-container
    When I register key-value store based cache in DI-container with key 'cache-service' and NATS client without key
    Then service provided created from service collection
    And it should be possible to get cache instance with key 'cache-service'

  Scenario: 03. Register key/value store based cache with normal NATS client and normal settings
    Given key-value store based cache settings registered in DI-container without key
    And NATS client registered in DI-container
    When I register key-value store based cache in DI-container without key and NATS client without key
    Then service provided created from service collection
    And it should be possible to get cache instance without key

  Scenario: 04. Register key/value store based cache with keyed NATS client and normal settings
    Given key-value store based cache settings registered in DI-container without key
    And NATS client registered in DI-container with key 'nats-client'
    When I register key-value store based cache in DI-container without key and NATS client key 'nats-client'
    Then service provided created from service collection
    And it should be possible to get cache instance without key
