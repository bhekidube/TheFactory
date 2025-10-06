import { Component, Input } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-operator-admin',
  templateUrl: './operator-admin.component.html',
  styleUrls: ['./operator-admin.component.css']
})
export class OperatorAdminComponent {
  @Input() operatorName: string = '';
  @Input() summary: any;

  constructor(private route: ActivatedRoute, private http: HttpClient) {}

  ngOnInit(): void {
    this.operatorName = this.route.snapshot.paramMap.get('operator') || '';
    const summaryString = localStorage.getItem('operatorAdminSummary');
    this.summary = summaryString ? JSON.parse(summaryString) : null;

    alert(JSON.stringify(summaryString));
    alert(JSON.stringify('test__'+ this.summary)); // Check if operatorId is present

    // Get lookup data for locations
    this.http.get<any[]>(`${environment.apiBaseUrl}/api/Lookup/Locations`)
      .subscribe({
        next: data => this.locations = data,
        error: err => console.error('Failed to load locations', err)
      });
  }
  
  userRole: string = localStorage.getItem('userRole') || 'Public';
  isLoggedIn: boolean = !!localStorage.getItem('userName');

  viewRoute(route: any): void {
    // Implement your logic here, e.g. show route details or navigate
    alert(`Viewing route from ${route.fromLocation} to ${route.toLocation}`);
  }

  showCreateRouteForm = false;
  createRouteMessage = '';
  locations: any[] = [];
  newRoute = {
    fromLocationId: null,
    toLocationId: null
  };

  insertRoute(): void {
    const payload = {
      operatorId: Number(this.summary?.operatorId) || 0,
      fromId: Number(this.newRoute.fromLocationId),
      toId: Number(this.newRoute.toLocationId),
      createdBy: Number(localStorage.getItem('userId')) || 0
    };
    alert(this.summary.operatorId);

    alert(JSON.stringify(payload));
    this.http.post<any>(`${environment.apiBaseUrl}/api/BusTrips/InsertRoute`, payload)
      .subscribe({
        next: res => {
          this.createRouteMessage = 'Route created successfully!';
          this.showCreateRouteForm = false;
          // Optionally refresh routes list here
        },
        error: err => {
          this.createRouteMessage = err.error?.error || err.message || 'Failed to create route.';
        }
      });
  }
}
