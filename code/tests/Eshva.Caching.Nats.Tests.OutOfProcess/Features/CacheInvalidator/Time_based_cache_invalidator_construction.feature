Feature: Time-based cache invalidator construction

  Background:
    Given minimal expired entries purging interval is 2 minutes

  Scenario: 01. Can construct standard purger with purging interval greater than minimal purging interval
    When I construct standard purger with purging interval of 6 minutes
    Then no errors are reported

  Scenario: 02. Can construct standard purger with purging interval equals to minimal purging interval
    When I construct standard purger with purging interval of 2 minutes
    Then no errors are reported

  Scenario: 03. Should report an error if purging interval is less than minimal purging interval
    When I construct standard purger with purging interval of 1 minutes
    Then argument out of range exception should be reported
