Feature: Delete expired cache entries with purger for NATS object-store based cache

  Background:
    Given entry with key 'expires in 3 minutes' and value 'expires in 3 minutes value' which expires in 3 minutes put into cache
    And entry with key 'expires in 1 minute' and value 'expires in 1 minute value' which expires in 1 minutes put into cache
    And purger for NATS object-store based cache with purging interval 2 minutes

  Scenario: 01. Purger should remove expired entries from object-store if passed more time than purging interval
    Given time passed by 2 minutes
    When I request scan for expired entries if required
    Then 'expires in 3 minutes' entry is present in the object-store bucket
    And 'expires in 1 minute' entry is not present in the object-store bucket

  Scenario: 02. Purger should not remove any entries from object-store if passed less time than purging interval
    Given time passed by 1,5 minutes
    When I request scan for expired entries if required
    Then 'expires in 3 minutes' entry is present in the object-store bucket
    And 'expires in 1 minute' entry is present in the object-store bucket
