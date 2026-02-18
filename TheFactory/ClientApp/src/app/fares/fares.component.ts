import { Component } from '@angular/core';

@Component({
  selector: 'app-fares',
  templateUrl: './fares.component.html',
  styleUrls: ['./fares.component.css']
})
export class FaresComponent {
  busData = [
    { operator: 'Mzansi Express', time: '14:00', pickup: 'Powerhouse / 88 Simmonds', priceRange: '540 - 650' },
    { operator: 'Brooklyn Express', time: '12:00', pickup: 'Park Station (Bay 21)', priceRange: '560 - 680' },
    { operator: 'Intercity Xpress', time: '17:30', pickup: 'Park Station', priceRange: '650 - 750' },
    { operator: 'Delta Coaches', time: '14:00', pickup: 'Park Station', priceRange: '540 - 600' },
    { operator: 'Imperial Lane', time: '15:30', pickup: 'Powerhouse', priceRange: '600 - 640' },
    { operator: 'Bravo Tours', time: '11:30', pickup: 'Powerhouse', priceRange: '580 - 650' }
  ];
}
