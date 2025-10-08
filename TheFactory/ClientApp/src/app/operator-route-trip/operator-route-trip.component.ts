import { Component, Input, OnInit, ViewChild, OnChanges, SimpleChanges } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-operator-route-trip',
  templateUrl: './operator-route-trip.component.html',
  styleUrls: ['./operator-route-trip.component.css']
})
export class OperatorRouteTripComponent implements OnInit, OnChanges {
  @Input() route: any;

  displayedColumns: string[] = ['tripId', 'departureDateTime', 'arrivalDateTime', 'price', 'active'];
  dataSource = new MatTableDataSource<any>();

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  newTrip = {
    departureDateTime: '',
    arrivalDateTime: '',
    price: null,
    notes: '',
    active: true
  };
  createTripMessage = '';

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.loadTrips();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['route'] && this.route?.routeId) {
      this.loadTrips();
    }
  }

  loadTrips() {
    if (!this.route?.routeId) return;
    this.http.get<any[]>(`${environment.apiBaseUrl}/api/BusTrips/GetTripsByRoute/${this.route.routeId}`)
      .subscribe(trips => {
        this.dataSource.data = trips;
        if (this.paginator) this.dataSource.paginator = this.paginator;
        if (this.sort) this.dataSource.sort = this.sort;
      });
  }

  createTrip() {
    if (!this.route?.routeId) return;
    const payload = {
      routeId: this.route.routeId,
      departureDateTime: this.newTrip.departureDateTime,
      arrivalDateTime: this.newTrip.arrivalDateTime,
      price: this.newTrip.price,
      createdBy: Number(localStorage.getItem('userId')) || 0,
      notes: this.newTrip.notes,
      active: this.newTrip.active,
      updatedBy: null,
      updatedDate: null
    };
    this.http.post<any>(`${environment.apiBaseUrl}/api/BusTrips/InsertRouteTrip`, payload)
      .subscribe({
        next: res => {
          this.createTripMessage = 'Trip created!';
          this.loadTrips();
          this.newTrip = { departureDateTime: '', arrivalDateTime: '', price: null, notes: '', active: true };
        },
        error: err => {
          this.createTripMessage = err.error?.error || 'Failed to create trip.';
        }
      });
  }
}
