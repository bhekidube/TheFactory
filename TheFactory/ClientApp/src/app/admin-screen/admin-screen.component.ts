import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

interface AdminSummary {
  totalUsers: number;
  totalOperators: number;
  totalRoutes: number;
  totalTickets: number;
  UserRole: number; // Add this line
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
  userRole: UserRole = 'Public'; // Default, should be set from authentication

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    // TODO: Replace with actual user role from authentication service
    this.userRole = this.getUserRole();

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

  getUserRole(): UserRole {
    // TODO: Replace with actual logic to get user role from authentication
    return 'Admin';
  }

  isAdminRole(role: UserRole): boolean {
    return role === 'SystemAdmin' || role === 'Admin' || role === 'OperatorAdmin';
  }
}
