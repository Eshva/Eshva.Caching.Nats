Feature: Time-based cache invalidation construction

  Background:
    Given minimal expired entries purging interval is 2 minutes

  Scenario: 01. Can construct time-based cache invalidation with purging interval greater than minimal purging interval
    When I construct time-based cache invalidation with purging interval of 6 minutes
    Then no errors are reported

  Scenario: 02. Can construct time-based cache invalidation with purging interval equals to minimal purging interval
    When I construct time-based cache invalidation with purging interval of 2 minutes
    Then no errors are reported

  Scenario: 03. Should report an error if purging interval is less than minimal purging interval
    When I construct time-based cache invalidation with purging interval of 1 minutes
    Then argument out of range exception should be reported
