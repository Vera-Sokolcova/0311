import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { catchError, EMPTY, exhaustMap, filter, forkJoin, interval } from 'rxjs';

import { ParticipantCardComponent } from '@app/components/group-orders/participant-card/participant-card.component';
import { GroupSessionsResponse } from '@app/models/group-orders/group-sessions-response.model';
import { GroupReservationStatus } from '@app/models/group-orders/group-status.model';
import { Participant } from '@app/models/group-orders/participant.model';
import { GuestInvite } from '@app/models/reservations/guest-invite.model';
import { GroupOrdersService } from '@app/services/group-orders/group-orders.service';
import { ToastService } from '@app/services/toast/toast.service';
import { ModalComponent } from '@app/shared/custom-components/modal/modal.component';
import { consolidatePreOrder } from '@app/shared/helpers/consolidate-pre-order';

export type PageState = 'loading' | 'ready' | 'not-found' | 'error';
export const GROUP_SYNC_INTERVAL_MS = 15_000;

@Component({
  selector: 'app-group-order-page',
  imports: [ParticipantCardComponent, ModalComponent, CurrencyPipe, DatePipe],
  templateUrl: './group-order-page.component.html',
  styleUrl: './group-order-page.component.scss',
  changeDetection: ChangeDetectionStrategy.Eager,
})
export class GroupOrderPageComponent implements OnInit {
  private readonly groupOrdersService = inject(GroupOrdersService);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reservationId = inject(ActivatedRoute).snapshot.paramMap.get('reservationId') ?? '';

  readonly state = signal<PageState>('loading');
  readonly invite = signal<GuestInvite | null>(null);
  readonly sessions = signal<GroupSessionsResponse>({ participants: [], total: 0 });
  readonly ownerSessionId = signal('');
  readonly groupStatus = signal<GroupReservationStatus>('Open');

  readonly lastUpdated = signal<Date | null>(null);
  readonly syncFailed = signal(false);

  readonly submitDialogOpen = signal(false);
  readonly submitting = signal(false);
  readonly comment = signal('');
  readonly maxCommentLength = 300;
  readonly participantToRemove = signal<Participant | null>(null);
  readonly removing = signal(false);

  private groupReservationId = '';
  private syncStarted = false;

  readonly participants = computed(() => this.sessions().participants);
  readonly isSent = computed(() => this.groupStatus() === 'Submitted');

  readonly consolidated = computed(() => consolidatePreOrder(this.participants()));
  readonly lines = computed(() => this.consolidated().items);
  /** Grand Total is the consolidated order (submitted carts only), not `total` from the API. */
  readonly grandTotal = computed(() => this.consolidated().total);

  readonly notSubmittedCount = computed(
    () => this.participants().filter((person) => !person.isSubmitted).length
  );
  readonly hasNoDishes = computed(() =>
    this.participants().every((person) => person.items.length === 0)
  );
  readonly canSubmit = computed(
    () => this.lines().length > 0 && !this.submitting() && !this.isSent()
  );
  readonly canRemove = computed(() => !this.isSent());

  readonly statusLabel = computed(() => {
    const labels: Record<GroupReservationStatus, string> = {
      Open: 'Collecting pre-orders',
      Submitted: 'Sent to Restaurant',
      Closed: 'Closed',
    };

    return labels[this.groupStatus()];
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.state.set('loading');

    this.groupOrdersService
      .getGroupInvite$(this.reservationId)
      .pipe(
        exhaustMap((invite) => {
          this.invite.set(invite);
          this.groupReservationId = invite.GroupReservationId;

          return forkJoin({
            sessions: this.groupOrdersService.getSessions$(this.groupReservationId),
            ownerSessionId: this.groupOrdersService.getOwnerSessionId$(this.reservationId),
            status: this.groupOrdersService.getStatus$(this.groupReservationId),
          });
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: ({ sessions, ownerSessionId, status }) => {
          this.sessions.set(sessions);
          this.ownerSessionId.set(ownerSessionId);
          this.groupStatus.set(status.status);
          this.markFresh();
          this.state.set('ready');
          this.startSync();
        },
        error: (error: HttpErrorResponse) => {
          this.state.set(error.status === 404 ? 'not-found' : 'error');
        },
      });
  }

  /** "Refresh" button and the 15 s timer share the same silent request. */
  refresh(): void {
    if (!this.groupReservationId) {
      return;
    }

    forkJoin({
      sessions: this.groupOrdersService.getSessions$(this.groupReservationId, { silent: true }),
      status: this.groupOrdersService.getStatus$(this.groupReservationId, { silent: true }),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ sessions, status }) => this.applySync(sessions, status.status),
        error: () => this.syncFailed.set(true),
      });
  }

