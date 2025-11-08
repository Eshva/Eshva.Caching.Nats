@out-of-process
Feature: Try get entry from key-value store based cache
Those out of process tests work with the same NATS object-store bucket. They will interfere with cache entry names and
purging expired entries. This is the reason why they can not be run in parallel.

  Background:
    Given expired entries purging interval 2 minutes
    And default sliding expiration interval 1 minutes
    And key-value store based cache
    And entry with key 'big-one' and random byte array as value which expires in 3 minutes put into cache
    And entry with key 'will-be-gotten' and value 'will-be-gotten value' which expires in 3 minutes put into cache
    And entry with key 'will-be-removed' and value 'will-be-removed value' which expires in 1 minutes put into cache

  Scenario: 01. Get will-be-gotten cache entry by key asynchronously
    When I try get 'big-one' cache entry asynchronously
    Then cache entry successfully read
    And I should get same value as the requested entry

  Scenario: 02. Get will-be-gotten cache entry by key synchronously
    When I try get 'will-be-gotten' cache entry synchronously
    Then cache entry successfully read
    And I should get value 'will-be-gotten value' as the requested entry

  Scenario: 03. Get missing cache entry by key asynchronously
    When I try get 'missing' cache entry asynchronously
    Then cache entry did not read
    And no errors are reported
    And I should get value '' as the requested entry

  Scenario: 04. Get cache entry operation triggers purging expired entries if its interval has passed
    Given time passed by 2,5 minutes
    When I try get 'will-be-gotten' cache entry asynchronously
    Then cache entry successfully read
    And cache invalidation done
    And 'will-be-gotten' entry is present in the object-store bucket
    And 'will-be-removed' entry is not present in the object-store bucket

  Scenario: 05. Get cache entry operation does not trigger purging expired entries if its interval has not passed
    Given time passed by 1,5 minutes
    When I try get 'will-be-gotten' cache entry asynchronously
    Then cache entry successfully read
    And cache invalidation not started
    And 'will-be-gotten' entry is present in the object-store bucket
    And 'will-be-removed' entry is present in the object-store bucket

  Scenario: 06. Expiration should be postponed for gotten entry
    Given time passed by 2,5 minutes
    When I try get 'will-be-gotten' cache entry asynchronously
    Then cache entry successfully read
    And cache invalidation done
    And 'will-be-gotten' entry is present in the object-store bucket
    And 'will-be-gotten' entry should be expired today at 00:05:30
    And 'will-be-removed' entry is not present in the object-store bucket
