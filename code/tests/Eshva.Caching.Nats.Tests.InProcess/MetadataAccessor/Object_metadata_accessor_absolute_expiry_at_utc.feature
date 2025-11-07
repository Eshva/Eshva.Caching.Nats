Feature: Object metadata accessor absolute expiry at UTC

  Background:
    Given object metadata with key 'cache entry' and without metadata dictionary
    And object metadata accessor with defined arguments

  Scenario Template: 01. Can set absolute expiry at UTC to date and time value
    When I set absolute expiry at UTC of accessor to '<property-value>'
    Then metadata dictionary 'AbsoluteExpiryAtUtc' entry should be set to '<dictionary-value>'

    Examples:
      | property-value      | dictionary-value   |
      | 07.11.2025 01:23:45 | 638980754250000000 |
      | 01.01.1974 01:23:45 | 622618322250000000 |
      | 01.01.1974 00:00:00 | 622618272000000000 |

  Scenario: 02. Should remove entry from metadata dictionary if absoulute expiry set to null
    Given metadata dictionary 'AbsoluteExpiryAtUtc' entry set to 'null'
    When I set absolute expiry at UTC of accessor to 'null'
    Then metadata dictionary should not contain 'AbsoluteExpiryAtUtc' entry

  Scenario Template: 03. Can get previously set expires at UTC
    Given metadata dictionary 'AbsoluteExpiryAtUtc' entry set to '<dictionary-value>'
    When I get absolute expiry at UTC of accessor
    Then gotten absolute expiry at UTC should be set to '<property-value>'

    Examples:
      | dictionary-value   | property-value      |
      | 638980754250000000 | 07.11.2025 01:23:45 |
      | 622618322250000000 | 01.01.1974 01:23:45 |
      | 622618272000000000 | 01.01.1974 00:00:00 |
      | invalid value      | null                |

  Scenario: 04. Should get 'null' for absolute expiry at UTC if not previously set
    Given metadata dictionary 'AbsoluteExpiryAtUtc' entry missing
    When I get absolute expiry at UTC of accessor
    Then gotten absolute expiry at UTC should be set to 'null'
