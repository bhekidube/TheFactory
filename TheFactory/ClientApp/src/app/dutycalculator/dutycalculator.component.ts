import { Component } from '@angular/core';

@Component({
  selector: 'app-dutycalculator',
  templateUrl: './dutycalculator.component.html',
  styleUrls: ['./dutycalculator.component.css']
})
export class DutyCalculatorComponent {
  itemValue: number | null = null;
  isExcluded: boolean = false;
  totalDuty: number | null = null;

  calculateDuty() {
    // Simple placeholder logic
    if (this.itemValue !== null) {
      // Example: $200 discount if not excluded
      const discount = this.isExcluded ? 0 : 200;
      this.totalDuty = Math.max(0, (this.itemValue - discount) * 0.35);
    } else {
      this.totalDuty = null;
    }
  }
}
