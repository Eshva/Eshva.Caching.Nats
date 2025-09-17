Feature: Get entry from object-store based cache

  Background:
    Given expired entries purging interval 2 minutes
    And default sliding expiration interval 1 minutes
    And object-store based cache
    And entry with key 'existing' and value 'existing value' which expires in 3 minutes put into cache
    And entry with key 'to be removed' and value 'to be removed value' which expires in 1 minutes put into cache

  Scenario: 01. Get existing cache entry by key asynchronously
    When I get 'existing' cache entry asynchronously
    Then I should get value 'existing value' as the requested entry

  Scenario: 02. Get existing cache entry by key synchronously
    When I get 'existing' cache entry synchronously
    Then I should get value 'existing value' as the requested entry

  Scenario: 03. Get missing cache entry by key asynchronously
    When I get 'missing' cache entry asynchronously
    Then no errors are reported
    And I should get a null value as the requested entry

  Scenario: 04. Get cache entry asynchronously on cache with closed connection should get error
    When I get from cache some entry with corrupted metadata asynchronously
    Then invalid operation exception should be reported

  Scenario: 05. Get cache entry synchronously on cache with closed connection should get error
    When I get from cache some entry with corrupted metadata synchronously
    Then invalid operation exception should be reported

  Scenario: 06. Get cache entry operation triggers purging expired entries if its interval has passed
    Given passed a bit more than purging expired entries interval
    When I get 'existing' cache entry asynchronously
    Then 'existing' entry is present in the object-store bucket
    And 'to be removed' entry is not present in the object-store bucket

  Scenario: 07. Get cache entry operation does not trigger purging expired entries if its interval has not passed
    Given passed a bit less than purging expired entries interval
    When I get 'existing' cache entry asynchronously
    When I get 'to be removed' cache entry asynchronously
    Then 'existing' entry is present in the object-store bucket
    And 'to be removed' entry is present in the object-store bucket

  Scenario: 08. Expiration should be postponed for gotten entry
    Given passed a bit more than purging expired entries interval
    When I get 'existing' cache entry asynchronously
    Then 'existing' entry is present in the object-store bucket
    And 'to be removed' entry is not present in the object-store bucket
