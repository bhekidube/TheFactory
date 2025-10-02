import { Component, Input } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';

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

    // Get lookup data for locations
    this.http.get<any[]>('https://AzureLinuxAppService.azurewebsites.net/api/Lookup/Locations')
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
    fromLocationId: '',
    toLocationId: ''
  };

  insertRoute(): void {
    const payload = {
      operatorId: this.summary?.operatorId || 0,
      fromId: this.newRoute.fromLocationId,
      toId: this.newRoute.toLocationId,
      createdBy: localStorage.getItem('userId') || 0 // or get from your auth context
    };

    this.http.post<any>('https://AzureLinuxAppService.azurewebsites.net/api/BusTrips/InsertRoute', payload)
      .subscribe({
        next: res => {
          this.createRouteMessage = 'Route created successfully!';
          this.showCreateRouteForm = false;
          // Optionally refresh routes list here
        },
        error: err => {
          this.createRouteMessage = err.error?.error || 'Failed to create route.';
        }
      });
  }
}
