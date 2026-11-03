import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable, switchMap } from 'rxjs';

import { SKIP_ERROR_TOAST } from '@app/auth/skip-error-toast.context';
import { GroupSessionsResponse } from '@app/models/group-orders/group-sessions-response.model';
import { GroupStatusResponse } from '@app/models/group-orders/group-status.model';
import { OwnerSessionResponse } from '@app/models/group-orders/owner-session.model';
import { SubmitGroupPreOrderRequest, SubmittedDish } from '@app/models/group-orders/submit-group-pre-order.model';
import { GuestInvite } from '@app/models/reservations/guest-invite.model';
import { GuestInviteService } from '@app/services/reservation/guest-invite.service';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';

export interface GroupRequestOptions {
  /** Background requests must not show an error toast. */
  silent?: boolean;
}

@Injectable({ providedIn: 'root' })
export class GroupOrdersService {
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly http = inject(HttpClient);
  private readonly guestInviteService = inject(GuestInviteService);

  /** reservationId → invite token → invite (it carries GroupReservationId and the header data). */
  getGroupInvite$(reservationId: string): Observable<GuestInvite> {
    return this.guestInviteService
      .getInviteToken$(reservationId)
      .pipe(switchMap((invite) => this.guestInviteService.getInvite$(invite.token)));
  }

  getSessions$(groupReservationId: string, options?: GroupRequestOptions): Observable<GroupSessionsResponse> {
    return this.http.get<GroupSessionsResponse>(`${this.groupUrl(groupReservationId)}/sessions`, {
      context: this.context(options),
    });
  }

  getStatus$(groupReservationId: string, options?: GroupRequestOptions): Observable<GroupStatusResponse> {
    return this.http.get<GroupStatusResponse>(`${this.groupUrl(groupReservationId)}/status`, {
      context: this.context(options),
    });
  }

  getOwnerSessionId$(reservationId: string): Observable<string> {
    return this.http
      .get<OwnerSessionResponse>(`${this.baseUrl}/reservations/${reservationId}/guest-session-id`)
      .pipe(map((response) => response.guestSessionId));
  }

  submit$(groupReservationId: string, reservationId: string, comment?: string): Observable<SubmittedDish[]> {
    const body: SubmitGroupPreOrderRequest = comment ? { reservationId, comment } : { reservationId };
    return this.http.post<SubmittedDish[]>(
      `${this.baseUrl}/group-reservations/${groupReservationId}/submit`,
      body
    );
  }

  removeSession$(groupReservationId: string, guestSessionId: string): Observable<GroupSessionsResponse> {
    return this.http.delete<GroupSessionsResponse>(
      `${this.groupUrl(groupReservationId)}/sessions/${guestSessionId}`
    );
  }

  private groupUrl(groupReservationId: string): string {
    return `${this.baseUrl}/guest/groups/${groupReservationId}`;
  }

  private context(options?: GroupRequestOptions): HttpContext {
    return new HttpContext().set(SKIP_ERROR_TOAST, options?.silent === true);
  }
}
