Feature: Time-based cache invalidation expired entries purging

  Background:
    Given time-based cache invalidation with purging interval of 6 minutes

  Scenario: 01. Purging should start on time
    Given time passed by 6 minutes
    When purging expired cache entries requested
    Then no errors are reported
    Then purging should be done

  Scenario: 02. Purging should not start if time has not yet come
    Given time passed by 5 minutes
    When purging expired cache entries requested
    Then no errors are reported
    Then purging should not start

  Scenario: 03. Only one out of a few concurrent purging should be executed
    Given time passed by 6 minutes
    When a few concurrent purging expired cache entries requested
    Then no errors are reported
    Then only one purging should be done
