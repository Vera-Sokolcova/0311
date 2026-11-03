@group-preorder
Feature: The group pre-order page

  The owner works with the group pre-order on the website: sees who joined, confirms the pre-order
  and removes a guest.

  Background:
    Given Olena has booked a table for a group
    And Olena has pre-ordered 1 portion of the first dish
    And Ivan has joined the group of Olena
    And Ivan has put 2 portions of the second dish into the cart
    And Ivan has confirmed the cart
    And Olena has signed in on the website

  @ui @l1
  Scenario: The owner sees the participants and the totals
    When Olena opens the group order page
    Then the page lists Ivan with a total of 2 portions of the second dish
    And the page shows Ivan as having confirmed the cart

  @ui @l2
  Scenario: The owner confirms the pre-order and the waiter sees it
    When Olena opens the group order page
    And the owner submits the pre-order on the page
    Then the page says the pre-order was sent to the restaurant
    When the waiter opens the pre-order of Olena
    Then the waiter sees these dishes in the order
      | dish        | portions |
      | first dish  | 1        |
      | second dish | 2        |

  @ui @l3
  Scenario: The owner removes a guest
    When Olena opens the group order page
    And the owner removes Ivan on the page
    Then the page no longer lists Ivan
