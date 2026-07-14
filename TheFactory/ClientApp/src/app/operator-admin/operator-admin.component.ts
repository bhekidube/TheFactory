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
   showForm = false;

  constructor(private route: ActivatedRoute, private http: HttpClient) {}

  ngOnInit(): void {
    this.operatorName = this.route.snapshot.paramMap.get('operator') || '';
    const summaryString = localStorage.getItem('operatorAdminSummary');
    this.summary = summaryString ? JSON.parse(summaryString) : null;




    // Get lookup data for locations
    this.http.get<any[]>(`${environment.apiBaseUrl}/api/Lookup/Locations`)
      .subscribe({
        next: data => this.locations = data,
        error: err => console.error('Failed to load locations', err)
      });






    const now = new Date();
    let dateToUse = new Date();
    if (now.getHours() >= 18) {
      dateToUse.setDate(now.getDate() + 1);
    }
    const yyyy = dateToUse.getFullYear();
    const mm = String(dateToUse.getMonth() + 1).padStart(2, '0');
    const dd = String(dateToUse.getDate()).padStart(2, '0');
    this.dateInput = `${yyyy}-${mm}-${dd}`;

    // Fetch locations and set default fromInput to location with ID 1 and toInput to location with ID 4
    this.http.get<any[]>(`${environment.apiBaseUrl}/api/Lookup/Locations`).subscribe(locations => {
      const defaultFrom = locations.find(l => l.locationID === 1);
      if (defaultFrom) {
        this.fromInput = defaultFrom.location;
        this.selectedFrom = defaultFrom;
      }
      const defaultTo = locations.find(l => l.locationID === 4);
      if (defaultTo) {
        this.toInput = defaultTo.location;
        this.selectedTo = defaultTo;
        this.toTownInput = defaultTo.town;

      }
      // Only search after defaults are set
      this.onSearch();
    });






  }
  
  userRole: string = localStorage.getItem('userRole') || 'Public';
  isLoggedIn: boolean = !!localStorage.getItem('userName');

  selectedRoute: any = null;

  viewRoute(route: any): void {
    this.selectedRoute = route;
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

    this.http.post<any>(`${environment.apiBaseUrl}/api/BusTrips/InsertRoute`, payload)
      .subscribe({
        next: res => {
          this.createRouteMessage = 'Route created successfully!';
          this.showCreateRouteForm = false;
          // Refresh the operator summary to update the route list
          this.http.get<any>(`${environment.apiBaseUrl}/api/Admin/OperatorSummary?operatorName=${encodeURIComponent(this.operatorName)}`)
            .subscribe({
              next: summary => {
                this.summary = summary;
                localStorage.setItem('operatorAdminSummary', JSON.stringify(summary));
              },
              error: err => {
                this.createRouteMessage = 'Route created, but failed to refresh route list.';
              }
            });
        },
        error: err => {
          this.createRouteMessage = err.error?.error || err.message || 'Failed to create route.';
        }
      });
  }




  index = 0;
  fromInput: string = '';
  toInput: string = '';
  toTownInput: string = '';
  fromSuggestions: any[] = [];
  toSuggestions: any[] = [];
  selectedFrom: any = null;
  selectedTo: any = null;
  dateInput: string = '';
  searchResults: any[] = [];




  onFromInputChange() {
    if (!this.fromInput || this.fromInput.length < 2) {
      this.fromSuggestions = [];
      return;
    }
    this.http.get<any[]>(`${environment.apiBaseUrl}/api/Lookup/Locations`).subscribe(locations => {
      this.fromSuggestions = locations.filter(l =>
        l.location.toLowerCase().includes(this.fromInput.toLowerCase())
      );
    });
  }

  onToInputChange() {
    if (!this.toInput || this.toInput.length < 2) {
      this.toSuggestions = [];
      return;
    }
    this.http.get<any[]>(`${environment.apiBaseUrl}/api/Lookup/Locations`).subscribe(locations => {
      this.toSuggestions = locations.filter(l =>
        l.location.toLowerCase().includes(this.toInput.toLowerCase())
      );
    });
  }

  selectFromSuggestion(loc: any) {
    this.fromInput = loc.location;
    this.selectedFrom = loc;
    this.fromSuggestions = [];
    this.onSearch(); // Automatically search after selecting "from"
  }

  selectToSuggestion(loc: any) {
    this.toInput = loc.location;
    this.selectedTo = loc;
    this.toSuggestions = [];
    this.toTownInput = loc.town;
    this.onSearch(); // Automatically search after selecting "to"
  }

  onSearch() {
    if (!this.selectedFrom || !this.selectedTo) {
      // Optionally show error to user
      return;
    }
    const departureDate = this.dateInput;
    const operatorId = this.summary?.operatorId || 0;

    this.http.get<any[]>(`${environment.apiBaseUrl}/api/BusTrips/GetRouteTripsByOperatorId`, {
      params: {
        operatorId,
        departureDate,
        fromId: this.selectedFrom.locationID,
        toId: this.selectedTo.locationID
      }
    }).subscribe(results => {
      this.searchResults = results;
    }, error => {
      this.searchResults = [];
      // Optionally handle error
    });
  }

  newTicketPrice: number = 0;
  newTicketCurrency: string = 'USD';
  newTicketStartDate: string = '';
  newTicketEndDate: string = '';

  insertRouteTripTicketPrice() {
    const payload = {
      operatorId: Number(this.summary?.operatorId) || 0,
      routeTripId: this.selectedRoute?.tripId || 0,
      price: this.newTicketPrice,
      currency: this.newTicketCurrency,
      startDate: this.newTicketStartDate,
      endDate: this.newTicketEndDate || null,
      active: true
    };

    this.http.post<any>(`${environment.apiBaseUrl}/api/BusTrips/InsertRouteTripTicketPrice`, payload)
      .subscribe({
        next: res => {
          alert('Ticket price added successfully!');
          // Optionally refresh data or clear form
        },
        error: err => {
          alert(err.error?.error || err.message || 'Failed to add ticket price.');
        }
      });
  }






}
