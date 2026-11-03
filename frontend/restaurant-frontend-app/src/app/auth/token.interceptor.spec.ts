import { HttpClient, HttpContext, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ToastService } from '../services/toast/toast.service';
import { AuthenticationService } from './services/authentication.service';
import { SKIP_ERROR_TOAST } from './skip-error-toast.context';
import { tokenInterceptor } from './token.interceptor';

const URL = 'https://test.api/reservations';

describe('tokenInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let toastService: jasmine.SpyObj<ToastService>;
  let authService: jasmine.SpyObj<AuthenticationService>;

  beforeEach(() => {
    toastService = jasmine.createSpyObj<ToastService>('ToastService', ['error']);
    authService = jasmine.createSpyObj<AuthenticationService>('AuthenticationService', [
      'getTokens',
    ]);
    authService.getTokens.and.returnValue(null);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([tokenInterceptor])),
        provideHttpClientTesting(),
        { provide: ToastService, useValue: toastService },
        { provide: AuthenticationService, useValue: authService },
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('shows the server message in a toast for a 400 response', () => {
    http.post(URL, {}).subscribe({ error: () => undefined });

    httpMock
      .expectOne(URL)
      .flush({ message: 'Overlaps' }, { status: 400, statusText: 'Bad Request' });

    expect(toastService.error).toHaveBeenCalledWith('Overlaps', 'Error');
  });

  it('does not show a toast but still rethrows when SKIP_ERROR_TOAST is set', () => {
    let receivedStatus: number | undefined;
    http
      .post(URL, {}, { context: new HttpContext().set(SKIP_ERROR_TOAST, true) })
      .subscribe({ error: (error) => (receivedStatus = error.status) });

    httpMock
      .expectOne(URL)
      .flush({ message: 'Overlaps' }, { status: 400, statusText: 'Bad Request' });

    expect(toastService.error).not.toHaveBeenCalled();
    expect(receivedStatus).toBe(400);
  });

  describe('guest routes', () => {
    const tokens = { idToken: 'owner-id-token' } as ReturnType<AuthenticationService['getTokens']>;

    it('sends the owner login token to the group sessions route', () => {
      authService.getTokens.and.returnValue(tokens);
      http.get('https://test.api/guest/groups/group-1/sessions').subscribe();

      const req = httpMock.expectOne('https://test.api/guest/groups/group-1/sessions');
      expect(req.request.headers.get('Authorization')).toBe('Bearer owner-id-token');
      req.flush({});
    });

    it('sends the owner login token to the invite lookup route', () => {
      authService.getTokens.and.returnValue(tokens);
      http.get('https://test.api/guest/invites/abc').subscribe();

      const req = httpMock.expectOne('https://test.api/guest/invites/abc');
      expect(req.request.headers.get('Authorization')).toBe('Bearer owner-id-token');
      req.flush({});
    });

    it('does not touch other guest routes', () => {
      authService.getTokens.and.returnValue(tokens);
      http.post('https://test.api/guest/sessions', {}).subscribe();

      const req = httpMock.expectOne('https://test.api/guest/sessions');
      expect(req.request.headers.has('Authorization')).toBeFalse();
      req.flush({});
    });
  });
});
