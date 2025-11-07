@out-of-process
Feature: Get entry from object-store based cache
Those out of process tests work with the same NATS object-store bucket. They will interfere with cache entry names and
purging expired entries. This is the reason why they can not be run in parallel.

  Background:
    Given expired entries purging interval 2 minutes
    And default sliding expiration interval 1 minutes
    And object-store based cache
    And entry with key 'will be gotten' and value 'will be gotten value' which expires in 3 minutes put into cache
    And entry with key 'will be removed' and value 'will be removed value' which expires in 1 minutes put into cache

  Scenario: 01. Get will be gotten cache entry by key asynchronously
    When I get 'will be gotten' cache entry asynchronously
    Then I should get value 'will be gotten value' as the requested entry

  Scenario: 02. Get will be gotten cache entry by key synchronously
    When I get 'will be gotten' cache entry synchronously
    Then I should get value 'will be gotten value' as the requested entry

  Scenario: 03. Get missing cache entry by key asynchronously
    When I get 'missing' cache entry asynchronously
    Then no errors are reported
    And I should get a null value as the requested entry

  Scenario: 04. Get cache entry with corrupted metadata asynchronously should get error
    Given metadata of cache entry with key 'will be gotten' corrupted
    When I get 'will be gotten' cache entry asynchronously
    Then invalid operation exception should be reported

  Scenario: 05. Get cache entry with corrupted metadata synchronously should get error
    Given metadata of cache entry with key 'will be gotten' corrupted
    When I get 'will be gotten' cache entry synchronously
    Then invalid operation exception should be reported

  Scenario: 06. Get cache entry operation triggers purging expired entries if its interval has passed
    Given time passed by 2,5 minutes
    When I get 'will be gotten' cache entry asynchronously
    Then cache invalidation done
    And 'will be gotten' entry is present in the object-store bucket
    And 'will be removed' entry is not present in the object-store bucket

  Scenario: 07. Get cache entry operation does not trigger purging expired entries if its interval has not passed
    Given time passed by 1,5 minutes
    When I get 'will be gotten' cache entry asynchronously
    Then cache invalidation not started
    And 'will be gotten' entry is present in the object-store bucket
    And 'will be removed' entry is present in the object-store bucket

  Scenario: 08. Expiration should be postponed for gotten entry
    Given time passed by 2,5 minutes
    When I get 'will be gotten' cache entry asynchronously
    Then cache invalidation done
    And 'will be gotten' entry is present in the object-store bucket
    And 'will be gotten' entry should be expired today at 00:05:30
    And 'will be removed' entry is not present in the object-store bucket
