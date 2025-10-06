@out-of-process
Feature: Remove entry from object-store based cache

  Background:
    Given expired entries purging interval 2 minutes
    And default sliding expiration interval 1 minutes
    And object-store based cache with synchronous purge
    And entry with key 'existing 3 minutes' and value 'existing 3 minutes value' which expires in 3 minutes put into cache
    And entry with key 'existing 4 minutes' and value 'existing 4 minutes value' which expires in 4 minutes put into cache
    And entry with key 'existing 1 minutes' and value 'existing 1 minutes value' which expires in 1 minutes put into cache

  Scenario: 01. Remove existing non-expired cache entry by key asynchronously
    Given time passed by 2 minutes
    When I remove 'existing 3 minutes' cache entry asynchronously
    Then 'existing 3 minutes' entry is not present in the object-store bucket
    And 'existing 4 minutes' entry is present in the object-store bucket
    And 'existing 1 minutes' entry is not present in the object-store bucket

  Scenario: 02. Remove existing non-expired cache entry by key synchronously
    Given time passed by 2 minutes
    When I remove 'existing 3 minutes' cache entry synchronously
    Then 'existing 3 minutes' entry is not present in the object-store bucket
    And 'existing 4 minutes' entry is present in the object-store bucket
    And 'existing 1 minutes' entry is not present in the object-store bucket

  Scenario: 03. Remove existing expired cache entry by key asynchronously should not report any errors
    Given time passed by 2 minutes
    When I remove 'existing 1 minutes' cache entry asynchronously
    Then 'existing 3 minutes' entry is present in the object-store bucket
    And 'existing 4 minutes' entry is present in the object-store bucket
    And 'existing 1 minutes' entry is not present in the object-store bucket

  Scenario: 04. Remove existing expired cache entry by key synchronously should not report any errors
    Given time passed by 2 minutes
    When I remove 'existing 1 minutes' cache entry synchronously
    Then 'existing 3 minutes' entry is present in the object-store bucket
    And 'existing 4 minutes' entry is present in the object-store bucket
    And 'existing 1 minutes' entry is not present in the object-store bucket

  Scenario: 05. Remove asychronously cache entry with correpted metadata should report an errors
    Given metadata of cache entry with key 'existing 1 minutes' corrupted
    When I remove 'existing 1 minutes' cache entry asynchronously
    Then invalid operation exception should be reported

  Scenario: 06. Remove sychronously cache entry with correpted metadata should report an errors
    Given metadata of cache entry with key 'existing 1 minutes' corrupted
    When I remove 'existing 1 minutes' cache entry synchronously
    Then invalid operation exception should be reported
