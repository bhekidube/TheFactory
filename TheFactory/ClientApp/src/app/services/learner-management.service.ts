import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpResponse } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  ClassAssignedSubjectDto,
  ClassDetailDto,
  ClassDto,
  CreateClassRequestDto,
  LearnerLookupDto,
  LearnerDto,
  LearnerDetailDto,
  LearnerAcademicRecordDto,
  LearnerReportResponseDto,
  SubjectUpsertRequestDto,
  TeacherLookupDto,
  SubjectDto,
  WorkDto,
  WorkDetailDto,
  WorkLearnerMarkDto,
  SaveWorkMarksRequestDto,
  WorkUpsertRequestDto,
  WorkTypeLookupDto,
  SubjectScoreDto,
  SubjectScoreUpsertRequest
} from '../learner-management/learner-management.models';

export interface UserRoleDto {
  userRoleId: number;
  name: string;
}

export interface CreateSchoolRequest {
  name: string;
  logoUrl?: string;
}

export interface AssignUserRoleRequest {
  userId: number;
  userRoleId: number;
}

export interface UserLookupDto {
  userId: number;
  name: string;
  email: string;
}

@Injectable({
  providedIn: 'root'
})
export class LearnerManagementService {
  private readonly baseUrl = `${environment.apiBaseUrl}/api`;

  constructor(private readonly http: HttpClient) {}

  getSchools(): Observable<Array<{ id: number; name: string }>> {
    return this.http.get<Array<{ id: number; name: string }>>(`${this.baseUrl}/schools`);
  }

  createSchool(payload: CreateSchoolRequest): Observable<{ id: number; name: string }> {
    return this.http.post<{ id: number; name: string }>(`${this.baseUrl}/schools`, payload, this.getAdminRequestOptions());
  }

  getUserRoles(): Observable<UserRoleDto[]> {
    return this.http.get<UserRoleDto[]>(`${this.baseUrl}/userroles`, this.getAdminRequestOptions());
  }

  searchUsers(query: string): Observable<UserLookupDto[]> {
    return this.http.get<UserLookupDto[]>(
      `${this.baseUrl}/userroles/users?query=${encodeURIComponent(query)}`,
      this.getAdminRequestOptions()
    );
  }

