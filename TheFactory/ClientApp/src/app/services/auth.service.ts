import { Inject, Injectable, PLATFORM_ID } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { isPlatformBrowser } from '@angular/common';
import { environment } from '../../environments/environment';

interface GoogleCredentialResponse {
  credential: string;
}

interface GoogleIdentityServices {
  initialize(options: {
    client_id: string;
    callback: (response: GoogleCredentialResponse) => void;
    ux_mode: 'popup';
    auto_select: boolean;
  }): void;
  renderButton(
    parent: HTMLElement,
    options: { theme: 'outline'; size: 'large'; width: number; text: 'signin_with' | 'signup_with' }
  ): void;
}

declare global {
  interface Window {
    google?: {
      accounts: {
        id: GoogleIdentityServices;
      };
    };
  }
}

export interface ResetPasswordPayload {
  email: string;
  token: string;
  newPassword: string;
}

export interface PasswordResetResponse {
  message: string;
}

export interface UserLoginResponse {
  userName: string;
  userRole: string;
  userId: number;
  token: string;
  operatorId?: number;
  cellPhoneNo?: string | null;
}

export interface GoogleNewUserResponse {
  isNewUser: true;
  email: string;
  firstName: string;
  lastName: string;
  pictureUrl?: string | null;
}

export type GoogleLoginResponse = UserLoginResponse | GoogleNewUserResponse;

export interface GoogleRegistrationDetails {
  operatorId: number;
  cellPhoneNo: string;
  alternateCellPhoneNo: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly baseUrl = `${environment.apiBaseUrl}/api/auth`;
  private googleScriptPromise?: Promise<GoogleIdentityServices>;
  private googleInitialized = false;
  private googleCredentialHandler?: (credential: string) => void;

  constructor(
    private readonly http: HttpClient,
    @Inject(PLATFORM_ID) private readonly platformId: object
  ) {}

  initializeGoogleSignIn(
    buttonContainer: HTMLElement,
    onCredential: (credential: string) => void,
    buttonText: 'signin_with' | 'signup_with' = 'signin_with'
  ): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) {
      return Promise.resolve();
    }

    if (!environment.googleClientId) {
      return Promise.reject(new Error('Google sign-in is not configured for this environment.'));
    }

    this.googleCredentialHandler = onCredential;
    return this.loadGoogleIdentityServices().then((google) => {
      if (!this.googleInitialized) {
        google.initialize({
          client_id: environment.googleClientId,
          callback: (response) => {
            if (!response.credential) {
              throw new Error('Google did not return an identity credential.');
            }
            this.googleCredentialHandler?.(response.credential);
          },
          ux_mode: 'popup',
          auto_select: false
        });
        this.googleInitialized = true;
      }
      buttonContainer.replaceChildren();
      google.renderButton(buttonContainer, {
        theme: 'outline',
        size: 'large',
        width: Math.max(200, Math.floor(buttonContainer.getBoundingClientRect().width)),
        text: buttonText
      });
    });
  }

  loginWithGoogle(credential: string): Observable<GoogleLoginResponse> {
    return this.http.post<GoogleLoginResponse>(
      `${environment.apiBaseUrl}/api/User/GoogleLogin`,
      { credential }
    );
  }

  completeGoogleRegistration(
    credential: string,
    registration: GoogleRegistrationDetails
  ): Observable<UserLoginResponse> {
    return this.http.post<UserLoginResponse>(
      `${environment.apiBaseUrl}/api/User/CompleteGoogleRegistration`,
      { credential, ...registration }
    );
  }

  completeGoogleProfile(credential: string, cellPhoneNo: string): Observable<{ cellPhoneNo: string }> {
    return this.http.post<{ cellPhoneNo: string }>(
      `${environment.apiBaseUrl}/api/User/CompleteGoogleProfile`,
      { credential, cellPhoneNo }
    ).pipe(
      catchError((error: HttpErrorResponse) => {
        console.error('Google profile completion request failed.', {
          status: error.status,
          response: error.error,
          message: error.message
        });
        return throwError(() => error);
      })
    );
  }

  logout(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    localStorage.removeItem('authToken');
    localStorage.removeItem('userName');
    localStorage.removeItem('userRole');
    localStorage.removeItem('userId');
    localStorage.removeItem('cellPhoneNo');
    localStorage.removeItem('operatorAdminSummary');
  }

  requestPasswordReset(email: string): Observable<PasswordResetResponse> {
    return this.http.post<PasswordResetResponse>(
      `${this.baseUrl}/forgot-password`,
      { email }
    );
  }

  resetPassword(payload: ResetPasswordPayload): Observable<PasswordResetResponse> {
    return this.http.post<PasswordResetResponse>(
      `${this.baseUrl}/reset-password`,
      payload
    );
  }

  private loadGoogleIdentityServices(): Promise<GoogleIdentityServices> {
    if (window.google?.accounts.id) {
      return Promise.resolve(window.google.accounts.id);
    }

    if (!this.googleScriptPromise) {
      this.googleScriptPromise = new Promise<GoogleIdentityServices>((resolve, reject) => {
        const existingScript = document.querySelector<HTMLScriptElement>(
          'script[data-google-identity-services], script[src^="https://accounts.google.com/gsi/client"]'
        );
        const script = existingScript ?? document.createElement('script');

        const resolveWhenLoaded = () => {
          const google = window.google?.accounts.id;
          if (google) {
            resolve(google);
          } else {
            reject(new Error('Google Identity Services loaded without its sign-in API.'));
          }
        };

        script.addEventListener('load', resolveWhenLoaded, { once: true });
        script.addEventListener(
          'error',
          () => {
            this.googleScriptPromise = undefined;
            reject(new Error('Google Identity Services could not be loaded.'));
          },
          { once: true }
        );

        if (!existingScript) {
          script.src = 'https://accounts.google.com/gsi/client';
          script.async = true;
          script.defer = true;
          script.dataset['googleIdentityServices'] = 'true';
          document.head.appendChild(script);
        }
      });
    }

    return this.googleScriptPromise;
  }
}
