import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { LearnerAcademicRecordDto, LearnerDetailDto } from '../learner-management/learner-management.models';
import { LearnerManagementService } from '../services/learner-management.service';

@Component({
  selector: 'app-learner-detail',
  templateUrl: './learner-detail.component.html',
  styleUrls: ['./learner-detail.component.css']
})
export class LearnerDetailComponent implements OnInit {
  learnerId = 0;
  learner: LearnerDetailDto | null = null;
  academicRecords: LearnerAcademicRecordDto[] = [];
  loading = false;
  errorMessage = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly learnerService: LearnerManagementService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = Number(params.get('id'));
      this.learnerId = Number.isFinite(id) ? id : 0;
      this.loadLearnerDetail();
    });
  }

  goBack(): void {
    window.history.back();
  }

  private loadLearnerDetail(): void {
    if (this.learnerId <= 0) {
      this.errorMessage = 'Invalid learner id.';
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    this.learnerService.getLearnerDetail(this.learnerId).subscribe({
      next: learner => {
        this.learner = learner;
        this.academicRecords = learner.academicRecords || [];
        this.loading = false;
      },
      error: err => {
        this.loading = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to load learner details.');
      }
    });
  }

  private mapHttpError(error: unknown, fallback: string): string {
    const httpError = error as HttpErrorResponse;

    if (!httpError || typeof httpError.status !== 'number') {
      return fallback;
    }

    if (httpError.status === 404) {
      return 'Learner not found.';
    }

    if (httpError.status === 0) {
      return 'Network error. Please check your connection and try again.';
    }

    if (httpError.status >= 500) {
      return 'Server error. Please try again in a moment.';
    }

    return fallback;
  }
}
