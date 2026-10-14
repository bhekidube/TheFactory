import { Component, OnInit } from '@angular/core';
import { AllUsersDirectoryDto } from '../learner-management/learner-management.models';
import { LearnerManagementService } from '../services/learner-management.service';
import { TableSortController } from '../table-sorting';

@Component({
  selector: 'app-all-users-directory',
  templateUrl: './all-users-directory.component.html',
  styleUrls: ['./all-users-directory.component.css']
})
export class AllUsersDirectoryComponent implements OnInit {
  readonly tableSort = new TableSortController();
  users: AllUsersDirectoryDto[] = [];
  pendingDeleteUser: AllUsersDirectoryDto | null = null;
  reassignUserId: number | null = null;
  searchTerm = '';
  loading = false;
  deleting = false;
  errorMessage = '';
  successMessage = '';

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

  get reassignmentUsers(): AllUsersDirectoryDto[] {
    return this.users.filter(user =>
      user.sourceType === 'User' && user.sourceId !== this.pendingDeleteUser?.sourceId
    );
  }

  get currentUserId(): number {
    return Number(localStorage.getItem('userId')) || 0;
  }

  loadUsers(): void {
    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';
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

  openDeleteDialog(user: AllUsersDirectoryDto): void {
    if (user.sourceType !== 'User' || user.sourceId === this.currentUserId) {
      return;
    }

    this.errorMessage = '';
    this.successMessage = '';
    this.pendingDeleteUser = user;
    this.reassignUserId = null;
  }

  closeDeleteDialog(): void {
    if (this.deleting) {
      return;
    }

    this.pendingDeleteUser = null;
    this.reassignUserId = null;
  }

  deleteUser(): void {
    if (!this.pendingDeleteUser || !this.reassignUserId || this.deleting) {
      return;
    }

    this.deleting = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.learnerManagementService.deleteDirectoryUser(this.pendingDeleteUser.sourceId, this.reassignUserId).subscribe({
      next: response => {
        const successMessage = response.message;
        this.pendingDeleteUser = null;
        this.reassignUserId = null;
        this.deleting = false;
        this.loadUsers();
        this.successMessage = successMessage;
      },
      error: error => {
        this.errorMessage = error?.error?.error || 'Failed to delete this user.';
        this.deleting = false;
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