  assignUserRole(payload: AssignUserRoleRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/userroles/assign`, payload, this.getAdminRequestOptions());
  }

  setSelectedTenantId(tenantId: number): void {
    localStorage.setItem('selectedTenantId', tenantId.toString());
  }

  clearSelectedTenantId(): void {
    localStorage.removeItem('selectedTenantId');
  }

  getLearners(): Observable<LearnerDto[]> {
    return this.http.get<LearnerDto[]>(`${this.baseUrl}/learners`, this.getRequestOptions());
  }

  getClasses(): Observable<ClassDto[]> {
    return this.http.get<ClassDto[]>(`${this.baseUrl}/classes`, this.getRequestOptions());
  }

  createClass(payload: CreateClassRequestDto): Observable<ClassDto> {
    return this.http.post<ClassDto>(`${this.baseUrl}/classes`, payload, this.getRequestOptions());
  }

  getClassById(id: number): Observable<ClassDetailDto> {
    return this.http.get<ClassDetailDto>(`${this.baseUrl}/classes/${id}`, this.getRequestOptions());
  }

  searchTeachers(query: string): Observable<TeacherLookupDto[]> {
    return this.http.get<TeacherLookupDto[]>(
      `${this.baseUrl}/teachers/search?q=${encodeURIComponent(query)}`,
      this.getRequestOptions()
    );
  }

  searchLearnersForLookup(query: string): Observable<LearnerLookupDto[]> {
    return this.http.get<LearnerLookupDto[]>(
      `${this.baseUrl}/learners/search?q=${encodeURIComponent(query)}`,
      this.getRequestOptions()
    );
  }

  searchSubjects(query: string): Observable<ClassAssignedSubjectDto[]> {
    return this.http.get<ClassAssignedSubjectDto[]>(
      `${this.baseUrl}/subjects/search?q=${encodeURIComponent(query)}`,
      this.getRequestOptions()
    );
  }

  assignSubjectToClass(classId: number, subjectId: number): Observable<ClassAssignedSubjectDto> {
    return this.http.post<ClassAssignedSubjectDto>(
      `${this.baseUrl}/classes/${classId}/subjects`,
      { subjectId },
      this.getRequestOptions()
    );
  }

  getSubjectsForCurriculum(): Observable<SubjectDto[]> {
    return this.http.get<SubjectDto[]>(`${this.baseUrl}/subjects`, this.getRequestOptions());
  }

  createSubject(payload: SubjectUpsertRequestDto): Observable<SubjectDto> {
    return this.http.post<SubjectDto>(`${this.baseUrl}/subjects`, payload, this.getRequestOptions());
  }

  updateSubject(id: number, payload: SubjectUpsertRequestDto): Observable<SubjectDto> {
    return this.http.post<SubjectDto>(`${this.baseUrl}/subjects/${id}`, payload, this.getRequestOptions());
  }

  getWorkItems(): Observable<WorkDto[]> {
    return this.http.get<WorkDto[]>(`${this.baseUrl}/work`, this.getRequestOptions())
      .pipe(map(items => this.normalizeWorkList(items)));
  }

  getWorkTypes(): Observable<WorkTypeLookupDto[]> {
    return this.http.get<WorkTypeLookupDto[]>(`${this.baseUrl}/work-types`, this.getRequestOptions());
  }

  createWorkItem(payload: WorkUpsertRequestDto): Observable<WorkDto> {
    return this.http.post<WorkDto>(`${this.baseUrl}/work`, payload, this.getRequestOptions())
      .pipe(map(item => this.normalizeWorkItem(item)));
  }

  updateWorkItem(id: number, payload: WorkUpsertRequestDto): Observable<WorkDto> {
    return this.http.put<WorkDto>(`${this.baseUrl}/work/${id}`, payload, this.getRequestOptions())
      .pipe(map(item => this.normalizeWorkItem(item)));
  }

  archiveWorkItem(id: number): Observable<unknown> {
    return this.http.delete(`${this.baseUrl}/work/${id}`, this.getRequestOptions());
  }

  getClassWorkItems(classId: number): Observable<WorkDto[]> {
    return this.http.get<WorkDto[]>(`${this.baseUrl}/classes/${classId}/work`, this.getRequestOptions())
      .pipe(map(items => this.normalizeWorkList(items)));
  }

  createClassWorkItem(classId: number, payload: WorkUpsertRequestDto): Observable<WorkDto> {
    return this.http.post<WorkDto>(`${this.baseUrl}/classes/${classId}/work`, payload, this.getRequestOptions())
      .pipe(map(item => this.normalizeWorkItem(item)));
  }

  updateClassWorkItem(classId: number, workId: number, payload: WorkUpsertRequestDto): Observable<WorkDto> {
    return this.http.put<WorkDto>(`${this.baseUrl}/classes/${classId}/work/${workId}`, payload, this.getRequestOptions())
      .pipe(map(item => this.normalizeWorkItem(item)));
  }

  archiveClassWorkItem(classId: number, workId: number): Observable<unknown> {
    return this.http.delete(`${this.baseUrl}/classes/${classId}/work/${workId}`, this.getRequestOptions());
  }

  getClassWorkDetail(classId: number, workId: number): Observable<WorkDetailDto> {
    return this.http.get<WorkDetailDto>(`${this.baseUrl}/classes/${classId}/work/${workId}`, this.getRequestOptions())
      .pipe(map(detail => this.normalizeWorkDetail(detail)));
  }

  saveClassWorkMarks(classId: number, workId: number, payload: SaveWorkMarksRequestDto): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/classes/${classId}/work/${workId}/marks`, payload, this.getRequestOptions());
  }

  getLearner(id: number): Observable<LearnerDto> {
    return this.http.get<LearnerDto>(`${this.baseUrl}/learners/${id}`, this.getRequestOptions());
  }

  getLearnerDetail(id: number): Observable<LearnerDetailDto> {
    return this.http.get<LearnerDetailDto>(`${this.baseUrl}/learners/${id}`, this.getRequestOptions());
  }

  getLearnerAcademicRecords(id: number): Observable<LearnerAcademicRecordDto[]> {
    return this.http.get<LearnerAcademicRecordDto[]>(`${this.baseUrl}/learners/${id}/academic-records`, this.getRequestOptions());
  }

  createLearner(payload: LearnerDto): Observable<LearnerDto> {
    return this.http.post<LearnerDto>(`${this.baseUrl}/learners`, payload, this.getRequestOptions());
  }

  updateLearner(id: number, payload: LearnerDto): Observable<LearnerDto> {
    return this.http.put<LearnerDto>(`${this.baseUrl}/learners/${id}`, payload, this.getRequestOptions());
  }

  archiveLearner(id: number): Observable<unknown> {
    return this.http.delete(`${this.baseUrl}/learners/${id}`, this.getRequestOptions());
  }

  getReport(learnerId: number): Observable<LearnerReportResponseDto> {
    return this.http.get<LearnerReportResponseDto>(`${this.baseUrl}/reports/${learnerId}`, this.getRequestOptions());
  }

  getSubjects(): Observable<SubjectDto[]> {
    return this.http.get<SubjectDto[]>(`${this.baseUrl}/Lookup/Subjects`, this.getRequestOptions());
  }

  downloadReportPdf(learnerId: number): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.baseUrl}/reports/${learnerId}/pdf`, {
      observe: 'response',
      responseType: 'blob',
      headers: this.getHeaders()
    });
  }

  upsertSubjectScore(learnerId: number, payload: SubjectScoreUpsertRequest): Observable<SubjectScoreDto> {
    return this.http.post<SubjectScoreDto>(`${this.baseUrl}/reports/${learnerId}/scores`, payload, this.getRequestOptions());
  }

  private getRequestOptions(): { headers: HttpHeaders } {
    return { headers: this.getHeaders() };
  }

  private getAdminRequestOptions(): { headers: HttpHeaders } {
    const baseHeaders = this.getHeaders();
    const userRole = localStorage.getItem('userRole') || '';
    return {
      headers: baseHeaders.set('X-User-Role', userRole)
    };
  }

  private getHeaders(): HttpHeaders {
    const tenantId = localStorage.getItem('selectedTenantId');
    if (!tenantId) {
      return new HttpHeaders();
    }

    return new HttpHeaders({
      'X-Tenant-Id': tenantId
    });
  }

  private normalizeWorkList(items: WorkDto[] | null | undefined): WorkDto[] {
    if (!Array.isArray(items)) {
      return [];
    }

    return items.map(item => this.normalizeWorkItem(item));
  }

  private normalizeWorkItem(item: WorkDto): WorkDto {
    const raw = item as unknown as Record<string, unknown>;
    const totalMarkRaw = raw.totalMark ?? raw.total_mark ?? raw.maxScore ?? 0;
    const totalMark = Number(totalMarkRaw);

    return {
      ...item,
      totalMark: Number.isFinite(totalMark) ? totalMark : 0
    };
  }

  private normalizeWorkDetail(detail: WorkDetailDto): WorkDetailDto {
    const raw = detail as unknown as Record<string, unknown>;
    const totalMarkRaw = raw.totalMark ?? raw.total_mark ?? raw.maxScore ?? 0;
    const totalMark = Number(totalMarkRaw);

    const learners = Array.isArray(detail.learners)
      ? detail.learners.map(learner => this.normalizeWorkDetailLearner(learner, totalMark))
      : [];

    return {
      ...detail,
      totalMark: Number.isFinite(totalMark) ? totalMark : 0,
      learners
    };
  }

  private normalizeWorkDetailLearner(learner: WorkLearnerMarkDto, parentTotalMark: number): WorkLearnerMarkDto {
    const raw = learner as unknown as Record<string, unknown>;
    const totalMarkRaw = raw.totalMark ?? raw.total_mark ?? parentTotalMark;
    const markObtainedRaw = raw.markObtained ?? raw.mark_obtained;
    const totalMark = Number(totalMarkRaw);
    const markObtained = markObtainedRaw === null || markObtainedRaw === undefined ? null : Number(markObtainedRaw);

    return {
      ...learner,
      totalMark: Number.isFinite(totalMark) ? totalMark : parentTotalMark,
      markObtained: markObtained !== null && Number.isFinite(markObtained) ? markObtained : null
    };
  }
}
