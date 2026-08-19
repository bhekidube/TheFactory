import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { ClassDto, SubjectDto, WorkDto, WorkTypeLookupDto, WorkUpsertRequestDto } from '../learner-management/learner-management.models';
import { LearnerManagementService } from '../services/learner-management.service';

@Component({
  selector: 'app-work-management',
  templateUrl: './work-management.component.html',
  styleUrls: ['./work-management.component.css']
})
export class WorkManagementComponent implements OnInit {
  workItems: WorkDto[] = [];
  classes: ClassDto[] = [];
  subjects: SubjectDto[] = [];
  workTypes: WorkTypeLookupDto[] = [];
  searchTerm = '';

  loadingWork = false;
  loadingClasses = false;
  loadingSubjects = false;
  loadingWorkTypes = false;
  creatingWork = false;
  updatingWork = false;
  archivingWorkId: number | null = null;

  showWorkForm = false;
  isEditMode = false;

  errorMessage = '';
  successMessage = '';

  workForm: WorkUpsertRequestDto = this.getEmptyWorkForm();
  editingWorkId = 0;

  constructor(private readonly learnerService: LearnerManagementService) {}

  ngOnInit(): void {
    this.loadWorkItems();
    this.loadClasses();
    this.loadSubjects();
    this.loadWorkTypes();
  }

  get filteredWorkItems(): WorkDto[] {
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) {
      return [...this.workItems];
    }

    return this.workItems.filter(work =>
      work.title.toLowerCase().includes(term)
      || work.workType.toLowerCase().includes(term)
      || work.className.toLowerCase().includes(term)
      || work.id.toString().includes(term)
      || work.dueDate.toLowerCase().includes(term)
    );
  }

  loadWorkItems(): void {
    this.loadingWork = true;
    this.errorMessage = '';

    this.learnerService.getWorkItems().subscribe({
      next: workItems => {
        this.workItems = workItems;
        this.loadingWork = false;
      },
      error: err => {
        this.loadingWork = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to load work items.');
      }
    });
  }

  loadClasses(): void {
    this.loadingClasses = true;
    this.learnerService.getClasses().subscribe({
      next: classes => {
        this.classes = classes;
        this.loadingClasses = false;
      },
      error: err => {
        this.loadingClasses = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to load classes.');
      }
    });
  }

  loadSubjects(): void {
    this.loadingSubjects = true;
    this.learnerService.getSubjectsForCurriculum().subscribe({
      next: subjects => {
        this.subjects = subjects.filter(subject => subject.isActive);
        this.loadingSubjects = false;
      },
      error: err => {
        this.loadingSubjects = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to load subjects.');
      }
    });
  }

  loadWorkTypes(): void {
    this.loadingWorkTypes = true;
    this.learnerService.getWorkTypes().subscribe({
      next: workTypes => {
        this.workTypes = workTypes;
        this.loadingWorkTypes = false;
      },
      error: err => {
        this.loadingWorkTypes = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to load work types.');
      }
    });
  }

  openAddWork(): void {
    this.clearMessages();
    this.isEditMode = false;
    this.editingWorkId = 0;
    this.workForm = this.getEmptyWorkForm();
    this.showWorkForm = true;
  }

  openEditWork(work: WorkDto): void {
    this.clearMessages();
    this.isEditMode = true;
    this.editingWorkId = work.id;
    this.workForm = {
      title: work.title,
      subjectId: work.subjectId,
      description: work.description,
      workTypeId: work.workTypeId,
      classId: work.classId,
      dueDate: work.dueDate,
      totalMark: work.totalMark
    };
    this.showWorkForm = true;
  }

  closeWorkForm(): void {
    if (this.creatingWork || this.updatingWork) {
      return;
    }

    this.showWorkForm = false;
    this.workForm = this.getEmptyWorkForm();
  }

  saveWork(): void {
    this.clearMessages();
    const validationError = this.validateWorkForm();
    if (validationError) {
      this.errorMessage = validationError;
      return;
    }

    if (!this.isEditMode) {
      this.creatingWork = true;
      this.learnerService.createWorkItem(this.workForm).subscribe({
        next: () => {
          this.creatingWork = false;
          this.showWorkForm = false;
          this.successMessage = 'Work item created successfully.';
          this.loadWorkItems();
        },
        error: err => {
          this.creatingWork = false;
          this.errorMessage = this.mapHttpError(err, 'Failed to create work item.');
        }
      });
      return;
    }

    this.updatingWork = true;
    this.learnerService.updateWorkItem(this.editingWorkId, this.workForm).subscribe({
      next: () => {
        this.updatingWork = false;
        this.showWorkForm = false;
        this.successMessage = 'Work item updated successfully.';
        this.loadWorkItems();
      },
      error: err => {
        this.updatingWork = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to update work item.');
      }
    });
  }

  archiveWork(work: WorkDto): void {
    this.clearMessages();

    const confirmed = confirm('Archive this work item?');
    if (!confirmed) {
      return;
    }

    this.archivingWorkId = work.id;
    this.learnerService.archiveWorkItem(work.id).subscribe({
      next: () => {
        this.archivingWorkId = null;
        this.successMessage = 'Work item archived successfully.';
        this.loadWorkItems();
      },
      error: err => {
        this.archivingWorkId = null;
        this.errorMessage = this.mapHttpError(err, 'Failed to archive work item.');
      }
    });
  }

  trackByWorkId(_: number, work: WorkDto): number {
    return work.id;
  }

  private validateWorkForm(): string {
    if (!this.workForm.title?.trim()) {
      return 'Title is required.';
    }

    if (!Number.isFinite(this.workForm.subjectId) || this.workForm.subjectId <= 0) {
      return 'Subject is required.';
    }

    if (!Number.isFinite(this.workForm.workTypeId) || this.workForm.workTypeId <= 0) {
      return 'Work type is required.';
    }

    const validWorkTypeIds = new Set(this.workTypes.map(workType => workType.id));
    if (!validWorkTypeIds.has(this.workForm.workTypeId)) {
      return 'Work type is invalid.';
    }

    if (!Number.isFinite(this.workForm.classId) || this.workForm.classId <= 0) {
      return 'Class is required.';
    }

    if (!Number.isFinite(this.workForm.totalMark) || this.workForm.totalMark <= 0) {
      return 'Total mark must be greater than zero.';
    }

    return '';
  }

  private getEmptyWorkForm(): WorkUpsertRequestDto {
    return {
      title: '',
      subjectId: 0,
      description: '',
      workTypeId: 0,
      classId: 0,
      dueDate: '',
      totalMark: 100
    };
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

    if (httpError.status === 404) {
      return 'The requested work item could not be found.';
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
