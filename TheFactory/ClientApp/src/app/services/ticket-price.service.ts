import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class TicketPriceService {
  constructor(private http: HttpClient) {}

  insertTicketPrice(model: any): Observable<any> {
    return this.http.post(`${environment.apiBaseUrl}/api/BusTrips/InsertRouteTripTicketPrice`, model);
  }

  // Add more methods as needed (e.g., getTicketPrices, updateTicketPrice, etc.)
}
