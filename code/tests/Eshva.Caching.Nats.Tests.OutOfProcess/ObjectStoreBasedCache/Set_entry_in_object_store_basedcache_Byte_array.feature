@out-of-process
Feature: Set entry in object store based cache. Byte array
Those out of process tests work with the same NATS object store bucket. They will interfere with cache entry names and
purging expired entries. This is the reason why they can not be run in parallel.

  Background:
    Given expired entries purging interval 2 minutes
    And default sliding expiration interval 1 minutes
    And object store based cache
    And entry with key 'existing' and value 'existing value' which expires in 3 minutes put into cache
    And entry with key 'will be removed' and value 'will be removed value' which expires in 1 minutes put into cache

  Scenario: 01. Set a new cache entry asynchronosly with sliding expiration
    Given time passed by 2 minutes
    When I set using byte array asynchronously 'new' cache entry with value 'some value' and sliding expiration in 5 minutes
    Then 'new' entry is present in the object store bucket
    And 'will be removed' entry is not present in the object store bucket
    And 'new' entry should be expired today at 00:07

  Scenario: 02. Set a new cache entry synchronosly with sliding expiration
    Given time passed by 2 minutes
    When I set using byte array synchronously 'new' cache entry with value 'some value' and sliding expiration in 5 minutes
    Then 'new' entry is present in the object store bucket
    And 'will be removed' entry is not present in the object store bucket
    And 'new' entry should be expired today at 00:07

  Scenario: 03. Set a new cache entry asynchronosly with absolute expiration
    Given time passed by 2 minutes
    When I set using byte array asynchronously 'new' cache entry with value 'some value' and absolute expiration at today at 00:30
    Then 'new' entry is present in the object store bucket
    And 'will be removed' entry is not present in the object store bucket
    And 'new' entry should be expired today at 00:30

  Scenario: 04. Set a new cache entry synchronosly with absolute expiration
    Given time passed by 2 minutes
    When I set using byte array synchronously 'new' cache entry with value 'some value' and absolute expiration at today at 00:30
    Then 'new' entry is present in the object store bucket
    And 'will be removed' entry is not present in the object store bucket
    And 'new' entry should be expired today at 00:30

  Scenario: 05. Set a new cache entry asynchronosly with absolute expiration relative to now
    Given time passed by 2 minutes
    When I set using byte array asynchronously 'new' cache entry with value 'some value' and absolute expiration 00:30 relative to now
    Then 'new' entry is present in the object store bucket
    And 'will be removed' entry is not present in the object store bucket
    And 'new' entry should be expired today at 00:32

  Scenario: 06. Set a new cache entry synchronosly with absolute expiration relative to now
    Given time passed by 2 minutes
    When I set using byte array synchronously 'new' cache entry with value 'some value' and absolute expiration 00:30 relative to now
    Then 'new' entry is present in the object store bucket
    And 'will be removed' entry is not present in the object store bucket
    And 'new' entry should be expired today at 00:32

  Scenario: 07. Set a new cache entry asynchronosly with absolute and sliding expiration
    Given time passed by 2 minutes
    When I set using byte array asynchronously 'new' cache entry with value 'some value' and absolute expiration at today at 00:30 and sliding expiration in 5 minutes
    Then 'new' entry is present in the object store bucket
    And 'will be removed' entry is not present in the object store bucket
    And 'new' entry should be expired today at 00:07

  Scenario: 08. Set a new cache entry synchronosly with absolute and sliding expiration
    Given time passed by 2 minutes
    When I set using byte array synchronously 'new' cache entry with value 'some value' and absolute expiration at today at 00:30 and sliding expiration in 5 minutes
    Then 'new' entry is present in the object store bucket
    And 'will be removed' entry is not present in the object store bucket
    And 'new' entry should be expired today at 00:07

  Scenario: 09. Set an existing cache entry asynchronosly with sliding expiration
    Given time passed by 2 minutes
    When I set using byte array asynchronously 'existing' cache entry with value 'some value' and sliding expiration in 5 minutes
    Then 'existing' entry is present in the object store bucket
    And 'will be removed' entry is not present in the object store bucket
    And 'existing' entry should be expired today at 00:07

  Scenario: 10. Set a new cache entry synchronosly with sliding expiration
    Given time passed by 2 minutes
    When I set using byte array synchronously 'existing' cache entry with value 'some value' and sliding expiration in 5 minutes
    Then 'existing' entry is present in the object store bucket
    And 'will be removed' entry is not present in the object store bucket
    And 'existing' entry should be expired today at 00:07

  Scenario: 11. Set an existing cache entry asynchronosly with absolute expiration
    Given time passed by 2 minutes
    When I set using byte array asynchronously 'existing' cache entry with value 'some value' and absolute expiration at today at 00:30
    Then 'existing' entry is present in the object store bucket
    And 'will be removed' entry is not present in the object store bucket
    And 'existing' entry should be expired today at 00:30

  Scenario: 12. Set a new cache entry synchronosly with absolute expiration
    Given time passed by 2 minutes
    When I set using byte array synchronously 'existing' cache entry with value 'some value' and absolute expiration at today at 00:30
    Then 'existing' entry is present in the object store bucket
    And 'will be removed' entry is not present in the object store bucket
    And 'existing' entry should be expired today at 00:30

  Scenario: 13. Set an existing cache entry asynchronosly with absolute and sliding expiration
    Given time passed by 2 minutes
    When I set using byte array asynchronously 'existing' cache entry with value 'some value' and absolute expiration at today at 00:30 and sliding expiration in 5 minutes
    Then 'existing' entry is present in the object store bucket
    And 'will be removed' entry is not present in the object store bucket
    And 'existing' entry should be expired today at 00:07

  Scenario: 14. Set an existing cache entry synchronosly with absolute and sliding expiration
    Given time passed by 2 minutes
    When I set using byte array synchronously 'existing' cache entry with value 'some value' and absolute expiration at today at 00:30 and sliding expiration in 5 minutes
    Then 'existing' entry is present in the object store bucket
    And 'will be removed' entry is not present in the object store bucket
    And 'existing' entry should be expired today at 00:07

  Scenario: 15. Set an existing cache entry asynchronosly with absolute expiration relative to now
    Given time passed by 2 minutes
    When I set using byte array asynchronously 'existing' cache entry with value 'some value' and absolute expiration 00:30 relative to now
    Then 'existing' entry is present in the object store bucket
    And 'will be removed' entry is not present in the object store bucket
    And 'existing' entry should be expired today at 00:32

  Scenario: 16. Set an existing cache entry synchronosly with absolute expiration relative to now
    Given time passed by 2 minutes
    When I set using byte array synchronously 'existing' cache entry with value 'some value' and absolute expiration 00:30 relative to now
    Then 'existing' entry is present in the object store bucket
    And 'will be removed' entry is not present in the object store bucket
    And 'existing' entry should be expired today at 00:32
