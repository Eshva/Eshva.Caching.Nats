Feature: Key/value based datastore construction

  Scenario: 01. Should be possible construct key/value based datastore with correct arguments
    Given key-value bucket
    And cache entry expiry serializer
    And expiry calculator
    When I construct key-value based datastore with given arguments
    Then no errors are reported

  Scenario: 02. Should report error when construct key/value based datastore without bucket
    Given cache entry expiry serializer
    And expiry calculator
    When I construct key-value based datastore with given arguments
    Then argument not specified error should be reported

  Scenario: 03. Should report error when construct key/value based datastore without serializer
    Given key-value bucket
    And expiry calculator
    When I construct key-value based datastore with given arguments
    Then argument not specified error should be reported

  Scenario: 04. Should report error when construct key/value based datastore without expiry calculator
    Given key-value bucket
    And cache entry expiry serializer
    When I construct key-value based datastore with given arguments
    Then argument not specified error should be reported
