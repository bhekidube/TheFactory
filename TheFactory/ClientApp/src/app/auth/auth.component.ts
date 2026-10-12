import { isPlatformBrowser } from '@angular/common';
import { Component, Inject, OnInit, PLATFORM_ID } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';
import { AddressDto } from '../learner-management/learner-management.models';
import { AuthService, UserLoginResponse } from '../services/auth.service';

@Component({
  selector: 'app-auth',
  templateUrl: './auth.component.html',
  styleUrls: ['./auth.component.css']
})
export class AuthComponent implements OnInit {
  showRegister = false; // Controls register form visibility
  loginPasswordVisible = false;
  registerPasswordVisible = false;
  loginSubmitted = false;
  readonly emailPattern = '^[^\\s@]+@[^\\s@]+(?:\\.[^\\s@]+)+$';

  registerModel: {
    userRoleId: number;
    name: string;
    email: string;
    cellPhoneNo: string;
    alternateCellPhoneNo: string;
    password: string;
    address: AddressDto;
    operatorId: number | null;
  } = {
    userRoleId: 3, // Default role->OperatorAdmin
    name: '',
    email: '',
    cellPhoneNo: '',
    alternateCellPhoneNo: '',
    password: '',
    address: {
      addressId: null,
      addressLine1: '',
      addressLine2: '',
      city: '',
      provinceState: '',
      postalCode: '',
      country: ''
    },
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
    if (isPlatformBrowser(this.platformId)) {
      const button = document.getElementById('google-signin-button');
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
    this.googleSignInPending = true;
    this.error = null;

    this.authService.loginWithGoogle(credential).subscribe({
      next: (response) => this.completeLogin(response, true),
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
    localStorage.setItem('userName', response.userName);
    localStorage.setItem('userRole', response.userRole);
    localStorage.setItem('userId', response.userId?.toString() || '');
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
