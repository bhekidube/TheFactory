import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ClassAssignedSubjectDto, ClassDetailDto } from '../learner-management/learner-management.models';
import { LearnerManagementService } from '../services/learner-management.service';

@Component({
  selector: 'app-class-detail',
  templateUrl: './class-detail.component.html',
  styleUrls: ['./class-detail.component.css']
})
export class ClassDetailComponent implements OnInit {
  classId = 0;
  classDetail: ClassDetailDto | null = null;
  loading = false;
  assigningSubject = false;
  searchingSubjects = false;

  showSubjectModal = false;
  subjectSearchTerm = '';
  subjectLookupResults: ClassAssignedSubjectDto[] = [];
  selectedSubject: ClassAssignedSubjectDto | null = null;

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
