import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

interface AdminSummary {
  totalUsers: number;
  totalOperators: number;
  totalRoutes: number;
  totalTickets: number;
}

type UserRole = 'SystemAdmin' | 'Admin' | 'OperatorAdmin' | 'Operator' | 'Customer' | 'Public';

@Component({
  selector: 'app-admin-screen',
  templateUrl: './admin-screen.component.html',
  styleUrls: ['./admin-screen.component.css']
})
export class AdminScreenComponent implements OnInit {
  summary: AdminSummary | null = null;
  loading = true;
  error: string | null = null;
  userRole: UserRole = 'Public'; // Should be set from authentication
  isLoggedIn = false; // Track login status
  userName: string = 'Unknown User'; // Default value

  constructor(private http: HttpClient, private router: Router) {}

  ngOnInit(): void {
    this.isLoggedIn = this.checkLogin();
    this.userRole = this.getUserRole();
    this.userName = this.getUserName(); // Add this line
alert(this.userName);
    if (!this.isLoggedIn) {
      this.router.navigate(['/auth']);
      return;
    }

    if (this.isAdminRole(this.userRole)) {
      this.http.get<AdminSummary>('https://AzureLinuxAppService.azurewebsites.net/api/Admin/Summary').subscribe({
        next: data => {
          this.summary = data;
          this.loading = false;
        },
        error: err => {
          this.error = 'Failed to load summary';
          this.loading = false;
        }
      });
    } else {
      this.loading = false;
    }
  }

  checkLogin(): boolean {
    return true;
  }

  getUserRole(): UserRole {
    // TODO: Replace with actual logic to get user role from authentication
    return 'Admin';
  }

  getUserName(): string {
    return localStorage.getItem('userName') || 'Unknown User';
  }

  isAdminRole(role: UserRole): boolean {
    return role === 'SystemAdmin' || role === 'Admin' || role === 'OperatorAdmin';
  }

  logout(): void {
    // Clear authentication (example: remove token from localStorage)
    localStorage.removeItem('authToken');
    localStorage.removeItem('userName');
    this.isLoggedIn = false;
    this.router.navigate(['/auth']);
  }
}
