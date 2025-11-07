Feature: Object metadata accessor construction

  Scenario: 01. Can construct object metadata accessor with NATS object metadata without assigned metadata dictionary
    Given object metadata with key 'cache entry' and without metadata dictionary
    When I construct object metadata accessor with defined arguments
    Then no errors are reported
    And object metadata of accessor equals one used in constructor
    And object metadata's metadata dictionary assigned

  Scenario: 02. Can construct object metadata accessor with NATS object metadata with assigned metadata dictionary
    Given object metadata with key 'cache entry' and with metadata dictionary
    When I construct object metadata accessor with defined arguments
    Then no errors are reported
    And object metadata of accessor equals one used in constructor
    And object metadata's metadata dictionary equals used in object metadata

  Scenario: 03. Should report an error if object metadata not specified
    Given object metadata not specified
    When I construct object metadata accessor with defined arguments
    Then argument not specified exception should be reported
