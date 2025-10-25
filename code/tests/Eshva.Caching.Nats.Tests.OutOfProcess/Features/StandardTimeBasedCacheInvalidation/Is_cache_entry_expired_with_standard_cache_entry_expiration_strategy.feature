Feature: Is cache entry expired with standard time-based cache invalidation

  Background:
    Given standard cache entry expiration strategy with clock set at today 20:00 and default sliding expiration time 1 minutes

  Scenario: 01. Cache entry which expires later than current time should not be reported as not expired
    Given cache entry that expires today at 20:00:01
    When I check is cache entry expired
    Then it should be not expired

  Scenario: 02. Cache entry which expires at current time should be reported as expired
    Given cache entry that expires today at 20:00:00
    When I check is cache entry expired
    Then it should be expired

  Scenario: 03. Cache entry which expires earlier than current time should be reported as expired
    Given cache entry that expires today at 19:59:59
    When I check is cache entry expired
    Then it should be expired
