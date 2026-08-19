import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { SubjectDto, SubjectUpsertRequestDto } from '../learner-management/learner-management.models';
import { LearnerManagementService } from '../services/learner-management.service';

@Component({
  selector: 'app-curriculum-view',
  templateUrl: './curriculum-view.component.html',
  styleUrls: ['./curriculum-view.component.css']
})
export class CurriculumViewComponent implements OnInit {
  subjects: SubjectDto[] = [];
  loading = false;
  saving = false;

  showModal = false;
  isEditMode = false;
  editingSubjectId = 0;

  form: SubjectUpsertRequestDto = {
    name: '',
    code: '',
    isActive: true
  };

  errorMessage = '';
  successMessage = '';

  constructor(private readonly learnerService: LearnerManagementService) {}

  ngOnInit(): void {
    this.loadSubjects();
  }

  loadSubjects(): void {
    this.loading = true;
    this.errorMessage = '';

    this.learnerService.getSubjectsForCurriculum().subscribe({
      next: subjects => {
        this.subjects = subjects;
        this.loading = false;
      },
      error: err => {
        this.loading = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to load curriculum subjects.');
      }
    });
  }

  openAddSubject(): void {
    this.clearMessages();
    this.isEditMode = false;
    this.editingSubjectId = 0;
    this.form = { name: '', code: '', isActive: true };
    this.showModal = true;
  }

  openEditSubject(subject: SubjectDto): void {
    this.clearMessages();
    this.isEditMode = true;
    this.editingSubjectId = subject.id;
    this.form = {
      name: subject.name,
      code: subject.code,
      isActive: subject.isActive
    };
    this.showModal = true;
  }

  toggleActive(subject: SubjectDto): void {
    const payload: SubjectUpsertRequestDto = {
      name: subject.name,
      code: subject.code,
      isActive: !subject.isActive
    };

    this.saving = true;
    this.clearMessages();

    this.learnerService.updateSubject(subject.id, payload).subscribe({
      next: () => {
        this.saving = false;
        this.successMessage = 'Subject status updated successfully.';
        this.loadSubjects();
      },
      error: err => {
        this.saving = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to update subject status.');
      }
    });
  }

  saveSubject(): void {
    const validation = this.validateForm();
    if (validation) {
      this.errorMessage = validation;
      return;
    }

    this.saving = true;
    this.clearMessages();

    if (!this.isEditMode) {
      this.learnerService.createSubject(this.form).subscribe({
        next: () => {
          this.saving = false;
          this.showModal = false;
          this.successMessage = 'Subject created successfully.';
          this.loadSubjects();
        },
        error: err => {
          this.saving = false;
          this.errorMessage = this.mapHttpError(err, 'Failed to create subject.');
        }
      });

      return;
    }

    this.learnerService.updateSubject(this.editingSubjectId, this.form).subscribe({
      next: () => {
        this.saving = false;
        this.showModal = false;
        this.successMessage = 'Subject updated successfully.';
        this.loadSubjects();
      },
      error: err => {
        this.saving = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to update subject.');
      }
    });
  }

  closeModal(): void {
    if (this.saving) {
      return;
    }

    this.showModal = false;
  }

  private validateForm(): string {
    if (!this.form.name?.trim()) {
      return 'Name is required.';
    }

    if (!this.form.code?.trim()) {
      return 'Code is required.';
    }

    return '';
  }

  private mapHttpError(error: unknown, fallback: string): string {
    const httpError = error as HttpErrorResponse;
    if (!httpError || typeof httpError.status !== 'number') {
      return fallback;
    }

    if (httpError.status === 400) {
      if (typeof httpError.error === 'string') {
        return httpError.error;
      }

      if (httpError.error && typeof httpError.error.error === 'string') {
        return httpError.error.error;
      }

      return 'Request validation failed. Please review your input.';
    }

    if (httpError.status === 0) {
      return 'Network error. Please check your connection and try again.';
    }

    if (httpError.status >= 500) {
      return 'Server error. Please try again in a moment.';
    }

    return fallback;
  }

  private clearMessages(): void {
    this.errorMessage = '';
    this.successMessage = '';
  }
}
