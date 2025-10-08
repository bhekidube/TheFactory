import { Component, Input, OnInit, ViewChild } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';

@Component({
  selector: 'app-operator-route-trip',
  templateUrl: './operator-route-trip.component.html',
  styleUrls: ['./operator-route-trip.component.css']
})
export class OperatorRouteTripComponent implements OnInit {
  @Input() route: any; // <-- Add this line

  displayedColumns: string[] = ['column1', 'column2', 'column3']; // Adjust as needed
  dataSource = new MatTableDataSource<any>();

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  ngOnInit() {
    // If your route contains trips, set them as dataSource
    if (this.route && this.route.trips) {
      this.dataSource.data = this.route.trips;
    }
  }

  ngAfterViewInit() {
    this.dataSource.paginator = this.paginator;
    this.dataSource.sort = this.sort;
  }
}
