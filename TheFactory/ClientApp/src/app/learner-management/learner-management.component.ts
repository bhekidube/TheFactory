import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { forkJoin } from 'rxjs';
import {
  ClassDto,
  CreateClassRequestDto,
  LearnerLookupDto,
  LearnerDto,
  LearnerReportResponseDto,
  SubjectDto,
  SubjectScoreDto,
  SubjectScoreUpsertRequest,
  TeacherLookupDto
} from './learner-management.models';
import { LearnerManagementService } from '../services/learner-management.service';

@Component({
  selector: 'app-learner-management',
  templateUrl: './learner-management.component.html',
  styleUrls: ['./learner-management.component.css']
})
export class LearnerManagementComponent implements OnInit {
  learners: LearnerDto[] = [];
  filteredLearners: LearnerDto[] = [];
  classes: ClassDto[] = [];
  searchTerm = '';
  classSearchTerm = '';
  teacherSearchTerm = '';
  learnerLookupSearchTerm = '';
  teacherLookupResults: TeacherLookupDto[] = [];
  learnerLookupResults: LearnerLookupDto[] = [];
  selectedTeacher: TeacherLookupDto | null = null;
  selectedLearners: LearnerLookupDto[] = [];
  selectedGrade = '';
  className = '';
  gradeOptions = ['Baby Class', 'Middle Class', 'ECD A', 'ECD B', 'Grade 1', 'Grade 2', 'Grade 3', 'Grade 4'];

  loadingLearners = false;
  loadingClasses = false;
  loadingLearnerDetail = false;
  loadingReport = false;
  loadingSubjects = false;
  searchingTeachers = false;
  searchingLearnersLookup = false;
  creatingClass = false;
  creatingLearner = false;
  updatingLearner = false;
  archivingLearnerId: number | null = null;
  savingSubjectScore = false;
  downloadingPdf = false;

  selectedLearner: LearnerDto | null = null;
  selectedReport: LearnerReportResponseDto | null = null;
  subjects: SubjectDto[] = [];

  showLearnerForm = false;
  showReportPanel = false;
  showClassForm = false;
  isEditMode = false;

  learnerForm: LearnerDto = this.getEmptyLearner();
  scoreForm: SubjectScoreDto = this.getEmptyScore();

  errorMessage = '';
  successMessage = '';

  constructor(private readonly learnerService: LearnerManagementService) {}

  ngOnInit(): void {
    this.loadLearners();
    this.loadClasses();
  }

  get activeLearnersCount(): number {
    return this.learners.length;
  }

  get learnersWithReportsCount(): number {
    return this.learners.filter(learner => learner.id > 0).length;
  }

  get filteredClasses(): ClassDto[] {
    const term = this.classSearchTerm.trim().toLowerCase();
    if (!term) {
      return [...this.classes];
    }

    return this.classes.filter(schoolClass =>
      schoolClass.name.toLowerCase().includes(term)
      || schoolClass.grade.toLowerCase().includes(term)
      || schoolClass.teacherName.toLowerCase().includes(term)
      || schoolClass.id.toString().includes(term)
    );
  }

  get overallAverage(): number {
    if (!this.selectedReport) {
      return 0;
    }

    return this.selectedReport.average;
  }

  loadLearners(): void {
    this.loadingLearners = true;
    this.errorMessage = '';

    this.learnerService.getLearners().subscribe({
      next: learners => {
        this.learners = learners;
        this.applySearch();
        this.loadingLearners = false;
      },
      error: err => {
        this.loadingLearners = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to load learners.');
      }
    });
  }

