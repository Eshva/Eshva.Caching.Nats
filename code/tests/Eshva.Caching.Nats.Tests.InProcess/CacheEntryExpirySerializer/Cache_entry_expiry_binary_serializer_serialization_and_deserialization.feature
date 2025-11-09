Feature: Cache entry expiry binary serializer serialization and deserialization

  Background:
    Given cache entry expiry binary serializer

  Scenario Template: 01. Serialize and deserialize
    Given cache entry expiry with '<expires-at-utc>', '<absolute-expiry-at-utc>', '<sliding-expiry-interval>'
    When I serialize cache entry expiry with binary serializer
    Then deserialized cache entry expiry should have '<expires-at-utc>', '<absolute-expiry-at-utc>', '<sliding-expiry-interval>'

    Examples:
      | expires-at-utc      | absolute-expiry-at-utc | sliding-expiry-interval |
      | 07.11.2025 01:23:45 | 01.01.1974 00:00:00    | 01:23:45                |
      | 01.01.1974 01:23:45 | 07.11.2025 01:23:45    | 23:05:11                |
      | 01.01.1974 00:00:00 | 01.01.1974 01:23:45    | 10:01:23:45             |
      | 07.11.2025 01:23:45 | null                   | 10:01:23:45             |
      | 07.11.2025 01:23:45 | 01.01.1974 01:23:45    | null                    |
      | 07.11.2025 01:23:45 | null                   | null                    |
