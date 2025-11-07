Feature: Object metadata accessor expires at UTC accessing

  Background:
    Given object metadata with key 'cache entry' and without metadata dictionary
    And object metadata accessor with defined arguments

  Scenario Template: 01. Can set expires at UTC to date and time value
    When I set expires at UTC of accessor to '<property-value>'
    Then metadata dictionary 'ExpiresAtUtc' entry should be set to '<dictionary-value>'

    Examples:
      | property-value      | dictionary-value   |
      | 07.11.2025 01:23:45 | 638980754250000000 |
      | 01.01.1974 01:23:45 | 622618322250000000 |
      | 01.01.1974 00:00:00 | 622618272000000000 |

  Scenario Template: 02. Can get previously set expires at UTC
    Given metadata dictionary 'ExpiresAtUtc' entry set to '<dictionary-value>'
    When I get expires at UTC of accessor
    Then gotten expires at UTC should be set to '<property-value>'

    Examples:
      | dictionary-value   | property-value      |
      | 638980754250000000 | 07.11.2025 01:23:45 |
      | 622618322250000000 | 01.01.1974 01:23:45 |
      | 622618272000000000 | 01.01.1974 00:00:00 |
      | invalid value      | never expires       |

  Scenario: 03. Should get 'never expires' for expires at UTC if not previously set
    Given metadata dictionary 'ExpiresAtUtc' entry missing
    When I get expires at UTC of accessor
    Then gotten expires at UTC should be set to 'never expires'
