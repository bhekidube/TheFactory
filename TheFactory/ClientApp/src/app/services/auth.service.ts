import { Inject, Injectable, PLATFORM_ID } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
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
    options: { theme: 'outline'; size: 'large'; width: number }
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
  operatorId?: number;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly baseUrl = `${environment.apiBaseUrl}/api/auth`;
  private googleScriptPromise?: Promise<GoogleIdentityServices>;

  constructor(
    private readonly http: HttpClient,
    @Inject(PLATFORM_ID) private readonly platformId: object
  ) {}

  initializeGoogleSignIn(
    buttonContainer: HTMLElement,
    onCredential: (credential: string) => void
  ): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) {
      return Promise.resolve();
    }

    if (!environment.googleClientId) {
      return Promise.reject(new Error('Google sign-in is not configured for this environment.'));
    }

    return this.loadGoogleIdentityServices().then((google) => {
      google.initialize({
        client_id: environment.googleClientId,
        callback: (response) => {
          if (!response.credential) {
            throw new Error('Google did not return an identity credential.');
          }
          onCredential(response.credential);
        },
        ux_mode: 'popup',
        auto_select: false
      });
      google.renderButton(buttonContainer, {
        theme: 'outline',
        size: 'large',
        width: Math.max(200, Math.floor(buttonContainer.getBoundingClientRect().width))
      });
    });
  }

  loginWithGoogle(credential: string): Observable<UserLoginResponse> {
    return this.http.post<UserLoginResponse>(
      `${environment.apiBaseUrl}/api/User/GoogleLogin`,
      { credential }
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
          'script[data-google-identity-services]'
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
          () => reject(new Error('Google Identity Services could not be loaded.')),
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
