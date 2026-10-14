import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-logout-link',
  template: `
    <span *ngIf="userName">
      Logged in as <strong>{{ userName }}</strong>
      <a class="logout-link" href="#" (click)="logout($event)">Log out</a>
    </span>
  `,
  styles: [`.logout-link { color: #d9534f; cursor: pointer; margin-left: 10px; }`]
})
export class LogoutLinkComponent {
  userName: string = localStorage.getItem('userName') || '';

  constructor(
    private readonly router: Router,
    private readonly authService: AuthService
  ) {}

  logout(event: Event): void {
    event.preventDefault();
    this.authService.logout();
    this.router.navigate(['/']);
  }
}
