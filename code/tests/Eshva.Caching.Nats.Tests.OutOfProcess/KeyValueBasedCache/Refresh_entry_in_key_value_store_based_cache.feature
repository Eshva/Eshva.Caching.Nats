@out-of-process
Feature: Refresh entry in key-value store based cache
Those out of process tests work with the same NATS object store bucket. They will interfere with cache entry names and
purging expired entries. This is the reason why they can not be run in parallel.

  Background:
    Given clock set at today 00:00
    And expired entries purging interval 2 minutes
    And default sliding expiration interval 1 minutes
    And key-value store based cache
    And entry with key 'will-be-refreshed' and value 'will-be-refreshed value' which expires in 3 minutes put into cache
    And entry with key 'will-be-removed' and value 'will-be-removed value' which expires in 1 minutes put into cache

  Scenario: 01. Refresh existing entry that should not be expired yet asynchronously
    Given time passed by 2 minutes
    When I refresh 'will-be-refreshed' cache entry asynchronously
    Then cache invalidation done
    And 'will-be-refreshed' entry is present in the object store bucket
    And 'will-be-refreshed' entry should be expired today at 00:05

  Scenario: 02. Refresh existing entry that should not be expired yet synchronously
    Given time passed by 2 minutes
    When I refresh 'will-be-refreshed' cache entry synchronously
    Then cache invalidation done
    And 'will-be-refreshed' entry is present in the object store bucket
    And 'will-be-refreshed' entry should be expired today at 00:05
    And 'will-be-removed' entry is not present in the object store bucket

  Scenario: 03. Refresh asynchronously missed entry should report an error
    When I refresh 'missing' cache entry asynchronously
    Then no errors are reported

  Scenario: 04. Refresh asynchronously missed entry should report an error
    When I refresh 'missing' cache entry synchronously
    Then no errors are reported

  Scenario: 05. Refresh asynchronously already deleted entry should report an error
    Given object with key 'will-be-removed' removed from object store bucket
    When I refresh 'will-be-removed' cache entry asynchronously
    Then no errors are reported

  Scenario: 06. Refresh synchronously already deleted entry should report an error
    Given object with key 'will-be-removed' removed from object store bucket
    When I refresh 'will-be-removed' cache entry synchronously
    Then no errors are reported
