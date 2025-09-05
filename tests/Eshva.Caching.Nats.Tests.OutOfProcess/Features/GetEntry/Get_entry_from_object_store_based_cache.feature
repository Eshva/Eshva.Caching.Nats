Feature: Get entry from object-store based cache

  Background:
    Given entry with key 'existing' and value 'existing value' which never expires put into cache

  Scenario: Get existing cache entry by key asynchronously
    When I get 'existing' cache entry asynchronously
    Then I should get value 'existing value' as the requested entry

  Scenario: Get existing cache entry by key synchronously
    When I get 'existing' cache entry synchronously
    Then I should get value 'existing value' as the requested entry

  Scenario: Get missing cache entry by key asynchronously
    When I get 'missing' cache entry asynchronously
    Then no errors are reported
    Then I should get a null value as the requested entry

  Scenario: Get cache entry asynchronously on cache with closed connection should get error
    When I get from cache some entry with corrupted metadata asynchronously
    Then invalid operation exception should be reported

  Scenario: Get cache entry synchronously on cache with closed connection should get error
    When I get from cache some entry with corrupted metadata synchronously
    Then invalid operation exception should be reported
