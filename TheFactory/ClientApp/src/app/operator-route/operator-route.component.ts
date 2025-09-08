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
    // Load locations
    this.http.get<any[]>('/api/location').subscribe(data => {
      this.locations = data;
    });

    // Load routes
    this.http.get<any[]>('/api/route').subscribe(data => {
      this.routes = data;
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