  isMe(person: Participant): boolean {
    return person.id === this.ownerSessionId();
  }

  openSubmitDialog(): void {
    if (this.canSubmit()) {
      this.submitDialogOpen.set(true);
    }
  }

  onCommentInput(event: Event): void {
    this.comment.set((event.target as HTMLTextAreaElement).value);
  }

  closeSubmitDialog(): void {
    if (!this.submitting()) {
      this.submitDialogOpen.set(false);
    }
  }

  confirmSubmit(): void {
    if (this.submitting()) {
      return;
    }

    this.submitting.set(true);
    this.groupOrdersService
      .submit$(this.groupReservationId, this.reservationId, this.comment().trim() || undefined)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.submitDialogOpen.set(false);
          this.groupStatus.set('Submitted');
          this.toastService.success('Pre-order sent to the restaurant');
        },
        // the error toast comes from tokenInterceptor; the page stays usable
        error: () => {
          this.submitting.set(false);
          this.submitDialogOpen.set(false);
        },
      });
  }

  askRemove(person: Participant): void {
    this.participantToRemove.set(person);
  }

  cancelRemove(): void {
    if (!this.removing()) {
      this.participantToRemove.set(null);
    }
  }

  /** Removing a guest never changes the owner's cart: keep it if the response carries a shorter one. */
  private keepOwnerCart(snapshot: GroupSessionsResponse): GroupSessionsResponse {
    const previous = this.sessions().participants.find((person) => person.type === 'Owner');
    const current = snapshot.participants.find((person) => person.type === 'Owner');

    if (!previous || !current || previous.items.length <= current.items.length) {
      return snapshot;
    }

    return {
      participants: snapshot.participants.map((person) => (person.type === 'Owner' ? previous : person)),
      total: snapshot.total + previous.total - current.total,
    };
  }

  confirmRemove(): void {
    const person = this.participantToRemove();

    if (!person || this.removing()) {
      return;
    }

    this.removing.set(true);
    this.groupOrdersService
      .removeSession$(this.groupReservationId, person.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (snapshot) => {
          this.removing.set(false);
          this.participantToRemove.set(null);
          this.sessions.set(this.keepOwnerCart(snapshot));
        },
        error: () => {
          this.removing.set(false);
          this.participantToRemove.set(null);
        },
      });
  }

  private startSync(): void {
    // "Try again" calls load() again: never start a second timer
    if (this.syncStarted) {
      return;
    }

    this.syncStarted = true;

    interval(GROUP_SYNC_INTERVAL_MS)
      .pipe(
        filter(() => this.groupStatus() === 'Open'),
        // a new request does not start while the previous one is still pending
        exhaustMap(() =>
          forkJoin({
            sessions: this.groupOrdersService.getSessions$(this.groupReservationId, { silent: true }),
            status: this.groupOrdersService.getStatus$(this.groupReservationId, { silent: true }),
          }).pipe(
            catchError(() => {
              this.syncFailed.set(true);
              return EMPTY;
            })
          )
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(({ sessions, status }) => this.applySync(sessions, status.status));
  }

  private applySync(sessions: GroupSessionsResponse, status: GroupReservationStatus): void {
    this.sessions.set(sessions);
    this.groupStatus.set(status);
    this.markFresh();
  }

  private markFresh(): void {
    this.lastUpdated.set(new Date());
    this.syncFailed.set(false);
  }
}
