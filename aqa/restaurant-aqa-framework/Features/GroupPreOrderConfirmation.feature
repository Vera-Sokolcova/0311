@group-preorder
Feature: Confirming the consolidated group pre-order

  After looking at what everybody chose, the owner confirms one consolidated pre-order that goes to the
  restaurant. Only carts the guests have confirmed count; the same dish from several guests is added up.

  Background:
    Given Olena has booked a table for a group
    And Olena has pre-ordered 1 portion of the first dish

  Rule: The confirmed carts become one order for the restaurant

    @api @l2
    Scenario: Same dishes from different guests are added up and unconfirmed carts are left out
      Given Ivan has joined the group of Olena
      And Ivan has put 2 portions of the first dish into the cart
      And Ivan has confirmed the cart
      And Maria has joined the group of Olena
      And Maria has put 1 portion of the first dish into the cart
      And Maria has put 1 portion of the second dish into the cart
      And Maria has confirmed the cart
      And Petro has joined the group of Olena
      And Petro has put 3 portions of the third dish into the cart
      When Olena confirms the group pre-order
      Then the pre-order is sent to the restaurant
      And the consolidated pre-order consists of
        | dish        | portions |
        | first dish  | 3        |
        | second dish | 1        |

    @api @l2
    Scenario: The waiter sees the confirmed order made up as the owner confirmed it
      Given Ivan has joined the group of Olena
      And Ivan has put 2 portions of the second dish into the cart
      And Ivan has confirmed the cart
      And Maria has joined the group of Olena
      And Maria has put 1 portion of the third dish into the cart
      And Maria has confirmed the cart
      And Olena has confirmed the group pre-order
      When the waiter opens the pre-order of Olena
      Then the waiter sees the order as confirmed by the customer
      And the waiter sees these dishes in the order
        | dish        | portions |
        | first dish  | 1        |
        | second dish | 2        |
        | third dish  | 1        |

  Rule: A pre-order without confirmed dishes cannot be confirmed

    @api @l2 @negative
    Scenario: No cart has been confirmed
      Given Ivan has joined the group of Olena
      And Ivan has put 1 portion of the first dish into the cart
      When Olena confirms the group pre-order
      Then the pre-order is refused with "There are no submitted guest sessions in the group."

    @api @l2 @negative
    Scenario: Nobody has joined
      When Olena confirms the group pre-order
      Then the pre-order is refused with "There are no submitted guest sessions in the group."

  Rule: Only the owner may confirm, and only once

    @api @l2 @negative
    Scenario: A group that was already confirmed cannot be confirmed again
      Given Ivan has joined the group of Olena
      And Ivan has put 1 portion of the second dish into the cart
      And Ivan has confirmed the cart
      And Olena has confirmed the group pre-order
      When Olena confirms the group pre-order
      Then the pre-order is refused because it was already sent

    @api @l2 @negative
    Scenario: Another customer cannot confirm the group
      Given Ivan has joined the group of Olena
      And Ivan has put 1 portion of the second dish into the cart
      And Ivan has confirmed the cart
      And Dmytro is a customer
      When Dmytro confirms the group pre-order of Olena
      Then the request is refused because only the owner may do it

    @api @l2 @negative
    Scenario: A guest cannot confirm the group
      Given Ivan has joined the group of Olena
      And Ivan has put 1 portion of the second dish into the cart
      And Ivan has confirmed the cart
      When Ivan confirms the group pre-order of Olena
      Then the request is refused because a guest has no account

    @api @l2 @negative
    Scenario: The waiter cannot confirm the group
      When the waiter confirms the group pre-order of Olena
      Then the request is refused because only the owner may do it

    @api @l2 @negative
    Scenario: Someone who is not signed in cannot confirm the group
      When someone who is not signed in confirms the group pre-order of Olena
      Then the request is refused because nobody is signed in

    @api @l2 @negative
    Scenario: A group that does not exist cannot be confirmed
      When Olena confirms a group pre-order that does not exist
      Then the group cannot be found
