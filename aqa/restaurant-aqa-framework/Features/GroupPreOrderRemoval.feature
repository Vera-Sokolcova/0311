@group-preorder
Feature: Removing a participant from a group pre-order

  The owner can remove a guest, and their dishes with them. Afterwards the list and the total are
  recalculated for the guests who remain. Only the owner of that very group may do it.

  Background:
    Given Olena has booked a table for a group
    And Olena has pre-ordered 1 portion of the first dish

  Rule: The owner can remove a participant of their group

    @api @l3
    Scenario: Removing one of several guests recalculates the total
      Given Ivan has joined the group of Olena
      And Ivan has put 2 portions of the first dish into the cart
      And Maria has joined the group of Olena
      And Maria has put 1 portion of the second dish into the cart
      When Olena removes Ivan from the group
      Then the participant is removed
      And Ivan is not in the group any more
      And the group has 1 participant
      And the group total is the price of 1 portion of the second dish

    @api @l3
    Scenario: A guest who confirmed the cart can be removed and no longer counts
      Given Ivan has joined the group of Olena
      And Ivan has put 1 portion of the second dish into the cart
      And Ivan has confirmed the cart
      When Olena removes Ivan from the group
      And Olena confirms the group pre-order
      Then the pre-order is refused with "There are no submitted guest sessions in the group."

    @api @l3
    Scenario: Removing the last guest leaves an empty group
      Given Ivan has joined the group of Olena
      And Ivan has put 1 portion of the second dish into the cart
      When Olena removes Ivan from the group
      Then nobody is listed and the group total is zero

  Rule: Only a participant of this group can be removed

    @api @l3 @negative
    Scenario: A guest of another group cannot be removed
      Given Dmytro has booked a table for a group
      And Ivan has joined the group of Dmytro
      When Olena removes Ivan from the group of Olena
      Then the participant is refused because they belong to another group

    @api @l3 @negative
    Scenario: A participant who does not exist cannot be removed
      When Olena removes somebody who never joined from the group
      Then the participant cannot be found

  Rule: Only the owner may remove a participant

    @api @l3 @negative
    Scenario: Another customer cannot remove a guest
      Given Ivan has joined the group of Olena
      And Dmytro is a customer
      When Dmytro removes Ivan from the group of Olena
      Then the request is refused because only the owner may do it

    @api @l3 @negative
    Scenario: A guest cannot remove another guest
      Given Ivan has joined the group of Olena
      And Maria has joined the group of Olena
      When Ivan removes Maria from the group of Olena
      Then the request is refused because a guest has no account

    @api @l3 @negative
    Scenario: The waiter cannot remove a guest
      Given Ivan has joined the group of Olena
      When the waiter removes Ivan from the group of Olena
      Then the request is refused because only the owner may do it

    @api @l3 @negative
    Scenario: Someone who is not signed in cannot remove a guest
      Given Ivan has joined the group of Olena
      When someone who is not signed in removes Ivan from the group of Olena
      Then the request is refused because nobody is signed in

  Rule: A whole group pre-order works from start to finish

    @api @l3 @e2e
    Scenario: The waiter sees the order without the participant the owner removed
      Given Ivan has joined the group of Olena
      And Ivan has put 2 portions of the first dish into the cart
      And Ivan has confirmed the cart
      And Maria has joined the group of Olena
      And Maria has put 1 portion of the second dish into the cart
      And Maria has confirmed the cart
      And Petro has joined the group of Olena
      And Petro has put 1 portion of the third dish into the cart
      And Petro has confirmed the cart
      When Olena opens the group pre-order
      Then the group has 3 participants
      When Olena removes Ivan from the group
      And Olena confirms the group pre-order
      And the waiter opens the pre-order of Olena
      Then the waiter sees the order as confirmed by the customer
      And the waiter sees these dishes in the order
        | dish        | portions |
        | first dish  | 1        |
        | second dish | 1        |
        | third dish  | 1        |
