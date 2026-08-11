import { Component, OnInit } from '@angular/core';
import {
  LearnerManagementService,
  UserRoleDto
} from '../services/learner-management.service';

@Component({
  selector: 'app-learning',
  templateUrl: './learning.component.html',
  styleUrls: ['./learning.component.css']
})
export class LearningComponent implements OnInit {
  schools: Array<{ id: string; name: string }> = [];
  userRoles: UserRoleDto[] = [];
  loading = false;
  adminLoading = false;
  errorMessage = '';
  adminErrorMessage = '';
  adminSuccessMessage = '';

  readonly isSystemAdmin = (localStorage.getItem('userRole') || '').toLowerCase() === 'systemadmin';

  createTenantModel = {
    name: '',
    logoUrl: ''
  };

  roleAllocationModel = {
    email: '',
    userRoleId: 0
  };

  constructor(private readonly learnerService: LearnerManagementService) {}

  ngOnInit(): void {
    this.learnerService.clearSelectedTenantId();
    this.loadSchools();
    if (this.isSystemAdmin) {
      this.loadUserRoles();
    }
  }

  createTenant(): void {
    const tenantName = this.createTenantModel.name.trim();
    if (!tenantName || !this.isSystemAdmin) {
      return;
    }

    this.adminLoading = true;
    this.adminErrorMessage = '';
    this.adminSuccessMessage = '';

    this.learnerService.createSchool({
      name: tenantName,
      logoUrl: this.createTenantModel.logoUrl.trim() || undefined
    }).subscribe({
      next: createdSchool => {
        this.adminLoading = false;
        this.adminSuccessMessage = `Tenant created: ${createdSchool.name}`;
        this.createTenantModel = { name: '', logoUrl: '' };
        this.loadSchools();
      },
      error: (errorResponse) => {
        this.adminLoading = false;
        this.adminErrorMessage = errorResponse?.error?.error || 'Unable to create tenant.';
      }
    });
  }

  allocateUserRole(): void {
    const email = this.roleAllocationModel.email.trim();
    const userRoleId = Number(this.roleAllocationModel.userRoleId);
    if (!email || !userRoleId || !this.isSystemAdmin) {
      return;
    }

    this.adminLoading = true;
    this.adminErrorMessage = '';
    this.adminSuccessMessage = '';

    this.learnerService.assignUserRole({
      email,
      userRoleId
    }).subscribe({
      next: response => {
        this.adminLoading = false;
        this.adminSuccessMessage = response.message || 'User role updated successfully.';
        this.roleAllocationModel = { email: '', userRoleId: 0 };
      },
      error: (errorResponse) => {
        this.adminLoading = false;
        this.adminErrorMessage = errorResponse?.error?.error || 'Unable to update user role.';
      }
    });
  }

  private loadSchools(): void {
    this.loading = true;
    this.errorMessage = '';

    this.learnerService.getSchools().subscribe({
      next: schools => {
        this.schools = schools.map(school => ({
          id: school.id.toString(),
          name: school.name
        }));
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Unable to load schools right now.';
      }
    });
  }

  private loadUserRoles(): void {
    this.learnerService.getUserRoles().subscribe({
      next: roles => {
        this.userRoles = roles;
      },
      error: () => {
        this.adminErrorMessage = 'Unable to load user roles right now.';
      }
    });
  }
}
