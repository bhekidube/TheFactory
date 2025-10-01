import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

interface AdminSummary {
  totalOperators: number;
  totalRoutes: number;
  totalTrips: number;
  totalTickets: number;
  operatorNames: string[];
}

interface OperatorAdminSummary {
  totalOperators: number;
}

type UserRole = 'SystemAdmin' | 'Admin' | 'OperatorAdmin' | 'Operator' | 'Customer' | 'Public';

@Component({
  selector: 'app-admin-screen',
  templateUrl: './admin-screen.component.html',
  styleUrls: ['./admin-screen.component.css']
})
export class AdminScreenComponent implements OnInit {
  summary: AdminSummary | null = null;
  operatorAdminSummary: any = null;
  loading = true;
  error: string | null = null;
  userRole: UserRole = (localStorage.getItem('userRole') as UserRole) || 'Public'; // Should be set from authentication
  isLoggedIn = false; // Track login status
  userName: string = 'Unknown User'; // Default value
  targetEmail: string = '';
  newUserRoleId: number = 2; // Default to Admin
  roleChangeMessage: string = '';
  selectedOperator: string | null = null;

  constructor(private http: HttpClient, private router: Router) {}

  ngOnInit(): void 
  {
    this.isLoggedIn = this.checkLogin();
    this.userRole = this.getUserRole();
    this.userName = this.getUserName(); 

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
    return (localStorage.getItem('userRole') as UserRole) || 'Public';
  }

  getUserName(): string {
    return localStorage.getItem('userName') || 'Unknown User';
  }

  isAdminRole(role: UserRole): boolean {
    return role === 'SystemAdmin' || role === 'Admin';
  }

  logout(): void {
    // Clear authentication (example: remove token from localStorage)
    localStorage.removeItem('authToken');
    localStorage.removeItem('userName');
    this.isLoggedIn = false;
    this.router.navigate(['/auth']);
  }

  requestPermission(): void {
    alert('Your request for admin access has been sent. An administrator will review your request.');
    // You can implement an API call here to actually request permission
  }

  changeUserRole(): void {
    this.http.post<{ message: string, error?: string }>(
      'https://AzureLinuxAppService.azurewebsites.net/api/User/ChangeUserRole',
      { email: this.targetEmail, newUserRoleId: this.newUserRoleId }
    ).subscribe({
      next: res => {
        this.roleChangeMessage = res.message;
      },
      error: err => {
        this.roleChangeMessage = err.error?.error || 'Failed to change user role.';
      }
    });
  }

  viewOperator(operator: string): void {
    this.selectedOperator = operator;
    this.operatorAdminSummary = null; // Reset previous summary
    this.http.get<any>(`https://AzureLinuxAppService.azurewebsites.net/api/Admin/GetOperatorSummary?name=${encodeURIComponent(operator)}`)
      .subscribe({
        next: summary => {
          this.operatorAdminSummary = summary;
        },
        error: err => {
          this.roleChangeMessage = 'Failed to load operator summary.';
        }
      });
  }
}
