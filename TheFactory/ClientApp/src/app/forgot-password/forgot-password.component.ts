import { Component } from '@angular/core';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.css']
})
export class ForgotPasswordComponent {
  email = '';
  isLoading = false;
  successMessage: string | null = null;
  errorMessage: string | null = null;

  constructor(private readonly authService: AuthService) {}

  requestReset(): void {
    this.isLoading = true;
    this.successMessage = null;
    this.errorMessage = null;

    this.authService.requestPasswordReset(this.email.trim()).subscribe({
      next: () => {
        this.isLoading = false;
        this.successMessage = 'Reset link sent';
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'We could not send a reset link. Please try again.';
      }
    });
  }
}
