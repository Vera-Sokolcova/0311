/** POST /group-reservations/{groupReservationId}/submit — request body */
export interface SubmitGroupPreOrderRequest {
  reservationId: string;
  /** Optional note for the restaurant, up to 300 characters. */
  comment?: string;
}

/** POST /group-reservations/{groupReservationId}/submit — response item */
export interface SubmittedDish {
  dishId: string;
  quantity: number;
}
