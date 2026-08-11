import { Component } from '@angular/core';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  title = 'app';
  showOperatorRoute = false;
  currentYear = new Date().getFullYear();
  // Call this method when you want to show the operator route component
  toggleOperatorRoute() {
    this.showOperatorRoute = !this.showOperatorRoute;
  }
}

