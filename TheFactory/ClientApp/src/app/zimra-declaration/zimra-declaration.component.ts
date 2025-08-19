import { Component, EventEmitter, OnInit, Output } from '@angular/core';

@Component({
  selector: 'app-zimra-declaration',
  templateUrl: './zimra-declaration.component.html',
  styleUrls: ['./zimra-declaration.component.css']
})
export class ZimraDeclarationComponent implements OnInit {
  @Output() close = new EventEmitter<void>();

  constructor() { }

  ngOnInit(): void {
  }

  closeForm() {
    this.close.emit();
  }
}
