Feature: Standard expired cache entries purger construction

  Background:
    Given minimal expired entries purging interval is 5 minutes
    And default expired entries purging interval is 10 minutes

  Scenario: 01. Can construct standard purger with purging interval greater than minimal purging interval
    When I construct standard purger with purging interval of 6 minutes and clock set at today 20:00
    Then no errors are reported

  Scenario: 02. Can construct standard purger with purging interval equals to minimal purging interval
    When I construct standard purger with purging interval of 5 minutes and clock set at today 20:00
    Then no errors are reported

  Scenario: 03. Can not construct standard purger with purging interval less than minimal purging interval
    When I construct standard purger with purging interval of 4 minutes and clock set at today 20:00
    Then argument out of range exception should be reported

  Scenario: 04. Can construct standard purger without purging interval
    When I construct standard purger without purging interval
    Then purging interval should be set to default purging interval

