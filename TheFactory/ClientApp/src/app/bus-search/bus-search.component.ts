import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-bus-search',
  templateUrl: './bus-search.component.html',
  styleUrls: ['./bus-search.component.css']
})
export class BusSearchComponent implements OnInit {
  messages = [
    "EKUSENI...............🚌REVIVAL(POWER HOUSE to BYO) LEAVING 08:00...............🚌BRAVO(BYO) LEAVING 11:00...............🚌 REVIVAL(BYO) LEAVING 12:00",
    "🚌 SWISS(BYO) LEAVING POWER HOUSE  19:00",
    "🚌 MZANSI(BYO) LEAVING POWER HOUSE  14:00",
    "🚌 MZANSI(BYO) LEAVING POWER HOUSE  17:00",
    "🚌 DELTA(BYO) LEAVING POWER HOUSE  15:00",
    "🚌 DELTA(BYO) LEAVING POWER HOUSE  16:00",
    "🚌 SWISS(BYO) LEAVING POWER HOUSE  15:00",
    "🚌 SWISS(BYO) LEAVING POWER HOUSE  16:00",
    "🚌 SWISS(BYO) LEAVING POWER HOUSE  17:00",
    "🚌 IMPERIAL(BYO) LEAVING POWER HOUSE  17:00",
    "🚌 IMPERIAL(BYO) LEAVING POWER HOUSE  18:00",
    "🚌 BROOKLYN(BYO) LEAVING POWER HOUSE  17:00",
    "🚌 REGIONAL(BYO) LEAVING POWER HOUSE  17:00",
    "🚌 MTHETHI(BYO) LEAVING POWER HOUSE  18:00",
  ];
  cities = [
    "Bulawayo", "Chegutu", "Kwekwe", "Zvishavane", "Masvingo", "Durban", "East London", "Francistown", "Gaborone", "Harare", "Johannesburg", "Lilongwe", "Livingstone", "Lusaka", "Manzini", "Maputo", "Maseru", "Mbabane", "Mutare", "Polokwane", "Port Elizabeth", "Pretoria", "Walvis Bay", "Windhoek"
  ];

  cityCodes: { [key: string]: string } = {
    "Bulawayo": "BYO",
    "Johannesburg": "Johannesburg",
    // ...add more as needed
  };

  digitalText = this.messages[0];
  index = 0;
  fromInput = '';
  toInput = '';
  fromSuggestions: string[] = [];
  toSuggestions: string[] = [];
  searchResults: string[] = [];
  dateInput: string = '';

  ngOnInit() {
    setInterval(() => this.updateMessage(), 10000);
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
    this.searchResults = this.messages.filter(msg =>
      msg.toLowerCase().includes(from.toLowerCase()) ||
      msg.toLowerCase().includes(fromCode.toLowerCase()) ||
      msg.toLowerCase().includes(to.toLowerCase()) ||
      msg.toLowerCase().includes(toCode.toLowerCase())
    );
  }
}
