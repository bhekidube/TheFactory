import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { SaveWorkMarksRequestDto, WorkDetailDto, WorkLearnerMarkDto } from '../learner-management/learner-management.models';
import { LearnerManagementService } from '../services/learner-management.service';

@Component({
  selector: 'app-work-detail',
  templateUrl: './work-detail.component.html',
  styleUrls: ['./work-detail.component.css']
})
export class WorkDetailComponent implements OnInit {
  classId = 0;
  workId = 0;
  workDetail: WorkDetailDto | null = null;

  loading = false;
  saving = false;

  errorMessage = '';
  successMessage = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly learnerService: LearnerManagementService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const classId = Number(params.get('classId'));
      const workId = Number(params.get('workId'));
      this.classId = Number.isFinite(classId) ? classId : 0;
      this.workId = Number.isFinite(workId) ? workId : 0;
      this.loadWorkDetail();
    });
  }

  loadWorkDetail(): void {
    if (this.classId <= 0 || this.workId <= 0) {
      this.errorMessage = 'Invalid class or work id.';
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    this.learnerService.getClassWorkDetail(this.classId, this.workId).subscribe({
      next: detail => {
        this.workDetail = {
          ...detail,
          learners: (detail.learners || []).map(learner => ({
            ...learner,
            comment: learner.comment || ''
          }))
        };
        this.loading = false;
      },
      error: err => {
        this.loading = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to load work details.');
      }
    });
  }

  backToClassDetail(): void {
    this.router.navigate(['/school/admin/classes', this.classId]);
  }

  saveMarks(): void {
    this.clearMessages();

    if (!this.workDetail || this.saving) {
      return;
    }

    const validationError = this.validateMarks(this.workDetail.learners, this.workDetail.totalMark);
    if (validationError) {
      this.errorMessage = validationError;
      return;
    }

    const payload: SaveWorkMarksRequestDto = {
      marks: this.workDetail.learners.map(learner => ({
        learnerId: learner.learnerId,
        markObtained: learner.markObtained,
        comment: learner.comment || ''
      }))
    };

    this.saving = true;
    this.learnerService.saveClassWorkMarks(this.classId, this.workId, payload).subscribe({
      next: () => {
        this.saving = false;
        this.successMessage = 'Marks saved successfully.';
      },
      error: err => {
        this.saving = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to save marks.');
      }
    });
  }

  trackByLearnerId(_: number, learner: WorkLearnerMarkDto): number {
    return learner.learnerId;
  }

  private validateMarks(learners: WorkLearnerMarkDto[], totalMark: number): string {
    if (!Number.isFinite(totalMark) || totalMark <= 0) {
      return 'Work total mark is invalid.';
    }

    for (const learner of learners) {
      if (learner.markObtained === null || learner.markObtained === undefined) {
        continue;
      }

      if (!Number.isFinite(learner.markObtained)) {
        return `Invalid mark entered for ${learner.learnerName}.`;
      }

      if (learner.markObtained < 0) {
        return `Mark obtained cannot be negative for ${learner.learnerName}.`;
      }

      if (learner.markObtained > totalMark) {
        return `Mark obtained for ${learner.learnerName} cannot exceed total mark (${totalMark}).`;
      }
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

      if (httpError.error && typeof httpError.error.title === 'string') {
        const details = this.extractValidationErrors(httpError.error.errors);
        return details.length > 0
          ? `${httpError.error.title}: ${details.join(' | ')}`
          : httpError.error.title;
      }

      return 'Request validation failed. Please review your input.';
    }

    if (httpError.status === 404) {
      return 'Work item or class was not found.';
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

  private extractValidationErrors(errors: unknown): string[] {
    if (!errors || typeof errors !== 'object') {
      return [];
    }

    const entries = Object.entries(errors as Record<string, unknown>);
    const messages: string[] = [];

    for (const [field, value] of entries) {
      if (!Array.isArray(value)) {
        continue;
      }

      const fieldMessages = value
        .filter(item => typeof item === 'string')
        .map(item => item.trim())
        .filter(item => item.length > 0);

      if (fieldMessages.length === 0) {
        continue;
      }

      messages.push(`${field}: ${fieldMessages.join(', ')}`);
    }

    return messages;
  }
}
