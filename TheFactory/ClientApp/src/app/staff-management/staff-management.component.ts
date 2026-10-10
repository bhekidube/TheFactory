import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { LearnerManagementService } from '../services/learner-management.service';
import { StaffDto, StaffUpsertRequest } from '../learner-management/learner-management.models';

@Component({
  selector: 'app-staff-management',
  templateUrl: './staff-management.component.html',
  styleUrls: ['./staff-management.component.css']
})
export class StaffManagementComponent implements OnInit, OnDestroy {
  schoolId = 0;
  staff: StaffDto[] = [];
  searchTerm = '';
  loading = false;
  saving = false;
  errorMessage = '';
  successMessage = '';
  showForm = false;
  editingNationalId: string | null = null;
  editingLegacyStaffId: number | null = null;
  formSubmitted = false;
  readonly roleOptions = ['Teacher', 'Admin', 'Support'];
  form: StaffUpsertRequest = this.emptyForm();

  private routeSubscription?: Subscription;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly learnerService: LearnerManagementService
  ) {}

  ngOnInit(): void {
    this.routeSubscription = this.route.parent?.paramMap.subscribe(params => {
      this.schoolId = Number(params.get('schoolId'));
      if (this.schoolId > 0) {
        this.learnerService.setSelectedTenantId(this.schoolId);
        this.loadStaff();
      }
    });
  }

  ngOnDestroy(): void {
    this.routeSubscription?.unsubscribe();
  }

  get activeStaffCount(): number {
    return this.staff.filter(member => member.status === 'Active').length;
  }

  get registeredRolesCount(): number {
    return new Set(this.staff.map(member => member.role.trim()).filter(Boolean)).size;
  }

  loadStaff(): void {
    this.loading = true;
    this.errorMessage = '';
    this.learnerService.getStaff(this.schoolId, this.searchTerm).subscribe({
      next: staff => {
        this.staff = staff;
        this.loading = false;
      },
      error: error => {
        this.loading = false;
        this.errorMessage = error?.error?.error || 'Failed to load staff.';
      }
    });
  }

  openAddStaff(): void {
    this.clearMessages();
    this.editingNationalId = null;
    this.editingLegacyStaffId = null;
    this.formSubmitted = false;
    this.form = this.emptyForm();
    this.showForm = true;
  }

  openEditStaff(member: StaffDto): void {
    this.clearMessages();
    this.editingNationalId = member.nationalId || null;
    this.editingLegacyStaffId = member.nationalId ? null : member.legacyId ?? null;
    this.formSubmitted = false;
    this.form = {
      nationalId: member.nationalId,
      userId: member.userId ?? null,
      firstName: member.firstName,
      surname: member.surname,
      role: member.role,
      email: member.email,
      phone: member.phone,
      status: member.status
    };
    this.showForm = true;
  }

  closeForm(): void {
    if (this.saving) {
      return;
    }

    this.showForm = false;
    this.editingNationalId = null;
    this.editingLegacyStaffId = null;
    this.formSubmitted = false;
  }

  saveStaff(): void {
    this.formSubmitted = true;
    this.clearMessages();
    if (!this.isFormValid()) {
      return;
    }

    const payload: StaffUpsertRequest = {
      ...this.form,
      firstName: this.form.firstName.trim(),
      surname: this.form.surname.trim(),
      role: this.form.role.trim(),
      email: this.form.email.trim(),
      phone: this.form.phone.trim(),
      status: this.form.status || 'Active'
    };

    this.saving = true;
    const request = this.editingLegacyStaffId !== null
      ? this.learnerService.updateLegacyStaff(this.schoolId, this.editingLegacyStaffId, payload)
      : this.editingNationalId
        ? this.learnerService.updateStaff(this.schoolId, this.editingNationalId, payload)
        : this.learnerService.createStaff(this.schoolId, payload);

    request.subscribe({
      next: () => {
        this.saving = false;
        this.showForm = false;
        this.successMessage = this.editingNationalId || this.editingLegacyStaffId !== null
          ? 'Staff member updated successfully.'
          : 'Staff member added successfully.';
        this.editingNationalId = null;
        this.editingLegacyStaffId = null;
        this.loadStaff();
      },
      error: error => {
        this.saving = false;
        this.errorMessage = error?.error?.error || 'Failed to save staff member.';
      }
    });
  }

  archiveStaff(member: StaffDto): void {
    if (!confirm(`Archive ${member.firstName} ${member.surname}?`)) {
      return;
    }

    this.clearMessages();
    const request = member.nationalId
      ? this.learnerService.archiveStaff(this.schoolId, member.nationalId)
      : member.legacyId !== null && member.legacyId !== undefined
        ? this.learnerService.archiveLegacyStaff(this.schoolId, member.legacyId)
        : null;

    if (!request) {
      this.errorMessage = 'This staff record cannot be edited until it has an identifier.';
      return;
    }

    request.subscribe({
      next: () => {
        this.successMessage = 'Staff member archived successfully.';
        this.loadStaff();
      },
      error: error => {
        this.errorMessage = error?.error?.error || 'Failed to archive staff member.';
      }
    });
  }

  trackByNationalId(index: number, member: StaffDto): string | number {
    return member.nationalId || member.legacyId || index;
  }

  isFormValid(): boolean {
    return !!this.form.firstName.trim()
      && !!this.form.nationalId.trim()
      && !!this.form.surname.trim()
      && !!this.form.role.trim()
      && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.form.email.trim());
  }

  private emptyForm(): StaffUpsertRequest {
    return {
      nationalId: '',
      userId: null,
      firstName: '',
      surname: '',
      role: 'Teacher',
      email: '',
      phone: '',
      status: 'Active'
    };
  }

  private clearMessages(): void {
    this.errorMessage = '';
    this.successMessage = '';
  }
}
