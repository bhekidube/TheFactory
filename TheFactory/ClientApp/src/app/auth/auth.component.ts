import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-auth',
  templateUrl: './auth.component.html',
  styleUrls: ['./auth.component.css']
})
export class AuthComponent implements OnInit {
  showRegister = false; // Controls register form visibility

  registerModel: any = {
    userRoleId: 3, // Default role->OperatorAdmin
    name: '',
    email: '',
    cellPhoneNo: '',
    alternateCellPhoneNo: '',
    password: '',
    operatorId: null // <-- Add this line
  };

  loginModel = {
    email: '',
    password: ''
  };

  error: string | null = null;
  operators: any[] = [];

  constructor(private http: HttpClient, private router: Router) {}

  ngOnInit(): void {
    this.fetchOperators();
  }

  fetchOperators(): void {
    this.http.get<any[]>(`${environment.apiBaseUrl}/api/LookUp/GetAllOperators`).subscribe({
      next: (data) => {
        this.operators = data;
      },
      error: (err) => {
        this.error = 'Failed to load operators';
      }
    });
  }

  toggleRegister() {
    this.showRegister = !this.showRegister;
  }

  register() {
    this.http.post(`${environment.apiBaseUrl}/api/User/Register`, this.registerModel).subscribe({
      next: () => {
        this.error = null;
        alert('Registration successful! Please login.');
        this.showRegister = false;
      },
      error: err => {
        alert(err.error?.error || 'Registration failed');
        this.error = err.error?.error || 'Registration failed';
      }
    });
  }

  login() {
    this.http.post<{ userName: string, userRole: string, userId: number }>(
      `${environment.apiBaseUrl}/api/User/Login`, this.loginModel
    ).subscribe({
      next: (response) => {
        this.error = null;
        localStorage.setItem('userName', response.userName);
        localStorage.setItem('userRole', response.userRole);
        localStorage.setItem('userId', response.userId?.toString() || '');
        if (response.userRole === 'OperatorAdmin') {
          this.viewOperator(response.userName, response.userId?.toString() || '');
        } else if (response.userRole === 'SystemAdmin') {
          this.router.navigate(['/admin-screen']);
        } else {
          // handle other roles if needed
        }
      },
      error: err => {
        this.error = err.error?.error || 'Login failed';
      }
    });
  }


  viewOperator(operator: string,userId: string): void {
    this.http.get<any>(`${environment.apiBaseUrl}/api/Admin/OperatorSummary?operatorName=${encodeURIComponent(operator)}`)
      .subscribe({
        next: summary => {
          // Store summary in localStorage for access in OperatorAdminComponent
          localStorage.setItem('operatorAdminSummary', JSON.stringify(summary));
          localStorage.setItem('userId', JSON.stringify(userId));
          this.router.navigate(['/operator-admin', operator]);
        },
        error: err => {
          this.error = 'Failed to load operator summary.';
        }
      });
  }


  forgotPassword() {
    alert('Forgot password functionality coming soon.');
  }
}
