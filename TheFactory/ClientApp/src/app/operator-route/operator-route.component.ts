import { Component, OnInit } from '@angular/core';
import { RouteService, RouteCreateDto, RouteUpdateDto } from '../services/route.service';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-operator-route',
  templateUrl: './operator-route.component.html'
})
export class OperatorRouteComponent implements OnInit {
  locations: any[] = [];
  routes: any[] = []; // <-- Add this property

  route: RouteCreateDto = {
    operatorId: 1, // Set from logged-in operator
    fromId: 0,
    toId: 0,
    date: '',
    departureTime: '',
    arrivalTime: '',
    price: 0,
    createdBy: 1 // Set from logged-in user
  };

  updateRouteId: number = 0;
  updateRouteData: RouteUpdateDto = { ...this.route, updatedBy: 1 };

  constructor(private routeService: RouteService, private http: HttpClient) {}

  ngOnInit() {
    this.http.get<any[]>('/api/location').subscribe({
      next: data => this.locations = data,
      error: err => console.error('Location error:', err)
    });

    this.http.get<any[]>('/api/route').subscribe({
      next: data => this.routes = data,
      error: err => console.error('Route error:', err)
    });
  }

  createRoute() {
    this.routeService.createRoute(this.route).subscribe(res => {
      alert(res.message);
    });
  }

  updateRoute() {
    this.routeService.updateRoute(this.updateRouteId, this.updateRouteData).subscribe(res => {
      alert(res.message);
    });
  }
}