  loadClasses(): void {
    this.loadingClasses = true;
    this.errorMessage = '';

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

  applySearch(): void {
    const term = this.searchTerm.trim().toLowerCase();

    if (!term) {
      this.filteredLearners = [...this.learners];
      return;
    }

    this.filteredLearners = this.learners.filter(learner => {
      const fullName = `${learner.firstName} ${learner.surname}`.toLowerCase();
      return (
        learner.firstName.toLowerCase().includes(term) ||
        learner.surname.toLowerCase().includes(term) ||
        fullName.includes(term) ||
        learner.id.toString().includes(term) ||
        learner.grade.toLowerCase().includes(term)
      );
    });
  }

  openAddLearner(): void {
    this.clearMessages();
    this.isEditMode = false;
    this.learnerForm = this.getEmptyLearner();
    this.showLearnerForm = true;
  }

  openAddClass(): void {
    this.clearMessages();
    this.resetClassForm();
    this.showClassForm = true;
  }

  closeClassForm(): void {
    if (this.creatingClass || this.searchingTeachers || this.searchingLearnersLookup) {
      return;
    }

    this.showClassForm = false;
    this.resetClassForm();
  }

  searchTeachers(): void {
    const term = this.teacherSearchTerm.trim();
    if (term.length < 2) {
      this.teacherLookupResults = [];
      return;
    }

    this.searchingTeachers = true;
    this.learnerService.searchTeachers(term).subscribe({
      next: results => {
        this.teacherLookupResults = results;
        this.searchingTeachers = false;
      },
      error: err => {
        this.searchingTeachers = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to search teachers.');
      }
    });
  }

  selectTeacher(teacher: TeacherLookupDto): void {
    this.selectedTeacher = teacher;
    this.teacherSearchTerm = `${teacher.name} (${teacher.email})`;
    this.teacherLookupResults = [];
  }

  searchLearnersForLookup(): void {
    const term = this.learnerLookupSearchTerm.trim();
    if (term.length < 2) {
      this.learnerLookupResults = [];
      return;
    }

    this.searchingLearnersLookup = true;
    this.learnerService.searchLearnersForLookup(term).subscribe({
      next: results => {
        const selectedIds = new Set(this.selectedLearners.map(item => item.learnerId));
        this.learnerLookupResults = results.filter(item => !selectedIds.has(item.learnerId));
        this.searchingLearnersLookup = false;
      },
      error: err => {
        this.searchingLearnersLookup = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to search learners.');
      }
    });
  }

  addLearnerToClass(learner: LearnerLookupDto): void {
    if (this.selectedLearners.some(item => item.learnerId === learner.learnerId)) {
      return;
    }

    this.selectedLearners = [...this.selectedLearners, learner];
    this.learnerLookupResults = this.learnerLookupResults.filter(item => item.learnerId !== learner.learnerId);
    this.learnerLookupSearchTerm = '';
  }

  removeLearnerFromClass(learnerId: number): void {
    this.selectedLearners = this.selectedLearners.filter(item => item.learnerId !== learnerId);
  }

  saveClass(): void {
    this.clearMessages();

    const validationError = this.validateClassForm();
    if (validationError) {
      this.errorMessage = validationError;
      return;
    }

    if (!this.selectedTeacher) {
      this.errorMessage = 'Teacher is required.';
      return;
    }

    const payload: CreateClassRequestDto = {
      name: this.className.trim(),
      grade: this.selectedGrade.trim(),
      teacherId: this.selectedTeacher.teacherId,
      learnerIds: this.selectedLearners.map(item => item.learnerId)
    };

    this.creatingClass = true;
    this.learnerService.createClass(payload).subscribe({
      next: () => {
        this.creatingClass = false;
        this.showClassForm = false;
        this.successMessage = 'Class created successfully.';
        this.loadClasses();
      },
      error: err => {
        this.creatingClass = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to create class.');
      }
    });
  }

  openEditLearner(learner: LearnerDto): void {
    this.clearMessages();
    this.isEditMode = true;
    this.showLearnerForm = true;
    this.loadingLearnerDetail = true;

    this.learnerService.getLearner(learner.id).subscribe({
      next: detail => {
        this.learnerForm = { ...detail };
        this.loadingLearnerDetail = false;
      },
      error: err => {
        this.loadingLearnerDetail = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to load learner details.');
      }
    });
  }

  closeLearnerForm(): void {
    if (this.creatingLearner || this.updatingLearner || this.loadingLearnerDetail) {
      return;
    }

    this.showLearnerForm = false;
    this.learnerForm = this.getEmptyLearner();
  }

  saveLearner(): void {
    this.clearMessages();

    const validationMessage = this.validateLearner(this.learnerForm);
    if (validationMessage) {
      this.errorMessage = validationMessage;
      return;
    }

    const payload: LearnerDto = {
      id: this.learnerForm.id,
      firstName: this.learnerForm.firstName.trim(),
      surname: this.learnerForm.surname.trim(),
      grade: this.learnerForm.grade.trim()
    };

    if (!this.isEditMode) {
      this.creatingLearner = true;
      this.learnerService.createLearner(payload).subscribe({
        next: () => {
          this.creatingLearner = false;
          this.showLearnerForm = false;
          this.successMessage = 'Learner created successfully.';
          this.loadLearners();
        },
        error: err => {
          this.creatingLearner = false;
          this.errorMessage = this.mapHttpError(err, 'Failed to create learner.');
        }
      });

      return;
    }

    this.updatingLearner = true;
    this.learnerService.updateLearner(payload.id, payload).subscribe({
      next: () => {
        this.updatingLearner = false;
        this.showLearnerForm = false;
        this.successMessage = 'Learner updated successfully.';
        this.loadLearners();
        if (this.selectedLearner?.id === payload.id) {
          this.selectedLearner = { ...payload };
        }
      },
      error: err => {
        this.updatingLearner = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to update learner.');
      }
    });
  }

  archiveLearner(learner: LearnerDto): void {
    this.clearMessages();

    const confirmed = confirm(
      'Archive this learner?\n\nThis learner will be removed from the active learner list while their records are retained.'
    );

    if (!confirmed) {
      return;
    }

    this.archivingLearnerId = learner.id;
    this.learnerService.archiveLearner(learner.id).subscribe({
      next: () => {
        this.archivingLearnerId = null;
        this.successMessage = 'Learner archived successfully.';

        if (this.selectedLearner?.id === learner.id) {
          this.closeReportPanel();
        }

        this.loadLearners();
      },
      error: err => {
        this.archivingLearnerId = null;
        this.errorMessage = this.mapHttpError(err, 'Failed to archive learner.');
      }
    });
  }

  viewReport(learner: LearnerDto): void {
    this.clearMessages();
    this.selectedLearner = learner;
    this.showReportPanel = true;
    this.loadingReport = true;
    this.loadingSubjects = true;

    forkJoin({
      report: this.learnerService.getReport(learner.id),
      subjects: this.learnerService.getSubjects()
    }).subscribe({
      next: ({ report, subjects }) => {
        this.selectedReport = report;
        this.subjects = subjects;
        this.scoreForm = this.getEmptyScore();
        this.loadingReport = false;
        this.loadingSubjects = false;
      },
      error: err => {
        this.loadingReport = false;
        this.loadingSubjects = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to load learner report.');
      }
    });
  }

  closeReportPanel(): void {
    if (this.loadingReport || this.savingSubjectScore || this.downloadingPdf) {
      return;
    }

    this.showReportPanel = false;
    this.selectedLearner = null;
    this.selectedReport = null;
    this.subjects = [];
    this.scoreForm = this.getEmptyScore();
  }

  downloadReportPdf(): void {
    if (!this.selectedLearner || this.downloadingPdf) {
      return;
    }

    this.clearMessages();
    this.downloadingPdf = true;

    this.learnerService.downloadReportPdf(this.selectedLearner.id).subscribe({
      next: response => {
        const blob = response.body;
        if (!blob) {
          this.downloadingPdf = false;
          this.errorMessage = 'The PDF response was empty.';
          return;
        }

        const fileName = this.resolveFileName(response.headers.get('content-disposition'));
        const blobUrl = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = blobUrl;
        anchor.download = fileName;
        document.body.appendChild(anchor);
        anchor.click();
        document.body.removeChild(anchor);
        URL.revokeObjectURL(blobUrl);

        this.downloadingPdf = false;
        this.successMessage = 'Report download started.';
      },
      error: err => {
        this.downloadingPdf = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to download learner report.');
      }
    });
  }

  saveSubjectScore(): void {
    if (!this.selectedLearner) {
      return;
    }

    this.clearMessages();

    const validationMessage = this.validateScore(this.scoreForm);
    if (validationMessage) {
      this.errorMessage = validationMessage;
      return;
    }

    this.savingSubjectScore = true;
    const subjectId = Number(this.scoreForm.subjectId);
    const selectedSubject = this.subjects.find(subject => subject.id === subjectId);

    const payload: SubjectScoreUpsertRequest = {
      learnerId: this.selectedLearner.id,
      subjectId,
      possibleMark: Number(this.scoreForm.possibleMark),
      pupilMark: Number(this.scoreForm.pupilMark),
      score: Number(this.scoreForm.pupilMark),
      grade: this.scoreForm.grade.trim(),
      teacherComments: this.scoreForm.teacherComments.trim()
    };

    this.learnerService.upsertSubjectScore(this.selectedLearner.id, payload).subscribe({
      next: () => {
        this.savingSubjectScore = false;
        this.successMessage = 'Subject score saved successfully.';
        this.scoreForm = this.getEmptyScore();
        this.viewReport(this.selectedLearner as LearnerDto);
      },
      error: err => {
        this.savingSubjectScore = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to save subject score.');
      }
    });
  }

  editSubjectScore(subject: SubjectScoreDto): void {
    this.clearMessages();
    this.scoreForm = {
      learnerId: this.selectedLearner?.id,
      subjectId: subject.subjectId,
      subject: subject.subject,
      possibleMark: subject.possibleMark,
      pupilMark: subject.pupilMark,
      grade: subject.grade,
      teacherComments: subject.teacherComments,
      score: subject.score ?? subject.pupilMark
    };
  }

  clearSubjectScoreForm(): void {
    this.scoreForm = this.getEmptyScore();
  }

  get hasSubjects(): boolean {
    return this.subjects.length > 0;
  }

  get isEditingSubjectScore(): boolean {
    return this.scoreForm.subjectId > 0
      && this.selectedReport?.subjects.some(subject => subject.subjectId === this.scoreForm.subjectId) === true;
  }

  trackByLearnerId(_: number, learner: LearnerDto): number {
    return learner.id;
  }

  trackByClassId(_: number, schoolClass: ClassDto): number {
    return schoolClass.id;
  }

  private validateLearner(learner: LearnerDto): string {
    if (!learner.firstName?.trim()) {
      return 'First name is required.';
    }

    if (!learner.surname?.trim()) {
      return 'Surname is required.';
    }

    if (!learner.grade?.trim()) {
      return 'Grade is required.';
    }

    return '';
  }

  private validateClassForm(): string {
    if (!this.className.trim()) {
      return 'Class name is required.';
    }

    if (!this.selectedGrade.trim()) {
      return 'Class grade is required.';
    }

    if (!this.selectedTeacher || this.selectedTeacher.teacherId <= 0) {
      return 'Teacher is required.';
    }

    return '';
  }

  private validateScore(score: SubjectScoreDto): string {
    const subjectId = Number(score.subjectId);
    if (!Number.isFinite(subjectId) || subjectId <= 0) {
      return 'Subject is required.';
    }

    const possibleMark = Number(score.possibleMark);
    const pupilMark = Number(score.pupilMark);

    if (!Number.isFinite(possibleMark) || possibleMark <= 0) {
      return 'Possible mark must be greater than 0.';
    }

    if (!Number.isFinite(pupilMark) || pupilMark < 0 || pupilMark > possibleMark) {
      return 'Pupil mark must be between 0 and possible mark.';
    }

    return '';
  }

  private getEmptyLearner(): LearnerDto {
    return {
      id: 0,
      firstName: '',
      surname: '',
      grade: ''
    };
  }

  private getEmptyScore(): SubjectScoreDto {
    return {
      learnerId: this.selectedLearner?.id,
      subjectId: 0,
      subject: '',
      possibleMark: 100,
      pupilMark: 0,
      grade: '',
      teacherComments: ''
    };
  }

  private mapHttpError(error: unknown, fallback: string): string {
    const httpError = error as HttpErrorResponse;

    if (!httpError || typeof httpError.status !== 'number') {
      return fallback;
    }

    if (httpError.status === 400) {
      const apiMessage = this.tryResolveApiMessage(httpError.error);
      return apiMessage || 'Request validation failed. Please review your input.';
    }

    if (httpError.status === 404) {
      return 'The requested learner or report could not be found.';
    }

    if (httpError.status === 0) {
      return 'Network error. Please check your connection and try again.';
    }

    if (httpError.status >= 500) {
      return 'Server error. Please try again in a moment.';
    }

    return fallback;
  }

  private tryResolveApiMessage(errorPayload: unknown): string {
    if (!errorPayload) {
      return '';
    }

    if (typeof errorPayload === 'string') {
      return errorPayload;
    }

    if (typeof errorPayload === 'object' && errorPayload !== null) {
      const maybeError = errorPayload as { error?: unknown; message?: unknown };
      if (typeof maybeError.error === 'string') {
        return maybeError.error;
      }

      if (typeof maybeError.message === 'string') {
        return maybeError.message;
      }
    }

    return '';
  }

  private resolveFileName(contentDisposition: string | null): string {
    if (!contentDisposition) {
      return this.defaultFileName();
    }

    const fileNameMatch = /filename\*?=(?:UTF-8''|\")?([^;\"]+)/i.exec(contentDisposition);
    if (!fileNameMatch || !fileNameMatch[1]) {
      return this.defaultFileName();
    }

    return decodeURIComponent(fileNameMatch[1].trim().replace(/\"/g, ''));
  }

  private defaultFileName(): string {
    if (!this.selectedLearner) {
      return 'Learner_Report.pdf';
    }

    return `${this.selectedLearner.firstName}_${this.selectedLearner.surname}_Report.pdf`;
  }

  private clearMessages(): void {
    this.errorMessage = '';
    this.successMessage = '';
  }

  private resetClassForm(): void {
    this.className = '';
    this.selectedGrade = '';
    this.teacherSearchTerm = '';
    this.learnerLookupSearchTerm = '';
    this.teacherLookupResults = [];
    this.learnerLookupResults = [];
    this.selectedTeacher = null;
    this.selectedLearners = [];
  }
}
