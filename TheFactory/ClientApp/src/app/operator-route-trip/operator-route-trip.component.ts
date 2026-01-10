import { Component, Input, OnInit, ViewChild, OnChanges, SimpleChanges } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { TicketPriceService } from '../services/ticket-price.service';

@Component({
  selector: 'app-operator-route-trip',
  templateUrl: './operator-route-trip.component.html',
  styleUrls: ['./operator-route-trip.component.css']
})
export class OperatorRouteTripComponent implements OnInit, OnChanges {
  @Input() route: any;

  displayedColumns: string[] = [
    'actions',
    'active',
    'departureDateTime',
    'notes',   
    'price',
    'arrivalDateTime',
    'createdBy',
    'createdDate',
    'updatedBy',
    'updatedDate'
  ];
  dataSource = new MatTableDataSource<any>();

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  newTrip = {
    departureDateTime: '',
    arrivalDateTime: '',
    price: null,
    notes: '',
    active: true,
    createdBy: null,
    createdDate: '',
    updatedBy: null,
    updatedDate: ''
  };
  createTripMessage = '';
  showCreateTripForm = false;
  editMode = false;
  editingTripId: number | null = null;

  showTicketPriceFormFor: { [tripId: number]: boolean } = {};
  ticketPriceModels: { [tripId: number]: any } = {};
  ticketPriceMessages: { [tripId: number]: string } = {};

  constructor(private http: HttpClient, private ticketPriceService: TicketPriceService) {}

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
    const userId = Number(localStorage.getItem('userId')) || 0;
    const payload = {
      routeId: this.route.routeId,
      departureDateTime: this.newTrip.departureDateTime,
      arrivalDateTime: this.newTrip.arrivalDateTime,
      price: this.newTrip.price,
      createdBy: userId,
      notes: this.newTrip.notes,
      active: this.newTrip.active,
      updatedBy: userId, // Set in the background
      updatedDate: null
    };
    this.http.post<any>(`${environment.apiBaseUrl}/api/BusTrips/InsertRouteTrip`, payload)
      .subscribe({
        next: res => {
          this.createTripMessage = 'Trip created!';
          this.loadTrips(); // <-- Refresh the table after creating a trip
          this.newTrip = {
            departureDateTime: '',
            arrivalDateTime: '',
            price: null,
            notes: '',
            active: true,
            createdBy: null,
            createdDate: '',
            updatedBy: null,
            updatedDate: ''
          };
          this.showCreateTripForm = false; // Optionally close the form after saving
        },
        error: err => {
          this.createTripMessage = err.error?.error || 'Failed to create trip.';
        }
      });
  }

  editTrip(trip: any) {
    this.editMode = true;
    this.showCreateTripForm = true;
    this.editingTripId = trip.tripId;
    this.newTrip = {
      departureDateTime: trip.departureDateTime ? trip.departureDateTime.substring(0, 16) : '',
      arrivalDateTime: trip.arrivalDateTime ? trip.arrivalDateTime.substring(0, 16) : '',
      price: trip.price,
      notes: trip.notes,
      active: trip.active,
      createdBy: trip.createdBy,
      createdDate: trip.createdDate,
      updatedBy: trip.updatedBy,
      updatedDate: trip.updatedDate
    };
  }

  updateTrip() {
    if (!this.route?.routeId || !this.editingTripId) return;
    const userId = Number(localStorage.getItem('userId')) || 0;
    const payload = {
      tripId: this.editingTripId,
      routeId: this.route.routeId,
      departureDateTime: this.newTrip.departureDateTime,
      arrivalDateTime: this.newTrip.arrivalDateTime,
      price: this.newTrip.price,
      notes: this.newTrip.notes,
      active: this.newTrip.active,
      updatedBy: userId
    };
    this.http.put<any>(`${environment.apiBaseUrl}/api/BusTrips/UpdateRouteTrip`, payload)
      .subscribe({
        next: res => {
          this.createTripMessage = 'Trip updated!';
          this.loadTrips();
          this.newTrip = {
            departureDateTime: '',
            arrivalDateTime: '',
            price: null,
            notes: '',
            active: true,
            createdBy: null,
            createdDate: '',
            updatedBy: null,
            updatedDate: ''
          };
          this.editMode = false;
          this.editingTripId = null;
          this.showCreateTripForm = false;
        },
        error: err => {
          this.createTripMessage = err.error?.error || 'Failed to update trip.';
        }
      });
  }

  deleteTrip(element: any): void {
    // TODO: Implement delete logic here
    console.log('Delete trip:', element);
  }

  toggleTicketPriceForm(row: any) {
    alert('Toggle for tripId:'+  row.tripId +' - '+ row);
    console.log('Toggle for tripId:' + row.tripId , row);
    this.showTicketPriceFormFor[row.tripId] = !this.showTicketPriceFormFor[row.tripId];
    if (this.showTicketPriceFormFor[row.tripId] && !this.ticketPriceModels[row.tripId]) {
      this.ticketPriceModels[row.tripId] = {
        price: row.price,
        currency: row.currency || 'USD',
        startDate: '',
        endDate: '',
        notes: ''
      };
    }console.log('showTicketPriceFormFor', this.showTicketPriceFormFor[row.tripId]);
    console.log('isTicketPriceFormRow', this.isTicketPriceFormRow);
  }

  saveTicketPrice(row: any) {
    const model = {
      operatorRouteTripId: row.tripId,
      ...this.ticketPriceModels[row.tripId]
    };
    this.ticketPriceService.insertTicketPrice(model).subscribe({
      next: () => {
        this.ticketPriceMessages[row.tripId] = 'Ticket price saved!';
        setTimeout(() => {
          this.showTicketPriceFormFor[row.tripId] = false;
          this.ticketPriceMessages[row.tripId] = '';
        }, 1500);
      },
      error: err => {
        this.ticketPriceMessages[row.tripId] = 'Error: ' + (err.error?.message || 'Could not save');
      }
    });
  }

  isTicketPriceFormRow = (index: number, row: any) => this.showTicketPriceFormFor[row.tripId];
}
