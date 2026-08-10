import html2canvas from 'html2canvas';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { animate, state, style, transition, trigger } from '@angular/animations';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-bus-search',
  templateUrl: './bus-search.component.html',
  styleUrls: ['./bus-search.component.css'],
  animations: [
    trigger('slideToggle', [
      state('closed', style({
        height: '0px',
        opacity: 0,
        transform: 'translateY(-8px)',
        marginTop: '0px'
      })),
      state('open', style({
        height: '*',
        opacity: 1,
        transform: 'translateY(0)',
        marginTop: '*'
      })),
      transition('closed <=> open', animate('240ms cubic-bezier(0.4, 0, 0.2, 1)'))
    ])
  ]
})
export class BusSearchComponent implements OnInit {
  showZimraForm = false;
  showForm = true;
  hasSearched = false;

  messages = [
    "EKUSENI...............🚌REVIVAL(POWER HOUSE to BYO) LEAVING 10:30...............🚌BRAVO(BYO) LEAVING 11:30 (+27 82 715 6380)...............🚌 REVIVAL(BYO) LEAVING 09:30 (+27 61 843 2404)",
    "🚌 SWISS(BYO) LEAVING POWER HOUSE  14:00 - +27 64 475 8301",
    "🚌 MZANSI(BYO) LEAVING POWER HOUSE  14:00 +27 11 057 8418",
    "🚌 MZANSI(BYO) LEAVING POWER HOUSE  17:00 +27 11 057 8418",
    "🚌 DELTA(BYO) LEAVING POWER HOUSE  15:00 076 441 0202. ",
    "🚌 DELTA(BYO) LEAVING POWER HOUSE  16:30 076 441 0202. ",
    "🚌 SWISS(BYO) LEAVING POWER HOUSE  15:30  +27 64 475 8301",
    "🚌 SWISS(BYO) LEAVING POWER HOUSE  16:30  +27 64 475 8301",
    "🚌 IMPERIAL(BYO) LEAVING POWER HOUSE  16:00  +27 10 595 4367",
    "🚌 BROOKLYN(BYO) LEAVING POWER HOUSE  16:45 +263 78 260 1615",
    "🚌 REGIONAL(BYO) LEAVING POWER HOUSE  17:00 +27 78 047 5428",
    "🚌 MTHETHI(BYO) LEAVING POWER HOUSE  18:00 +263 71 625 7718",
  ];
  digitalText = this.messages[0];
  index = 0;

  // If you already have operator chips/checkboxes, bind them to this array.
  selectedOperatorFilters: string[] = [];

  // Existing fields...
  fromInput: string = '';
  toInput: string = '';
  dateInput: string = '';
  selectedFrom: any = null;
  selectedTo: any = null;
  fromSuggestions: any[] = [];
  toSuggestions: any[] = [];
  toTownInput: string = '';
  searchResults: any[] = [];

  constructor(private http: HttpClient) {}

  // Dynamic active filter badge count
  get activeFilterCount(): number {
    let count = 0;
    if (this.fromInput?.trim()) count++;
    if (this.toInput?.trim()) count++;
    if (this.selectedOperatorFilters?.length) count++;
    return count;
  }

  toggleForm(): void {
    this.showForm = !this.showForm;
  }

  openSearchForm(): void {
    this.showForm = true;
  }

  swapLocations(): void {
    const fromInput = this.fromInput;
    this.fromInput = this.toInput;
    this.toInput = fromInput;

    const fromSelection = this.selectedFrom;
    this.selectedFrom = this.selectedTo;
    this.selectedTo = fromSelection;

    this.toTownInput = this.selectedTo?.town ?? '';

    this.fromSuggestions = [];
    this.toSuggestions = [];

    if (this.selectedFrom && this.selectedTo) {
      this.onSearch(false);
    }
  }

  ngOnInit() {
    setInterval(() => this.updateMessage(), 10000);

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

  updateMessage() {
    this.digitalText = this.messages[this.index];
    this.index = (this.index + 1) % this.messages.length;
  }

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
    this.onSearch(false); // Keep form open while refining filters
  }

  selectToSuggestion(loc: any) {
    this.toInput = loc.location;
    this.selectedTo = loc;
    this.toSuggestions = [];
    this.toTownInput = loc.town;
    this.onSearch(false); // Keep form open while refining filters
  }

  onSearch(collapseToSummary = true) {
    if (!this.selectedFrom || !this.selectedTo) {
      // Optionally show error to user
      return;
    }
    this.hasSearched = true;
    if (collapseToSummary) {
      this.showForm = false;
    }
    const departureDate = this.dateInput;
    this.http.get<any[]>(`${environment.apiBaseUrl}/api/BusTrips/GetRouteTrips`, {
      params: {
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

  isSoldOut(row: any): boolean {
    return row?.isSoldOut === true || row?.soldOut === true || row?.availableSeats === 0;
  }

  isDeparted(row: any): boolean {
    if (!row?.departureDateTime) {
      return false;
    }

    const departureTime = new Date(row.departureDateTime);
    if (Number.isNaN(departureTime.getTime())) {
      return false;
    }

    return departureTime.getTime() < Date.now();
  }

  isTripUnavailable(row: any): boolean {
    return this.isSoldOut(row) || this.isDeparted(row);
  }

  getActionLabel(row: any): string {
    if (this.isSoldOut(row)) {
      return 'Sold Out';
    }

    if (this.isDeparted(row)) {
      return 'Departed';
    }

    return 'Select Seat';
  }

  getPriceDisplay(row: any): string {
    const rawPrice = row?.price ?? row?.fare ?? row?.amount;
    const parsedPrice = Number(rawPrice);

    if (Number.isFinite(parsedPrice)) {
      return `R ${parsedPrice.toFixed(2)}`;
    }

    return '--';
  }

  onSelectSeat(row: any): void {
    if (this.isTripUnavailable(row)) {
      return;
    }

    // Hook this to your booking flow endpoint/route when available.
    alert(`Booking flow not connected yet for ${row?.operatorName ?? 'this bus'}.`);
  }

  async postToFacebook() {
    const tableElement = document.querySelector('.screenshot-table-container') as HTMLElement;
    if (!tableElement) {
      alert('Could not find the bus schedule table.');
      return;
    }
    try {
      const canvas = await html2canvas(tableElement);
      const imageData = canvas.toDataURL('image/png');
      // Send image to backend API
      this.http.post('/api/facebook/post', { image: imageData }).subscribe({
        next: () => alert('Posted to Facebook successfully!'),
        error: () => alert('Failed to post to Facebook.')
      });
    } catch (err) {
      alert('Error capturing table image.');
    }
  }
}
