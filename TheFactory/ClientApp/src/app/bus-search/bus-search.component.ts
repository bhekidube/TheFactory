import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { animate, state, style, transition, trigger } from '@angular/animations';
import { environment } from '../../environments/environment';
import { AuthService } from '../services/auth.service';
import { TableSortController } from '../table-sorting';

type ScheduleColumn = 'bus' | 'departure' | 'price' | 'action';
type ScheduleColumnVisibility = Record<ScheduleColumn, boolean>;

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
  private readonly columnVisibilityApiUrl = `${environment.apiBaseUrl}/api/ScheduleSettings/columns`;
  private readonly columnWidths: Record<ScheduleColumn, number> = {
    bus: 38,
    departure: 18,
    price: 18,
    action: 26
  };
  readonly tableSort = new TableSortController();
  columnVisibility: ScheduleColumnVisibility = {
    bus: true,
    departure: true,
    price: true,
    action: true
  };
  columnVisibilityLoaded = false;
  isSavingColumnVisibility = false;
  columnVisibilityError: string | null = null;
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

  get isLoggedIn(): boolean {
    return !!this.signedInUserName;
  }

  get signedInUserName(): string {
    if (typeof localStorage === 'undefined') {
      return '';
    }

    const userName = localStorage.getItem('userName') || '';
    const userRole = localStorage.getItem('userRole') || '';
    const userId = localStorage.getItem('userId') || '';
    return userName.trim() && userRole.trim() && userId.trim() ? userName : '';
  }

  get signedInUserRole(): string {
    return this.isLoggedIn ? localStorage.getItem('userRole') || '' : '';
  }

  get isSystemAdmin(): boolean {
    return this.isLoggedIn && this.signedInUserRole.trim() === 'SystemAdmin';
  }

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
  editingDepartureTripId: number | null = null;
  departureDateTimeDraft = '';
  savingDepartureTripId: number | null = null;
  departureEditError: string | null = null;
  editingPriceTripId: number | null = null;
  priceDraft: number | string | null = '';
  savingPriceTripId: number | null = null;
  priceEditError: string | null = null;

  constructor(
    private http: HttpClient,
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/']);
  }

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
    this.loadColumnVisibilityPreferences();
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

  isColumnVisible(column: ScheduleColumn): boolean {
    return this.columnVisibility[column];
  }

  isOnlyVisibleColumn(column: ScheduleColumn): boolean {
    return this.columnVisibility[column] &&
      Object.values(this.columnVisibility).filter(Boolean).length === 1;
  }

  getColumnWidth(column: ScheduleColumn): number {
    const visibleWidth = Object.entries(this.columnWidths)
      .filter(([key]) => this.isColumnVisible(key as ScheduleColumn))
      .reduce((total, [, width]) => total + width, 0);

    return (this.columnWidths[column] / visibleWidth) * 100;
  }

  onColumnVisibilityChange(column: ScheduleColumn, event: Event): void {
    const target = event.target;
    if (target instanceof HTMLInputElement) {
      this.setColumnVisibility(column, target.checked);
    }
  }

  setColumnVisibility(column: ScheduleColumn, visible: boolean): void {
    if (!this.isSystemAdmin || !this.columnVisibilityLoaded || this.isSavingColumnVisibility ||
        (!visible && this.isOnlyVisibleColumn(column))) {
      return;
    }

    const previousVisibility = this.columnVisibility;
    const nextVisibility = { ...previousVisibility, [column]: visible };
    if (!Object.values(nextVisibility).some(Boolean)) {
      return;
    }

    this.columnVisibility = nextVisibility;
    this.isSavingColumnVisibility = true;
    this.columnVisibilityError = null;

    const headers = new HttpHeaders({
      'X-User-Id': typeof localStorage === 'undefined' ? '' : localStorage.getItem('userId') || ''
    });
    this.http.put<ScheduleColumnVisibility>(this.columnVisibilityApiUrl, nextVisibility, { headers }).subscribe({
      next: savedVisibility => {
        this.columnVisibility = this.normalizeColumnVisibility(savedVisibility);
        this.isSavingColumnVisibility = false;
      },
      error: () => {
        this.columnVisibility = previousVisibility;
        this.isSavingColumnVisibility = false;
        this.columnVisibilityError = 'Could not save the shared timetable layout. Please try again.';
      }
    });
  }

  loadColumnVisibilityPreferences(): void {
    this.http.get<ScheduleColumnVisibility>(this.columnVisibilityApiUrl).subscribe({
      next: preferences => {
        this.columnVisibility = this.normalizeColumnVisibility(preferences);
        this.columnVisibilityLoaded = true;
        this.columnVisibilityError = null;
      },
      error: () => {
        this.columnVisibilityLoaded = true;
        this.columnVisibilityError = 'Could not load the shared timetable layout. Showing all columns.';
      }
    });
  }

  private normalizeColumnVisibility(
    preferences: Partial<ScheduleColumnVisibility> | null | undefined
  ): ScheduleColumnVisibility {
    const candidate = preferences && typeof preferences === 'object' ? preferences : {};
    const normalized: ScheduleColumnVisibility = {
      bus: typeof candidate.bus === 'boolean' ? candidate.bus : true,
      departure: typeof candidate.departure === 'boolean' ? candidate.departure : true,
      price: typeof candidate.price === 'boolean' ? candidate.price : true,
      action: typeof candidate.action === 'boolean' ? candidate.action : true
    };

    return Object.values(normalized).some(Boolean)
      ? normalized
      : { bus: true, departure: true, price: true, action: true };
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

  getLoginActionLabel(row: any): string {
    return this.isTripUnavailable(row) ? this.getActionLabel(row) : 'Sign in to select seat';
  }

  getPriceDisplay(row: any): string {
    const rawPrice = row?.price ?? row?.fare ?? row?.amount;
    const parsedPrice = Number(rawPrice);

    if (Number.isFinite(parsedPrice)) {
      return `R ${parsedPrice.toFixed(2)}`;
    }

    return '--';
  }

  startDepartureEdit(row: any): void {
    if (!this.isSystemAdmin || !row?.tripId) {
      return;
    }

    this.editingDepartureTripId = row.tripId;
    this.departureDateTimeDraft = this.toLocalDateTimeInput(row.departureDateTime);
    this.departureEditError = null;
  }

  cancelDepartureEdit(): void {
    if (this.savingDepartureTripId === this.editingDepartureTripId) {
      return;
    }

    this.editingDepartureTripId = null;
    this.departureDateTimeDraft = '';
    this.departureEditError = null;
  }

  saveDepartureDateTime(row: any): void {
    if (!this.isSystemAdmin || this.editingDepartureTripId !== row?.tripId ||
        !this.departureDateTimeDraft || this.savingDepartureTripId !== null) {
      return;
    }

    const updatedBy = Number(localStorage.getItem('userId'));
    if (!Number.isInteger(updatedBy) || updatedBy <= 0) {
      this.departureEditError = 'Your session is missing a valid user ID. Please sign in again.';
      return;
    }

    const departureDateTime = new Date(this.departureDateTimeDraft);
    if (Number.isNaN(departureDateTime.getTime())) {
      this.departureEditError = 'Enter a valid departure date and time.';
      return;
    }

    this.savingDepartureTripId = row.tripId;
    this.departureEditError = null;
    this.http.put<{ rowsAffected: number }>(`${environment.apiBaseUrl}/api/BusTrips/UpdateRouteTrip`, {
      tripId: row.tripId,
      departureDateTime: this.departureDateTimeDraft,
      arrivalDateTime: row.arrivalDateTime,
      price: row.price,
      notes: row.notes,
      active: row.active,
      updatedBy
    }).subscribe({
      next: result => {
        if (result.rowsAffected === 0) {
          this.departureEditError = 'This trip could not be found. Refresh the schedule and try again.';
          this.savingDepartureTripId = null;
          return;
        }

        row.departureDateTime = departureDateTime.toISOString();
        this.onSearch(false);
        this.editingDepartureTripId = null;
        this.departureDateTimeDraft = '';
        this.savingDepartureTripId = null;
      },
      error: () => {
        this.departureEditError = 'Failed to update the departure. Please try again.';
        this.savingDepartureTripId = null;
      }
    });
  }

  private toLocalDateTimeInput(value: string | Date | null | undefined): string {
    if (!value) {
      return '';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return '';
    }

    const pad = (part: number) => String(part).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}` +
      `T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }

  startPriceEdit(row: any): void {
    if (!this.isSystemAdmin || !row?.tripId) {
      return;
    }

    this.editingPriceTripId = row.tripId;
    this.priceDraft = Number(row.price);
    this.priceEditError = null;
  }

  cancelPriceEdit(): void {
    if (this.savingPriceTripId === this.editingPriceTripId) {
      return;
    }

    this.editingPriceTripId = null;
    this.priceDraft = '';
    this.priceEditError = null;
  }

  savePrice(row: any): void {
    if (!this.isSystemAdmin || this.editingPriceTripId !== row?.tripId ||
        this.priceDraft === '' || this.priceDraft === null || this.savingPriceTripId !== null) {
      return;
    }

    const price = Number(this.priceDraft);
    if (!Number.isFinite(price) || price < 0) {
      this.priceEditError = 'Enter a valid non-negative price.';
      return;
    }

    const updatedBy = Number(localStorage.getItem('userId'));
    if (!Number.isInteger(updatedBy) || updatedBy <= 0) {
      this.priceEditError = 'Your session is missing a valid user ID. Please sign in again.';
      return;
    }

    this.savingPriceTripId = row.tripId;
    this.priceEditError = null;
    this.http.put<{ rowsAffected: number }>(`${environment.apiBaseUrl}/api/BusTrips/UpdateRouteTrip`, {
      tripId: row.tripId,
      departureDateTime: row.departureDateTime,
      arrivalDateTime: row.arrivalDateTime,
      price,
      notes: row.notes,
      active: row.active,
      updatedBy
    }).subscribe({
      next: result => {
        if (result.rowsAffected === 0) {
          this.priceEditError = 'This trip could not be found. Refresh the schedule and try again.';
          this.savingPriceTripId = null;
          return;
        }

        row.price = price;
        this.onSearch(false);
        this.editingPriceTripId = null;
        this.priceDraft = '';
        this.savingPriceTripId = null;
      },
      error: () => {
        this.priceEditError = 'Failed to update the ticket price. Please try again.';
        this.savingPriceTripId = null;
      }
    });
  }

  onSelectSeat(row: any): void {
    if (!this.isLoggedIn) {
      this.router.navigate(['/auth']);
      return;
    }

    if (this.isTripUnavailable(row)) {
      return;
    }

    // Hook this to your booking flow endpoint/route when available.
    alert(`Booking flow not connected yet for ${row?.operatorName ?? 'this bus'}.`);
  }
}
