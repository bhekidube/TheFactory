import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  title = 'app';
  showOperatorRoute = false;
  currentYear = new Date().getFullYear();

  constructor(private readonly router: Router) {}

  get showTransportTagline(): boolean {
    const currentPath = this.router.url.split('?')[0].toLowerCase();
    return !currentPath.startsWith('/learning') && !currentPath.startsWith('/school/admin');
  }

  // Call this method when you want to show the operator route component
  toggleOperatorRoute() {
    this.showOperatorRoute = !this.showOperatorRoute;
  }
}

