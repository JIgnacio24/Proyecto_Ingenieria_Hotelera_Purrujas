import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Narvar } from './narvar';

describe('Narvar', () => {
  let component: Narvar;
  let fixture: ComponentFixture<Narvar>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Narvar],
    }).compileComponents();

    fixture = TestBed.createComponent(Narvar);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
