import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-reset-password',
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.css']
})
export class ResetPasswordComponent implements OnInit {
  email = '';
  token = '';
  newPassword = '';
  confirmPassword = '';
  isLoading = false;
  isComplete = false;
  errorMessage: string | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly authService: AuthService
  ) {}

  ngOnInit(): void {
    this.email = this.route.snapshot.queryParamMap.get('email') ?? '';
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';

    if (!this.email || !this.token) {
      this.errorMessage = 'This password-reset link is incomplete. Request a new link and try again.';
    }
  }

  resetPassword(): void {
    if (this.isLoading || this.newPassword !== this.confirmPassword) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = null;

    this.authService.resetPassword({
      email: this.email,
      token: this.token,
      newPassword: this.newPassword
    }).subscribe({
      next: () => {
        this.isLoading = false;
        this.isComplete = true;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'This password-reset link is invalid or expired. Request a new link and try again.';
      }
    });
  }
}
