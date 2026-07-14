import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface BusResult {
  busName: string;
  from: string;
  to: string;
  date: string;
  departureTime: string;
  arrivalTime: string;
  price: number;
}

@Injectable({ providedIn: 'root' })
export class BusSearchService {
  constructor(private http: HttpClient) {}

  getBuses(from: string, to: string, date: string): Observable<BusResult[]> {
    return this.http.get<BusResult[]>('/api/bus', {
      params: { from, to, date }
    });
  }
}