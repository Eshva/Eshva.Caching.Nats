Feature: Calculate expiration with standard cache entry expiration strategy

  Background:
    Given standard cache entry expiration strategy with clock set at today 20:00 and default sliding expiration time 1 minutes

  Scenario: 01. Given only absolute expiration on expiration calculation it should return absolute expriation
    Given absolute expiration today at 21:00
    And no sliding expiration
    When I calculate expiration time
    Then it should be today at 21:00

  Scenario: 02. Given only sliding expiration on expiration calculation it should advance current time with sliding expriation
    Given no absolute expiration
    And sliding expiration in 10 minutes
    And time passed by 5 minutes
    When I calculate expiration time
    Then it should be today at 20:15

  Scenario: 03. Given no expirations on expiration calculation it should advance current time with default sliding expriation
    Given no absolute expiration
    And no sliding expiration
    And time passed by 5 minutes
    When I calculate expiration time
    Then it should be today at 20:06

  Scenario: 04. Given both expirations and absolute expiration in future of sliding expiration on expiration calculation it should advance current time with sliding expriation
    Given absolute expiration today at 21:00
    And sliding expiration in 40 minutes
    And time passed by 19 minutes
    When I calculate expiration time
    Then it should be today at 20:59

  Scenario: 05. Given both expirations and absolute expiration in past of sliding expiration on expiration calculation it should return absolute expriation
    Given absolute expiration today at 21:00
    And sliding expiration in 40 minutes
    And time passed by 21 minutes
    When I calculate expiration time
    Then it should be today at 21:00
