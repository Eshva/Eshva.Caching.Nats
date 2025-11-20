Feature: Object store based datastore construction

  Scenario: 01. Should be possible construct object store based datastore with correct arguments
    Given object store bucket
    And expiry calculator
    When I construct object store based datastore with given arguments
    Then no errors are reported

  Scenario: 02. Should report error when construct object store based datastore without bucket
    Given expiry calculator
    When I construct object store based datastore with given arguments
    Then argument not specified error should be reported

  Scenario: 03. Should report error when construct object store based datastore without expiry calculator
    Given object store bucket
    When I construct object store based datastore with given arguments
    Then argument not specified error should be reported
