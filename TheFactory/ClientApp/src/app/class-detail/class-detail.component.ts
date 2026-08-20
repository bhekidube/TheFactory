import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ClassAssignedSubjectDto, ClassDetailDto, WorkDto, WorkTypeLookupDto, WorkUpsertRequestDto } from '../learner-management/learner-management.models';
import { LearnerManagementService } from '../services/learner-management.service';

@Component({
  selector: 'app-class-detail',
  templateUrl: './class-detail.component.html',
  styleUrls: ['./class-detail.component.css']
})
export class ClassDetailComponent implements OnInit {
  readonly termOptions = ['Term 1', 'Term 2', 'Term 3', 'Term 4'];
  classId = 0;
  classDetail: ClassDetailDto | null = null;
  loading = false;
  loadingWork = false;
  assigningSubject = false;
  searchingSubjects = false;
  creatingWork = false;
  updatingWork = false;
  archivingWorkId: number | null = null;

  showSubjectModal = false;
  showWorkModal = false;
  subjectSearchTerm = '';
  subjectLookupResults: ClassAssignedSubjectDto[] = [];
  selectedSubject: ClassAssignedSubjectDto | null = null;
  isEditWorkMode = false;
  editingWorkId = 0;
  workItems: WorkDto[] = [];
  workTypes: WorkTypeLookupDto[] = [];
  workForm: WorkUpsertRequestDto = this.getEmptyWorkForm();

