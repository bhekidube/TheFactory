import { Component, OnInit } from '@angular/core';
import { AllUsersDirectoryDto } from '../learner-management/learner-management.models';
import { LearnerManagementService } from '../services/learner-management.service';

@Component({
  selector: 'app-all-users-directory',
  templateUrl: './all-users-directory.component.html',
  styleUrls: ['./all-users-directory.component.css']
})
export class AllUsersDirectoryComponent implements OnInit {
  users: AllUsersDirectoryDto[] = [];
  searchTerm = '';
  loading = false;
  errorMessage = '';

  constructor(private readonly learnerManagementService: LearnerManagementService) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  get filteredUsers(): AllUsersDirectoryDto[] {
    const search = this.searchTerm.trim().toLocaleLowerCase();
    if (!search) {
      return this.users;
    }

    return this.users.filter(user =>
      user.name.toLocaleLowerCase().includes(search)
      || user.email.toLocaleLowerCase().includes(search)
      || user.userType.toLocaleLowerCase().includes(search)
    );
  }

  loadUsers(): void {
    this.loading = true;
    this.errorMessage = '';
    this.learnerManagementService.getAllUsersDirectory().subscribe({
      next: users => {
        this.users = users;
        this.loading = false;
      },
      error: error => {
        this.errorMessage = error?.status === 403
          ? 'You are not authorized to view the all-users directory.'
          : error?.error?.error || 'Failed to load the all-users directory.';
        this.loading = false;
      }
    });
  }

  formatAddress(address: AllUsersDirectoryDto['address']): string {
    return [
      address.addressLine1,
      address.addressLine2,
      address.city,
      address.provinceState,
      address.postalCode,
      address.country
    ].map(value => value?.trim()).filter(Boolean).join(', ') || '—';
  }

  trackBySource(index: number, user: AllUsersDirectoryDto): string {
    return `${user.userType}:${user.sourceId}`;
  }
}
