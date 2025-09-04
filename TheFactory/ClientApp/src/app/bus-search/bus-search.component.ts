import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-bus-search',
  templateUrl: './bus-search.component.html',
  styleUrls: ['./bus-search.component.css']
})
export class BusSearchComponent implements OnInit {
  showZimraForm = false;
  showForm = false;
  messages = [
    "EKUSENI...............🚌REVIVAL(POWER HOUSE to BYO) LEAVING 08:00...............🚌BRAVO(BYO) LEAVING 11:00 (+27 82 715 6380)...............🚌 REVIVAL(BYO) LEAVING 12:00 (+27 61 843 2404)",
    "🚌 SWISS(BYO) LEAVING POWER HOUSE  19:00 - +27 64 475 8301",
    "🚌 MZANSI(BYO) LEAVING POWER HOUSE  14:00 +27 11 057 8418",
    "🚌 MZANSI(BYO) LEAVING POWER HOUSE  17:00 +27 11 057 8418",
    "🚌 DELTA(BYO) LEAVING POWER HOUSE  15:00 076 441 0202. ",
    "🚌 DELTA(BYO) LEAVING POWER HOUSE  16:00 076 441 0202. ",
    "🚌 SWISS(BYO) LEAVING POWER HOUSE  15:00  +27 64 475 8301",
    "🚌 SWISS(BYO) LEAVING POWER HOUSE  16:00  +27 64 475 8301",
    "🚌 SWISS(BYO) LEAVING POWER HOUSE  17:00  +27 64 475 8301",
    "🚌 IMPERIAL(BYO) LEAVING POWER HOUSE  17:00  +27 10 595 4367",
    "🚌 IMPERIAL(BYO) LEAVING POWER HOUSE  18:00  +27 10 595 4367",
    "🚌 BROOKLYN(BYO) LEAVING POWER HOUSE  17:00 +263 78 260 1615",
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
  fromInput: string = 'Power House (Jozi)';
  toInput: string = 'Bulawayo';
  fromSuggestions: string[] = [];
  toSuggestions: string[] = [];
  searchResults: {
    from: string;
    to: string;
    date: string;
    departureTime: string;
    arrivalTime: string;
    price: number;
    busName: string;
  }[] = [];
  dateInput: string = '';

  ngOnInit() {
    setInterval(() => this.updateMessage(), 10000);

    const now = new Date();
    let dateToUse = new Date();
    if (now.getHours() >= 18) {
      // After 6pm, use tomorrow's date
      dateToUse.setDate(now.getDate() + 1);
    }
    // Format as yyyy-MM-dd for input[type="date"]
    const yyyy = dateToUse.getFullYear();
    const mm = String(dateToUse.getMonth() + 1).padStart(2, '0');
    const dd = String(dateToUse.getDate()).padStart(2, '0');
    this.dateInput = `${yyyy}-${mm}-${dd}`;

    this.onSearch();
  }

  updateMessage() {
    this.digitalText = this.messages[this.index];
    this.index = (this.index + 1) % this.messages.length;
  }

  onFromInputChange() {
    const value = this.fromInput.toLowerCase();
    this.fromSuggestions = value
      ? this.cities.filter(city => city.toLowerCase().startsWith(value))
      : [];
  }

  onToInputChange() {
    const value = this.toInput.toLowerCase();
    this.toSuggestions = value
      ? this.cities.filter(city => city.toLowerCase().startsWith(value))
      : [];
  }

  selectFromSuggestion(city: string) {
    this.fromInput = city;
    this.fromSuggestions = [];
  }

  selectToSuggestion(city: string) {
    this.toInput = city;
    this.toSuggestions = [];
  }

  onSearch() {
    const from = this.fromInput.trim().toLowerCase();
    const to = this.toInput.trim().toLowerCase();
    const fromCode = this.cityCodes[this.fromInput] || this.fromInput;
    const toCode = this.cityCodes[this.toInput] || this.toInput;
    this.searchResults = [
      {
        busName: 'Revival',
        from: 'Power House (Jozi)',
        to: 'Bulawayo',
        date: '2025-09-04',
        departureTime: '08:00',
        arrivalTime: '10:00',
        price: 100
      },
      {
        busName: 'Bravo',
        from: 'Power House (Jozi)',
        to: 'Bulawayo',
        date: '2025-09-04',
        departureTime: '11:00',
        arrivalTime: '14:00',
        price: 100
      },
      {
        busName: 'Revival',
        from: 'Power House (Jozi)',
        to: 'Bulawayo',
        date: '2025-09-04',
        departureTime: '12:00',
        arrivalTime: '20:00',
        price: 100
      },
      {
        busName: 'Swiss',
        from: 'Power House (Jozi)',
        to: 'Bulawayo',
        date: '2025-09-04',
        departureTime: '19:00',
        arrivalTime: '10:00',
        price: 100
      },
      {
        busName: 'Mzansi',
        from: 'Power House (Jozi)',
        to: 'Bulawayo',
        date: '2025-09-04',
        departureTime: '14:00',
        arrivalTime: '06:00',
        price: 100
      },
      {
        busName: 'Mzansi',
        from: 'Power House (Jozi)',
        to: 'Bulawayo',
        date: '2025-09-04',
        departureTime: '17:00',
        arrivalTime: '20:00',
        price: 100
      },
      {
        busName: 'Delta',
        from: 'Power House (Jozi)',
        to: 'Bulawayo',
        date: '2025-09-04',
        departureTime: '15:00',
        arrivalTime: '10:00',
        price: 100
      },
      {
        busName: 'Delta',
        from: 'Power House (Jozi)',
        to: 'Bulawayo',
        date: '2025-09-04',
        departureTime: '16:00',
        arrivalTime: '20:00',
        price: 100
      },
      {
        busName: 'Swiss',
        from: 'Power House (Jozi)',
        to: 'Bulawayo',
        date: '2025-09-04',
        departureTime: '15:00',
        arrivalTime: '10:00',
        price: 100
      },
      {
        busName: 'Swiss',
        from: 'Power House (Jozi)',
        to: 'Bulawayo',
        date: '2025-09-04',
        departureTime: '16:00',
        arrivalTime: '14:00',
        price: 100
      },
       {
        busName: 'Swiss',
        from: 'Power House (Jozi)',
        to: 'Bulawayo',
        date: '2025-09-04',
        departureTime: '17:00',
        arrivalTime: '14:00',
        price: 100
      },
            {
        busName: 'Imperial',
        from: 'Power House (Jozi)',
        to: 'Bulawayo',
        date: '2025-09-04',
        departureTime: '17:00',
        arrivalTime: '14:00',
        price: 100
      },
            {
        busName: 'Imperial',
        from: 'Power House (Jozi)',
        to: 'Bulawayo',
        date: '2025-09-04',
        departureTime: '18:00',
        arrivalTime: '14:00',
        price: 100
      },
     {
        busName: 'Regional',
        from: 'Power House (Jozi)',
        to: 'Bulawayo',
        date: '2025-09-04',
        departureTime: '17:00',
        arrivalTime: '14:00',
        price: 100
      },
           {
        busName: 'Mthethi',
        from: 'Power House (Jozi)',
        to: 'Bulawayo',
        date: '2025-09-04',
        departureTime: '18:00',
        arrivalTime: '14:00',
        price: 100
      },
      {
        busName: 'Brooklyn',
        from: 'Power House (Jozi)',
        to: 'Bulawayo',
        date: '2025-09-04',
        departureTime: '17:00',
        arrivalTime: '20:00',
        price: 100
      }
    ].filter(result =>
      result.from.toLowerCase().includes(from) &&
      result.to.toLowerCase().includes(to)
    );
  }
}
