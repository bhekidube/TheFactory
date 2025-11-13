import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment'; // adjust path if needed

@Component({
  selector: 'app-bus-search',
  templateUrl: './bus-search.component.html',
  styleUrls: ['./bus-search.component.css']
})
export class BusSearchComponent implements OnInit {
  showZimraForm = false;
  showForm = false;
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
  cities = [
    "Bulawayo", "Chegutu", "Kwekwe", "Kadoma", "Zvishavane", "Masvingo", "Durban", "East London", "Francistown", "Gaborone", "Harare", "Johannesburg", "Lilongwe", "Livingstone", "Lusaka", "Manzini", "Maputo", "Maseru", "Mbabane", "Mutare", "Polokwane", "Port Elizabeth", "Pretoria", "Walvis Bay", "Windhoek"
  ];

  cityCodes: { [key: string]: string } = {
    "Bulawayo": "BYO",
    "Johannesburg": "Johannesburg",
    // ...add more as needed
  };

  digitalText = this.messages[0];
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

  constructor(private http: HttpClient) {}

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
}
