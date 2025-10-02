import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-logout-link',
  template: `<a class="logout-link" href="#" (click)="logout($event)">Log out</a>`,
  styles: [`.logout-link { color: #d9534f; cursor: pointer; }`]
})
export class LogoutLinkComponent {
  constructor(private router: Router) {}

  logout(event: Event): void {
    event.preventDefault();
    localStorage.removeItem('userName');
    localStorage.removeItem('userRole');
    localStorage.removeItem('authToken');
    this.router.navigate(['/auth']);
  }
}
