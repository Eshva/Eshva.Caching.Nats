Feature: Try get entry from object-store based cache

  Background:
    Given expired entries purging interval 2 minutes
    And default sliding expiration interval 1 minutes
    And object-store based cache with synchronous purge
    And entry with key 'big one' and random byte array as value which expires in 3 minutes put into cache
    And entry with key 'will be gotten' and value 'will be gotten value' which expires in 3 minutes put into cache
    And entry with key 'will be removed' and value 'will be removed value' which expires in 1 minutes put into cache

  Scenario: 01. Get will be gotten cache entry by key asynchronously
    When I try get 'big one' cache entry asynchronously
    Then cache entry successfully read
    Then I should get same value as the requested entry

  Scenario: 02. Get will be gotten cache entry by key synchronously
    When I try get 'will be gotten' cache entry synchronously
    Then cache entry successfully read
    Then I should get value 'will be gotten value' as the requested entry

  Scenario: 03. Get missing cache entry by key asynchronously
    When I try get 'missing' cache entry asynchronously
    Then cache entry did not read
    Then no errors are reported
    Then I should get value '' as the requested entry

  Scenario: 04. Get cache entry with corrupted metadata asynchronously should get error
    Given metadata of cache entry with key 'will be gotten' corrupted
    When I try get 'will be gotten' cache entry asynchronously
    Then invalid operation exception should be reported

  Scenario: 05. Get cache entry with corrupted metadata synchronously should get error
    Given metadata of cache entry with key 'will be gotten' corrupted
    When I get 'will be gotten' cache entry synchronously
    Then invalid operation exception should be reported

  Scenario: 06. Get cache entry operation triggers purging expired entries if its interval has passed
    Given passed a bit more than purging expired entries interval
    When I try get 'will be gotten' cache entry asynchronously
    Then cache entry successfully read
    Then 'will be gotten' entry is present in the object-store bucket
    And 'will be removed' entry is not present in the object-store bucket

  Scenario: 07. Get cache entry operation does not trigger purging expired entries if its interval has not passed
    Given passed a bit less than purging expired entries interval
    When I try get 'will be gotten' cache entry asynchronously
    Then cache entry successfully read
    Then 'will be gotten' entry is present in the object-store bucket
    And 'will be removed' entry is present in the object-store bucket

  Scenario: 08. Expiration should be postponed for gotten entry
    Given passed a bit more than purging expired entries interval
    When I try get 'will be gotten' cache entry asynchronously
    Then cache entry successfully read
    Then 'will be gotten' entry is present in the object-store bucket
    And 'will be gotten' entry should be expired today at 00:05:01
    And 'will be removed' entry is not present in the object-store bucket
