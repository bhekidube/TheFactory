import { TestBed } from '@angular/core/testing';

import { TicketPriceService } from './ticket-price.service';

describe('TicketPriceService', () => {
  let service: TicketPriceService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(TicketPriceService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
