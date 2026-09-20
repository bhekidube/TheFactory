import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter, forkJoin, Subscription } from 'rxjs';
import {
  ClassDto,
  CreateClassRequestDto,
  CreateLearnerRequestDto,
  GradeDto,
  LearnerLookupDto,
  LearnerDto,
  ParentGuardianDto,
  LearnerReportResponseDto,
  SubjectDto,
  SubjectScoreDto,
  SubjectScoreUpsertRequest,
  TeacherLookupDto,
  UpdateClassRequestDto
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
  gradeOptions: string[] = [];

  loadingLearners = false;
  loadingClasses = false;
  loadingGrades = false;
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
  editingClassId: number | null = null;
  isEditMode = false;
  editingLearnerId: number | null = null;
  classFormSubmitted = false;
  classNameTouched = false;
  learnerFormSubmitted = false;

  learnerForm: LearnerDto = this.getEmptyLearner();
  parentGuardianForm: ParentGuardianDto = this.getEmptyParentGuardian();
  scoreForm: SubjectScoreDto = this.getEmptyScore();
  relationshipOptions = ['Mother', 'Father', 'Guardian', 'Other'];

  errorMessage = '';
  successMessage = '';
  currentSection: 'learners' | 'reports' | 'classes' = 'learners';

  private routeSubscription?: Subscription;

  constructor(
    private readonly learnerService: LearnerManagementService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.setCurrentSectionFromRoute();
    this.routeSubscription = this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(() => this.setCurrentSectionFromRoute());

    this.loadLearners();
    this.loadClasses();
    this.loadGrades();
  }

  ngOnDestroy(): void {
    this.routeSubscription?.unsubscribe();
  }

  get isLearnersRoute(): boolean {
    return this.currentSection === 'learners' || this.currentSection === 'reports';
  }

  get isReportsRoute(): boolean {
    return this.currentSection === 'reports';
  }

  get isClassesRoute(): boolean {
    return this.currentSection === 'classes';
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

  get isClassNameValid(): boolean {
    return this.className.trim().length > 0;
  }

  get showClassNameValidationError(): boolean {
    return (this.classFormSubmitted || this.classNameTouched) && !this.isClassNameValid;
  }

  markClassNameTouched(): void {
    this.classNameTouched = true;
  }

  get isClassFormSubmittable(): boolean {
    if (this.creatingClass) {
      return false;
    }

    return this.isClassNameValid
      && this.selectedGrade.trim().length > 0
      && !!this.selectedTeacher
      && this.selectedTeacher.teacherId > 0;
  }

  private setCurrentSectionFromRoute(): void {
    const path = this.router.url.split('?')[0].toLowerCase();
    if (/\/learning\/schools\/\d+\/classes$/.test(path)) {
      this.currentSection = 'classes';
      return;
    }

    if (/\/learning\/schools\/\d+\/reports$/.test(path)) {
      this.currentSection = 'reports';
      return;
    }

    this.currentSection = 'learners';
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

  loadGrades(): void {
    this.loadingGrades = true;
    this.learnerService.getGrades().subscribe({
      next: (grades: GradeDto[]) => {
        this.gradeOptions = grades.map(grade => grade.name);
        this.loadingGrades = false;
      },
      error: err => {
        this.loadingGrades = false;
        this.errorMessage = this.mapHttpError(err, 'Failed to load grades.');
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
    this.editingLearnerId = null;
    this.learnerFormSubmitted = false;
    this.learnerForm = this.getEmptyLearner();
    this.parentGuardianForm = this.getEmptyParentGuardian();
    this.showLearnerForm = true;
  }

  openAddClass(): void {
    this.clearMessages();
    this.resetClassForm();
    this.showClassForm = true;
  }

  openEditClass(schoolClass: ClassDto): void {
    this.clearMessages();
    this.editingClassId = schoolClass.id;
    this.classFormSubmitted = false;
    this.classNameTouched = false;
    this.className = schoolClass.name;
    this.selectedGrade = schoolClass.grade;
    this.selectedTeacher = {
      teacherId: schoolClass.teacherId,
      name: schoolClass.teacherName,
      email: '',
      role: 'Teacher'
    };
    this.teacherSearchTerm = schoolClass.teacherName || `Staff #${schoolClass.teacherId}`;
    this.teacherLookupResults = [];
    this.selectedLearners = [];
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
    this.classFormSubmitted = true;
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
    const request = this.editingClassId
      ? this.learnerService.updateClass(this.editingClassId, {
        name: payload.name,
        grade: payload.grade,
        teacherId: payload.teacherId
      } as UpdateClassRequestDto)
      : this.learnerService.createClass(payload);

    request.subscribe({
      next: () => {
        this.creatingClass = false;
        this.showClassForm = false;
        this.successMessage = this.editingClassId ? 'Class updated successfully.' : 'Class created successfully.';
        this.editingClassId = null;
        this.resetClassForm();
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
    this.editingLearnerId = learner.id;
    this.learnerFormSubmitted = false;
    this.showLearnerForm = true;
    this.loadingLearnerDetail = true;

    if (!Number.isFinite(learner.id) || learner.id <= 0) {
      const fallback = this.normalizeLearnerResponse(learner);
      this.learnerForm = {
        ...fallback,
        id: 0,
        parentGuardian: fallback.parentGuardian || this.getEmptyParentGuardian()
      };
      this.parentGuardianForm = {
        ...this.getEmptyParentGuardian(),
        ...(fallback.parentGuardian || {})
      };
      this.loadingLearnerDetail = false;
      this.errorMessage = 'Unable to edit this learner because the learner id is missing.';
      return;
    }

    this.learnerService.getLearner(learner.id).subscribe({
      next: detail => {
        const normalized = this.normalizeLearnerResponse(detail);
        const resolvedId = normalized.id > 0 ? normalized.id : (this.editingLearnerId ?? 0);

        this.learnerForm = {
          ...normalized,
          id: resolvedId,
          parentGuardian: normalized.parentGuardian || this.getEmptyParentGuardian()
        };
        this.parentGuardianForm = {
          ...this.getEmptyParentGuardian(),
          ...(normalized.parentGuardian || {})
        };
        this.loadingLearnerDetail = false;
      },
      error: err => {
        const fallback = this.normalizeLearnerResponse(learner);
        this.learnerForm = {
          ...fallback,
          id: this.editingLearnerId ?? fallback.id,
          parentGuardian: fallback.parentGuardian || this.getEmptyParentGuardian()
        };
        this.parentGuardianForm = {
          ...this.getEmptyParentGuardian(),
          ...(fallback.parentGuardian || {})
        };
        this.loadingLearnerDetail = false;
        this.errorMessage = this.mapHttpError(err, 'Loaded learner data was incomplete, so the form was restored from the current list row.');
      }
    });
  }

  closeLearnerForm(): void {
    if (this.creatingLearner || this.updatingLearner || this.loadingLearnerDetail) {
      return;
    }

    this.showLearnerForm = false;
    this.editingLearnerId = null;
    this.learnerFormSubmitted = false;
    this.learnerForm = this.getEmptyLearner();
    this.parentGuardianForm = this.getEmptyParentGuardian();
  }

  saveLearner(): void {
    this.learnerFormSubmitted = true;
    this.clearMessages();

    const validationMessage = this.validateLearner(this.learnerForm, this.parentGuardianForm);
    if (validationMessage) {
      return;
    }

    const updatePayload: LearnerDto = {
      id: this.learnerForm.id,
      firstName: this.learnerForm.firstName.trim(),
      surname: this.learnerForm.surname.trim(),
      grade: this.learnerForm.grade.trim(),
      parentGuardian: {
        firstName: this.parentGuardianForm.firstName.trim(),
        surname: this.parentGuardianForm.surname.trim(),
        phoneNumber: this.normalizePhoneDigits(this.parentGuardianForm.phoneNumber),
        emailAddress: this.parentGuardianForm.emailAddress.trim(),
        relationshipToLearner: this.parentGuardianForm.relationshipToLearner.trim()
      }
    };

    const createPayload: CreateLearnerRequestDto = {
      firstName: this.learnerForm.firstName.trim(),
      surname: this.learnerForm.surname.trim(),
      grade: this.learnerForm.grade.trim(),
      parentGuardian: {
        firstName: this.parentGuardianForm.firstName.trim(),
        surname: this.parentGuardianForm.surname.trim(),
        phoneNumber: this.normalizePhoneDigits(this.parentGuardianForm.phoneNumber),
        emailAddress: this.parentGuardianForm.emailAddress.trim(),
        relationshipToLearner: this.parentGuardianForm.relationshipToLearner.trim()
      }
    };

    if (!this.isEditMode) {
      this.creatingLearner = true;
      this.learnerService.createLearner(createPayload).subscribe({
        next: () => {
          this.creatingLearner = false;
          this.showLearnerForm = false;
          this.learnerFormSubmitted = false;
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

    const learnerIdToUpdate = Number.isFinite(updatePayload.id) && updatePayload.id > 0
      ? updatePayload.id
      : (this.editingLearnerId ?? 0);

    if (!Number.isFinite(learnerIdToUpdate) || learnerIdToUpdate <= 0) {
      this.errorMessage = 'Unable to update learner: learner id is missing.';
      return;
    }

    updatePayload.id = learnerIdToUpdate;

    this.updatingLearner = true;
    this.learnerService.updateLearner(learnerIdToUpdate, updatePayload).subscribe({
      next: () => {
        this.updatingLearner = false;
        this.showLearnerForm = false;
        this.editingLearnerId = null;
        this.learnerFormSubmitted = false;
        this.successMessage = 'Learner updated successfully.';
        this.loadLearners();
        if (this.selectedLearner?.id === updatePayload.id) {
          this.selectedLearner = { ...updatePayload };
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

  get isParentEmailValid(): boolean {
    const value = this.parentGuardianForm.emailAddress.trim();
    if (!value) {
      return true;
    }

    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
  }

  get isParentPhoneValid(): boolean {
    const digitsOnly = this.normalizePhoneDigits(this.parentGuardianForm.phoneNumber);
    return /^(07\d{8}|2637\d{8})$/.test(digitsOnly);
  }

  private validateLearner(learner: LearnerDto, parentGuardian: ParentGuardianDto): string {
    if (!learner.firstName?.trim()) {
      return 'First name is required.';
    }

    if (!learner.surname?.trim()) {
      return 'Surname is required.';
    }

    if (!learner.grade?.trim()) {
      return 'Grade is required.';
    }

    if (!parentGuardian.firstName?.trim()) {
      return 'Parent first name is required.';
    }

    if (!parentGuardian.surname?.trim()) {
      return 'Parent surname is required.';
    }

    if (!this.isParentPhoneValid) {
      return 'Parent phone number must be a valid Zimbabwe mobile number (07XXXXXXXX or 2637XXXXXXXX).';
    }

    if (!this.isParentEmailValid) {
      return 'Parent email address format is invalid.';
    }

    if (!parentGuardian.relationshipToLearner?.trim()) {
      return 'Relationship to learner is required.';
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

  private normalizeLearnerResponse(raw: any): LearnerDto {
    const source = raw ?? {};
    const parentSource = source.parentGuardian ?? source.ParentGuardian ?? {};
    const idValue = Number(source.id ?? source.learnerId ?? 0);

    return {
      id: Number.isFinite(idValue) ? idValue : 0,
      firstName: String(source.firstName ?? source.FirstName ?? ''),
      surname: String(source.surname ?? source.Surname ?? ''),
      grade: String(source.grade ?? source.Grade ?? ''),
      parentGuardian: {
        firstName: String(parentSource.firstName ?? parentSource.FirstName ?? ''),
        surname: String(parentSource.surname ?? parentSource.Surname ?? ''),
        phoneNumber: String(parentSource.phoneNumber ?? parentSource.PhoneNumber ?? ''),
        emailAddress: String(parentSource.emailAddress ?? parentSource.EmailAddress ?? ''),
        relationshipToLearner: String(parentSource.relationshipToLearner ?? parentSource.RelationshipToLearner ?? '')
      }
    };
  }

  private getEmptyLearner(): LearnerDto {
    return {
      id: 0,
      firstName: '',
      surname: '',
      grade: '',
      parentGuardian: this.getEmptyParentGuardian()
    };
  }

  private getEmptyParentGuardian(): ParentGuardianDto {
    return {
      firstName: '',
      surname: '',
      phoneNumber: '',
      emailAddress: '',
      relationshipToLearner: ''
    };
  }

  private normalizePhoneDigits(phoneNumber: string): string {
    return (phoneNumber || '').replace(/\D/g, '');
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
    this.classFormSubmitted = false;
    this.classNameTouched = false;
    this.className = '';
    this.editingClassId = null;
    this.selectedGrade = '';
    this.teacherSearchTerm = '';
    this.learnerLookupSearchTerm = '';
    this.teacherLookupResults = [];
    this.learnerLookupResults = [];
    this.selectedTeacher = null;
    this.selectedLearners = [];
  }
}
