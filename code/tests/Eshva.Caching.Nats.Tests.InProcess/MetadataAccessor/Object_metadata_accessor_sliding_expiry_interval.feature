Feature: Object metadata accessor sliding expiry interval

  Background:
    Given object metadata with key 'cache entry' and without metadata dictionary
    And object metadata accessor with defined arguments

  Scenario Template: 01. Can set sliding expiry interval to date and time value
    When I set sliding expiry interval of accessor to '<property-value>'
    Then metadata dictionary 'SlidingExpiryInterval' entry should be set to '<dictionary-value>'

    Examples:
      | property-value | dictionary-value |
      | 01:23:45       | 50250000000      |
      | 23:05:11       | 831110000000     |
      | 00:00:00       | 0                |
      | 10:01:23:45    | 8690250000000    |

  Scenario: 02. Should remove entry from metadata dictionary if absoulute expiry set to null
    Given metadata dictionary 'SlidingExpiryInterval' entry set to 'null'
    When I set sliding expiry interval of accessor to 'null'
    Then metadata dictionary should not contain 'SlidingExpiryInterval' entry

  Scenario Template: 03. Can get previously set expires at UTC
    Given metadata dictionary 'SlidingExpiryInterval' entry set to '<dictionary-value>'
    When I get sliding expiry interval of accessor
    Then gotten sliding expiry interval should be set to '<property-value>'

    Examples:
      | dictionary-value | property-value |
      | 50250000000      | 01:23:45       |
      | 831110000000     | 23:05:11       |
      | 0                | 00:00:00       |
      | 8690250000000    | 10:01:23:45    |
      | invalid value    | null           |

  Scenario: 04. Should get 'null' for sliding expiry interval if not previously set
    Given metadata dictionary 'SlidingExpiryInterval' entry missing
    When I get sliding expiry interval of accessor
    Then gotten sliding expiry interval should be set to 'null'
