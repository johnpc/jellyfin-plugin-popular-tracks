Feature: Corrected artist "Popular" ordering
  As a Jellyfin listener whose server has no meaningful local play counts
  I want an artist's "Popular" tracks ordered by real listening popularity
  So that the top songs reflect what the world actually plays, not garbage PlayCount

  Scenario: Owned tracks are re-ordered by Last.fm popularity
    Given the library has these Radiohead tracks in this order:
      | title        |
      | Deep Cut     |
      | Creep        |
      | Karma Police |
    And Last.fm ranks Radiohead's top tracks as:
      | title        |
      | Karma Police |
      | Creep        |
    When the Popular list is built
    Then the tracks appear in this order:
      | title        |
      | Karma Police |
      | Creep        |
      | Deep Cut     |

  Scenario: Tracks unknown to Last.fm fall to the bottom keeping their original order
    Given the library has these Radiohead tracks in this order:
      | title      |
      | B-side One |
      | Hit Single |
      | B-side Two |
    And Last.fm ranks Radiohead's top tracks as:
      | title      |
      | Hit Single |
    When the Popular list is built
    Then the tracks appear in this order:
      | title      |
      | Hit Single |
      | B-side One |
      | B-side Two |