  errorMessage = '';
  successMessage = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly learnerService: LearnerManagementService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = Number(params.get('id'));
      this.classId = Number.isFinite(id) ? id : 0;
      this.loadClassDetail();
    });
  }

  loadClassDetail(): void {
    if (!this.classId || this.classId <= 0) {
      this.errorMessage = 'Invalid class id.';
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    this.learnerService.getClassById(this.classId).subscribe({
      next: detail => {
        this.classDetail = detail;
        this.loading = false;
        this.loadWorkTypes();
        this.loadWorkItems();
      },
      error: err => {
        this.loading = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to load class details.');
      }
    });
  }

  openAddSubjectModal(): void {
    this.clearMessages();
    this.showSubjectModal = true;
    this.subjectSearchTerm = '';
    this.subjectLookupResults = [];
    this.selectedSubject = null;
  }

  closeAddSubjectModal(): void {
    if (this.assigningSubject || this.searchingSubjects) {
      return;
    }

    this.showSubjectModal = false;
    this.subjectSearchTerm = '';
    this.subjectLookupResults = [];
    this.selectedSubject = null;
  }

  searchSubjects(): void {
    const term = this.subjectSearchTerm.trim();
    if (term.length < 2) {
      this.subjectLookupResults = [];
      return;
    }

    this.searchingSubjects = true;
    this.learnerService.searchSubjects(term).subscribe({
      next: subjects => {
        const assignedIds = new Set((this.classDetail?.subjects || []).map(item => item.subjectId));
        this.subjectLookupResults = subjects.filter(subject => !assignedIds.has(subject.subjectId));
        this.searchingSubjects = false;
      },
      error: err => {
        this.searchingSubjects = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to search subjects.');
      }
    });
  }

  selectSubject(subject: ClassAssignedSubjectDto): void {
    this.selectedSubject = subject;
    this.subjectSearchTerm = `${subject.name}${subject.code ? ' (' + subject.code + ')' : ''}`;
    this.subjectLookupResults = [];
  }

  assignSelectedSubject(): void {
    if (!this.selectedSubject || this.assigningSubject || !this.classDetail) {
      return;
    }

    this.assigningSubject = true;
    this.clearMessages();

    this.learnerService.assignSubjectToClass(this.classId, this.selectedSubject.subjectId).subscribe({
      next: assigned => {
        const current = this.classDetail?.subjects || [];
        if (!current.some(item => item.subjectId === assigned.subjectId) && this.classDetail) {
          this.classDetail = {
            ...this.classDetail,
            subjects: [...current, assigned].sort((a, b) => a.name.localeCompare(b.name))
          };
        }

        this.assigningSubject = false;
        this.showSubjectModal = false;
        this.successMessage = 'Subject assigned to class successfully.';
      },
      error: err => {
        this.assigningSubject = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to assign subject.');
      }
    });
  }

  loadWorkItems(): void {
    if (!this.classId || this.classId <= 0) {
      return;
    }

    this.loadingWork = true;
    this.learnerService.getClassWorkItems(this.classId).subscribe({
      next: items => {
        this.workItems = items;
        this.loadingWork = false;
      },
      error: err => {
        this.loadingWork = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to load class work items.');
      }
    });
  }

  loadWorkTypes(): void {
    this.learnerService.getWorkTypes().subscribe({
      next: workTypes => {
        this.workTypes = workTypes;
      },
      error: err => {
        this.errorMessage = this.mapHttpError(err, 'Failed to load work types.');
      }
    });
  }

  openAddWorkModal(): void {
    this.clearMessages();
    this.isEditWorkMode = false;
    this.editingWorkId = 0;
    this.workForm = this.getEmptyWorkForm();
    this.showWorkModal = true;
  }

  openEditWorkModal(work: WorkDto): void {
    this.clearMessages();
    this.isEditWorkMode = true;
    this.editingWorkId = work.id;
    this.workForm = {
      title: work.title,
      subjectId: work.subjectId,
      description: work.description,
      workTypeId: work.workTypeId,
      term: work.term,
      classId: this.classId,
      dueDate: work.dueDate,
      totalMark: work.totalMark
    };
    this.showWorkModal = true;
  }

  closeWorkModal(): void {
    if (this.creatingWork || this.updatingWork) {
      return;
    }

    this.showWorkModal = false;
    this.workForm = this.getEmptyWorkForm();
  }

  saveWork(): void {
    this.clearMessages();
    const validationError = this.validateWorkForm();
    if (validationError) {
      this.errorMessage = validationError;
      return;
    }

    if (!this.isEditWorkMode) {
      this.creatingWork = true;
      this.learnerService.createClassWorkItem(this.classId, this.workForm).subscribe({
        next: () => {
          this.creatingWork = false;
          this.showWorkModal = false;
          this.successMessage = 'Work item created successfully.';
          this.loadWorkItems();
        },
        error: err => {
          this.creatingWork = false;
          this.errorMessage = this.mapHttpError(err, 'Failed to create class work item.');
        }
      });

      return;
    }

    this.updatingWork = true;
    this.learnerService.updateClassWorkItem(this.classId, this.editingWorkId, this.workForm).subscribe({
      next: () => {
        this.updatingWork = false;
        this.showWorkModal = false;
        this.successMessage = 'Work item updated successfully.';
        this.loadWorkItems();
      },
      error: err => {
        this.updatingWork = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to update class work item.');
      }
    });
  }

  archiveWork(work: WorkDto): void {
    this.clearMessages();

    const confirmed = confirm('Delete this work item?');
    if (!confirmed) {
      return;
    }

    this.archivingWorkId = work.id;
    this.learnerService.archiveClassWorkItem(this.classId, work.id).subscribe({
      next: () => {
        this.archivingWorkId = null;
        this.successMessage = 'Work item deleted successfully.';
        this.loadWorkItems();
      },
      error: err => {
        this.archivingWorkId = null;
        this.errorMessage = this.mapHttpError(err, 'Failed to delete class work item.');
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

    const validWorkTypeIds = new Set(this.workTypes.map(workType => workType.id));
    if (!Number.isFinite(this.workForm.workTypeId) || !validWorkTypeIds.has(this.workForm.workTypeId)) {
      return 'A valid work type is required.';
    }

    if (!this.termOptions.includes((this.workForm.term || '').trim())) {
      return 'A valid term is required.';
    }

    const validSubjectIds = new Set((this.classDetail?.subjects || []).map(subject => subject.subjectId));
    if (!Number.isFinite(this.workForm.subjectId) || !validSubjectIds.has(this.workForm.subjectId)) {
      return 'A valid class subject is required.';
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
      term: '',
      classId: this.classId,
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
      return 'The requested class or subject could not be found.';
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
