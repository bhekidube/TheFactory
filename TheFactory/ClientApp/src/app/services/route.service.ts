import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface RouteCreateDto {
  operatorId: number;
  fromId: number;
  toId: number;
  date: string;
  departureTime: string;
  arrivalTime: string;
  price: number;
  createdBy: number;
}

export interface RouteUpdateDto extends RouteCreateDto {
  updatedBy: number;
}

@Injectable({ providedIn: 'root' })
export class RouteService {
  constructor(private http: HttpClient) {}

  createRoute(dto: RouteCreateDto): Observable<any> {
    return this.http.post('/api/route', dto);
  }

  updateRoute(id: number, dto: RouteUpdateDto): Observable<any> {
    return this.http.put(`/api/route/${id}`, dto);
  }
}