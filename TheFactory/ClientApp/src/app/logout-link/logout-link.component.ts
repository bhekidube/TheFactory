import { Component } from '@angular/core';
import { Router } from '@angular/router';

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

  constructor(private router: Router) {}

  logout(event: Event): void {
    event.preventDefault();
    localStorage.removeItem('userName');
    localStorage.removeItem('userRole');
    localStorage.removeItem('authToken');
    this.router.navigate(['/auth']);
  }
}
