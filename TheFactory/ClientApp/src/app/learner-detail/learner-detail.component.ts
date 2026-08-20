import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { LearnerAcademicRecordDto, LearnerDetailDto, LearnerReportPreviewDto } from '../learner-management/learner-management.models';
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
  reportPreviews: LearnerReportPreviewDto[] = [];
  loading = false;
  loadingReports = false;
  previewVisible = false;
  previewTerm = '';
  previewYear = 0;
  previewUrl: SafeResourceUrl | null = null;
  previewUrlRaw = '';
  errorMessage = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly learnerService: LearnerManagementService,
    private readonly sanitizer: DomSanitizer
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
        this.loadAcademicRecords();
      },
      error: err => {
        this.loading = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to load learner details.');
      }
    });
  }

  private loadAcademicRecords(): void {
    this.learnerService.getLearnerAcademicRecords(this.learnerId).subscribe({
      next: records => {
        this.academicRecords = records || [];
        this.loadReportPreviews();
        this.loading = false;
      },
      error: err => {
        this.loading = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to load academic records.');
      }
    });
  }

  openReportPreview(item: LearnerReportPreviewDto): void {
    this.previewTerm = item.term;
    this.previewYear = item.year;
    const unsafeUrl = this.learnerService.getLearnerReportPreviewUrl(this.learnerId, item.term, item.year);
    this.previewUrlRaw = unsafeUrl;
    this.previewUrl = this.sanitizer.bypassSecurityTrustResourceUrl(unsafeUrl);
    this.previewVisible = true;
  }

  closeReportPreview(): void {
    this.previewVisible = false;
    this.previewUrl = null;
    this.previewUrlRaw = '';
    this.previewTerm = '';
    this.previewYear = 0;
  }

  downloadPreviewReport(): void {
    if (!this.previewTerm || !this.previewYear) {
      return;
    }

    this.learnerService.downloadLearnerTermReportPdf(this.learnerId, this.previewTerm, this.previewYear).subscribe({
      next: response => {
        const blob = response.body;
        if (!blob) {
          return;
        }

        const downloadUrl = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = downloadUrl;
        link.download = `Learner_${this.learnerId}_${this.previewTerm.replace(/\s+/g, '_')}_${this.previewYear}.pdf`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(downloadUrl);
      },
      error: err => {
        this.errorMessage = this.mapHttpError(err, 'Failed to download report.');
      }
    });
  }

  printPreviewReport(): void {
    if (!this.previewUrlRaw) {
      return;
    }

    const printWindow = window.open(this.previewUrlRaw, '_blank');
    if (!printWindow) {
      return;
    }

    printWindow.addEventListener('load', () => {
      printWindow.focus();
      printWindow.print();
    });
  }

  private loadReportPreviews(): void {
    this.loadingReports = true;
    this.learnerService.getLearnerReportPreviews(this.learnerId).subscribe({
      next: previews => {
        this.reportPreviews = previews || [];
        this.loadingReports = false;
      },
      error: err => {
        this.loadingReports = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to load learner reports.');
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
