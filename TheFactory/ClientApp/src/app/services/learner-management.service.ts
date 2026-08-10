import { Injectable } from '@angular/core';
import { HttpClient, HttpResponse } from '@angular/common/http';
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

  getLearners(): Observable<LearnerDto[]> {
    return this.http.get<LearnerDto[]>(`${this.baseUrl}/learners`);
  }

  getLearner(id: number): Observable<LearnerDto> {
    return this.http.get<LearnerDto>(`${this.baseUrl}/learners/${id}`);
  }

  createLearner(payload: LearnerDto): Observable<LearnerDto> {
    return this.http.post<LearnerDto>(`${this.baseUrl}/learners`, payload);
  }

  updateLearner(id: number, payload: LearnerDto): Observable<LearnerDto> {
    return this.http.put<LearnerDto>(`${this.baseUrl}/learners/${id}`, payload);
  }

  archiveLearner(id: number): Observable<unknown> {
    return this.http.delete(`${this.baseUrl}/learners/${id}`);
  }

  getReport(learnerId: number): Observable<LearnerReportResponseDto> {
    return this.http.get<LearnerReportResponseDto>(`${this.baseUrl}/reports/${learnerId}`);
  }

  downloadReportPdf(learnerId: number): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.baseUrl}/reports/${learnerId}/pdf`, {
      observe: 'response',
      responseType: 'blob'
    });
  }

  upsertSubjectScore(learnerId: number, payload: SubjectScoreDto): Observable<SubjectScoreDto> {
    return this.http.post<SubjectScoreDto>(`${this.baseUrl}/reports/${learnerId}/scores`, payload);
  }
}
