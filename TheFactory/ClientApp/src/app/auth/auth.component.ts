import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

@Component({
  selector: 'app-auth',
  templateUrl: './auth.component.html',
  styleUrls: ['./auth.component.css']
})
export class AuthComponent {
  showRegister = false; // Controls register form visibility

  registerModel = {
    userRoleId: 4, // Default to 'Operator'
    name: '',
    email: '',
    cellPhoneNo: '',
    alternateCellPhoneNo: '',
    password: ''
  };

  loginModel = {
    email: '',
    password: ''
  };

  error: string | null = null;

  constructor(private http: HttpClient, private router: Router) {}

  toggleRegister() {
    this.showRegister = !this.showRegister;
  }

  register() {
    this.http.post('https://AzureLinuxAppService.azurewebsites.net/api/User/Register', this.registerModel).subscribe({
      next: () => {
        this.error = null;
        alert('Registration successful! Please login.');
        this.showRegister = false;
      },
      error: err => {
        this.error = err.error?.error || 'Registration failed';
      }
    });
  }

  login() {
    this.http.post<{ userName: string, userRole: string }>('https://AzureLinuxAppService.azurewebsites.net/api/User/Login', this.loginModel).subscribe({
      next: (response) => {
        this.error = null;
        localStorage.setItem('userName', response.userName);
        localStorage.setItem('userRole', response.userRole); // Save userRole
        if (response.userRole === 'OperatorAdmin') {
          this.router.navigate(['/operator-admin', response.userName]);
        } else {
          this.router.navigate(['/admin-screen']);
        }
      },
      error: err => {
        this.error = err.error?.error || 'Login failed';
      }
    });
  }

  forgotPassword() {
    alert('Forgot password functionality coming soon.');
  }
}
