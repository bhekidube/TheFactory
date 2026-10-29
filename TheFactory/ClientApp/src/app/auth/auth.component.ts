import { isPlatformBrowser } from '@angular/common';
import { AfterViewInit, Component, ElementRef, Inject, OnInit, PLATFORM_ID, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';
import { AuthService, UserLoginResponse } from '../services/auth.service';

@Component({
  selector: 'app-auth',
  templateUrl: './auth.component.html',
  styleUrls: ['./auth.component.css']
})
export class AuthComponent implements OnInit, AfterViewInit {
  @ViewChild('googleSignInButton')
  private googleSignInButton?: ElementRef<HTMLDivElement>;

  showRegister = false; // Controls register form visibility
  loginPasswordVisible = false;
  registerPasswordVisible = false;
  loginSubmitted = false;
  showGoogleProfileCompletion = false;
  googleProfilePhone = '';
  googleProfileSubmitted = false;
  googleProfileSaving = false;
  googleProfileError: string | null = null;
  private pendingGoogleCredential: string | null = null;
  private pendingGoogleLoginResponse: UserLoginResponse | null = null;
  readonly emailPattern = '^[^\\s@]+@[^\\s@]+(?:\\.[^\\s@]+)+$';
  readonly phonePattern = '^\\+[1-9]\\d{1,14}$';

  registerModel: {
    userRoleId: number;
    name: string;
    email: string;
    cellPhoneNo: string;
    alternateCellPhoneNo: string;
    password: string;
    operatorId: number | null;
  } = {
    userRoleId: 3, // Default role->OperatorAdmin
    name: '',
    email: '',
    cellPhoneNo: '+27',
    alternateCellPhoneNo: '+27',
    password: '',
    operatorId: null // <-- Add this line
  };

  loginModel = {
    email: '',
    password: ''
  };

  error: string | null = null;
  operators: any[] = [];
  googleSignInPending = false;

  constructor(
    private http: HttpClient,
    private router: Router,
    private authService: AuthService,
    @Inject(PLATFORM_ID) private readonly platformId: object
  ) {}

  ngOnInit(): void {
    this.fetchOperators();
  }

  ngAfterViewInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      const button = this.googleSignInButton?.nativeElement;
      if (button) {
        this.authService.initializeGoogleSignIn(button, (credential) => {
          this.loginWithGoogleCredential(credential);
        }).catch((error: Error) => {
          this.error = error.message;
        });
      }
    }
  }

  fetchOperators(): void {
    this.http.get<any[]>(`${environment.apiBaseUrl}/api/LookUp/GetAllOperators`).subscribe({
      next: (data) => {
        this.operators = data;
      },
      error: (err) => {
        this.error = 'Failed to load operators';
      }
    });
  }

  toggleRegister() {
    this.showRegister = !this.showRegister;
  }

  toggleLoginPassword(): void {
    this.loginPasswordVisible = !this.loginPasswordVisible;
  }

  toggleRegisterPassword(): void {
    this.registerPasswordVisible = !this.registerPasswordVisible;
  }

  isPhoneNumberValid(phoneNumber: string | null | undefined): boolean {
    return typeof phoneNumber === 'string' && new RegExp(this.phonePattern).test(phoneNumber);
  }

  completeGoogleProfile(): void {
    this.googleProfileSubmitted = true;
    this.googleProfileError = null;

    if (!this.isPhoneNumberValid(this.googleProfilePhone)) {
      return;
    }
    const credential = this.pendingGoogleCredential;
    const loginResponse = this.pendingGoogleLoginResponse;
    if (!credential || !loginResponse) {
      this.googleProfileError = 'Your Google sign-in expired. Please sign in again.';
      return;
    }

    this.googleProfileSaving = true;
    this.authService.completeGoogleProfile(credential, this.googleProfilePhone).subscribe({
      next: (profile) => {
        const response = {
          ...loginResponse,
          cellPhoneNo: profile.cellPhoneNo
        };
        this.googleProfileSaving = false;
        this.pendingGoogleCredential = null;
        this.pendingGoogleLoginResponse = null;
        this.showGoogleProfileCompletion = false;
        this.completeLogin(response, true);
      },
      error: (err) => {
        this.googleProfileSaving = false;
        const validationErrors = err.error?.errors as Record<string, string[] | string> | undefined;
        let validationMessage: string | undefined;
        if (validationErrors) {
          for (const key of Object.keys(validationErrors)) {
            const value = validationErrors[key];
            validationMessage = Array.isArray(value) ? value[0] : value;
            if (validationMessage) {
              break;
            }
          }
        }
        this.googleProfileError = err.error?.error
          || err.error?.title
          || validationMessage
          || 'Could not save your cellphone number. Please try again.';
      }
    });
  }

  isLoginEmailValid(): boolean {
    const email = this.loginModel.email.trim();
    return email.length > 0
      && !email.includes('..')
      && new RegExp(this.emailPattern).test(email);
  }

  continueAsGuest(): void {
    this.error = null;
    this.router.navigate(['/']);
  }

  register() {
    this.http.post(`${environment.apiBaseUrl}/api/User/Register`, this.registerModel).subscribe({
      next: () => {
        this.error = null;
        alert('Registration successful! Please login.');
        this.showRegister = false;
      },
      error: err => {
        alert(err.error?.error || 'Registration failed');
        this.error = err.error?.error || 'Registration failed';
      }
    });
  }

  login() {
    this.loginSubmitted = true;
    if (!this.isLoginEmailValid() || this.loginModel.password.length < 6) {
      return;
    }

    this.http.post<UserLoginResponse>(
      `${environment.apiBaseUrl}/api/User/Login`, this.loginModel
    ).subscribe({
      next: (response) => {
        this.completeLogin(response);
      },
      error: err => {
        this.error = err.error?.error || 'Login failed';
      }
    });
  }

  private loginWithGoogleCredential(credential: string): void {
    if (this.googleSignInPending) {
      return;
    }
    this.authService.logout();
    this.googleSignInPending = true;
    this.error = null;

    this.authService.loginWithGoogle(credential).subscribe({
      next: (response) => {
        if (this.isPhoneNumberValid(response.cellPhoneNo)) {
          this.completeLogin(response, true);
          return;
        }

        this.pendingGoogleCredential = credential;
        this.pendingGoogleLoginResponse = response;
        this.googleProfilePhone = '+27';
        this.googleProfileSubmitted = false;
        this.googleProfileError = null;
        this.showGoogleProfileCompletion = true;
      },
      error: (err) => {
        this.error = err.error?.error || 'Google sign-in failed. Please try again.';
        this.googleSignInPending = false;
      },
      complete: () => {
        this.googleSignInPending = false;
      }
    });
  }

  private completeLogin(response: UserLoginResponse, returnHome = false): void {
    this.error = null;
    if (returnHome && !this.isPhoneNumberValid(response.cellPhoneNo)) {
      this.googleProfileError = 'A valid international cellphone number is required to continue.';
      this.showGoogleProfileCompletion = true;
      return;
    }
    localStorage.setItem('userName', response.userName);
    localStorage.setItem('userRole', response.userRole);
    localStorage.setItem('userId', response.userId?.toString() || '');
    if (returnHome) {
      localStorage.setItem('cellPhoneNo', response.cellPhoneNo!);
    }
    if (returnHome) {
      this.router.navigate(['/']);
    } else if (response.userRole === 'OperatorAdmin') {
      this.viewOperator(response.userName, response.userId?.toString() || '');
    } else if (response.userRole === 'SystemAdmin') {
      this.router.navigate(['/admin-screen']);
    }
  }


  viewOperator(operator: string,userId: string): void {
    this.http.get<any>(`${environment.apiBaseUrl}/api/Admin/OperatorSummary?operatorName=${encodeURIComponent(operator)}`)
      .subscribe({
        next: summary => {
          // Store summary in localStorage for access in OperatorAdminComponent
          localStorage.setItem('operatorAdminSummary', JSON.stringify(summary));
          localStorage.setItem('userId', JSON.stringify(userId));
          this.router.navigate(['/operator-admin', operator]);
        },
        error: err => {
          this.error = 'Failed to load operator summary.';
        }
      });
  }


  forgotPassword() {
    this.router.navigate(['/forgot-password']);
  }
}
