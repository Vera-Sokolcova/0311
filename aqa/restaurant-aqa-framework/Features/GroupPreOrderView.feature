@group-preorder
Feature: Viewing the participants of a group pre-order

  The owner of a booking invites guests; every guest fills their own cart. The owner sees who joined,
  what each of them chose and what it costs, and nobody else gets to see that list.

  Background:
    Given Olena has booked a table for a group

  Rule: The owner sees every participant with their dishes and totals

    @api @l1
    Scenario: The owner sees each guest with their dishes and total
      Given Ivan has joined the group of Olena
      And Ivan has put 2 portions of the first dish into the cart
      And Maria has joined the group of Olena
      And Maria has put 1 portion of the second dish into the cart
      When Olena opens the group pre-order
      Then the group has 2 participants
      And Ivan is listed with 2 portions of the first dish
      And the total of Ivan is the price of 2 portions of the first dish
      And Maria is listed with 1 portion of the second dish
      And the total of Maria is the price of 1 portion of the second dish
      And the group total is the sum of the totals of the participants

    @api @l1
    Scenario: The group total follows a guest changing their cart
      Given Ivan has joined the group of Olena
      And Ivan has put 2 portions of the first dish into the cart
      When Ivan takes 1 portion of the first dish out of the cart
      And Olena opens the group pre-order
      Then the group total is the price of 1 portion of the first dish

    @api @l1
    Scenario: A guest who has not confirmed the cart is marked as still choosing
      Given Ivan has joined the group of Olena
      And Ivan has put 1 portion of the first dish into the cart
      And Maria has joined the group of Olena
      And Maria has put 1 portion of the second dish into the cart
      And Maria has confirmed the cart
      When Olena opens the group pre-order
      Then Ivan is listed as still choosing
      And Maria is listed as having confirmed the cart

    @api @l1
    Scenario: A guest without dishes is still listed
      Given Ivan has joined the group of Olena
      When Olena opens the group pre-order
      Then Ivan is listed with no dishes

    @api @l1
    Scenario: A group nobody has joined yet is empty
      When Olena opens the group pre-order
      Then nobody is listed and the group total is zero

    @api @l1
    Scenario: The owner's own seat comes first in the list
      Given Ivan has joined the group of Olena
      And Olena has taken a seat in the group
      When Olena opens the group pre-order
      Then the group has 2 participants
      And the owner comes first in the list

  Rule: Only the owner of the group may see the participants

    @api @l1 @negative
    Scenario: Another customer cannot see the group
      Given Dmytro is a customer
      When Dmytro opens the group pre-order of Olena
      Then the request is refused because only the owner may do it

    @api @l1 @negative
    Scenario: A guest cannot see the group
      Given Ivan has joined the group of Olena
      When Ivan opens the group pre-order of Olena
      Then the request is refused because a guest has no account

    @api @l1 @negative
    Scenario: The waiter cannot see the group
      When the waiter opens the group pre-order of Olena
      Then the request is refused because only the owner may do it

    @api @l1 @negative
    Scenario: Someone who is not signed in cannot see the group
      When someone who is not signed in opens the group pre-order of Olena
      Then the request is refused because nobody is signed in

    @api @l1 @negative
    Scenario: A group that does not exist cannot be opened
      When Olena opens a group pre-order that does not exist
      Then the group cannot be found
