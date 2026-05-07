import { CommonModule } from '@angular/common';
import { Component, input, OnInit } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  createDefaultGettingTherePageContent,
  GettingThereContentService,
  GettingTherePageContent
} from '../../services/getting-there-content.service';

@Component({
  selector: 'app-getting-there-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './getting-there.component.html',
  styleUrl: './getting-there.component.css'
})
export class GettingThereComponent implements OnInit {
  readonly variant = input<'home' | 'about'>('about');
  gettingThereContent: GettingTherePageContent = createDefaultGettingTherePageContent();

  constructor(private readonly gettingThereContentService: GettingThereContentService) {}

  ngOnInit(): void {
    void this.loadGettingThereContent();
  }

  private async loadGettingThereContent(): Promise<void> {
    this.gettingThereContent = await firstValueFrom(this.gettingThereContentService.getContent());
  }
}
