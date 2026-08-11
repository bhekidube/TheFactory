import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  LearnerDto,
  LearnerReportResponseDto,
  SubjectScoreDto
} from '../learner-management/learner-management.models';

@Injectable({
  providedIn: 'root'
})
export class LearnerManagementService {
  private readonly baseUrl = `${environment.apiBaseUrl}/api`;

  constructor(private readonly http: HttpClient) {}

  getSchools(): Observable<Array<{ id: number; name: string }>> {
    return this.http.get<Array<{ id: number; name: string }>>(`${this.baseUrl}/schools`);
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

  getLearner(id: number): Observable<LearnerDto> {
    return this.http.get<LearnerDto>(`${this.baseUrl}/learners/${id}`, this.getRequestOptions());
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

  downloadReportPdf(learnerId: number): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.baseUrl}/reports/${learnerId}/pdf`, {
      observe: 'response',
      responseType: 'blob',
      headers: this.getHeaders()
    });
  }

  upsertSubjectScore(learnerId: number, payload: SubjectScoreDto): Observable<SubjectScoreDto> {
    return this.http.post<SubjectScoreDto>(`${this.baseUrl}/reports/${learnerId}/scores`, payload, this.getRequestOptions());
  }

  private getRequestOptions(): { headers: HttpHeaders } {
    return { headers: this.getHeaders() };
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
}
