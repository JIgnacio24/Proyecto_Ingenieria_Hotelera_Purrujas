import { CommonModule, DOCUMENT } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { AfterViewInit, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  cloneFacilitiesPageContent,
  createDefaultFacilitiesPageContent,
  FACILITIES_SERVICE_REFERENCE_LABELS,
  FacilitiesContentService,
  FacilitiesPageContent
} from '../../core/facilities-content.service';

@Component({
  selector: 'app-facilities-editor',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './facilities-editor.component.html',
  styleUrl: './facilities-editor.component.css'
})
export class FacilitiesEditorComponent implements AfterViewInit {
  private readonly document = inject(DOCUMENT);
  private readonly facilitiesContentService = inject(FacilitiesContentService);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly feedback = signal('');
  readonly feedbackTone = signal<'success' | 'error' | ''>('');
  readonly serviceReferenceLabels = FACILITIES_SERVICE_REFERENCE_LABELS;

  facilitiesContent: FacilitiesPageContent = createDefaultFacilitiesPageContent();
  primaryListItemsText = this.facilitiesContent.primaryListItems.join('\n');
  secondaryListItemsText = this.facilitiesContent.secondaryListItems.join('\n');

  constructor() {
    void this.loadContent();
  }

  ngAfterViewInit(): void {
    this.document.defaultView?.requestAnimationFrame(() => {
      this.document.defaultView?.scrollTo({ top: 0, behavior: 'auto' });
    });
  }

  async loadContent(): Promise<void> {
    this.loading.set(true);
    this.clearFeedback();

    try {
      const content = await firstValueFrom(this.facilitiesContentService.getContent());
      this.applyContent(content);
    } catch (error) {
      this.applyContent(createDefaultFacilitiesPageContent());
      this.feedbackTone.set('error');
      this.feedback.set(
        this.resolveError(
          error,
          'No fue posible cargar el contenido de facilidades. Se muestran los valores predeterminados.'
        )
      );
    } finally {
      this.loading.set(false);
    }
  }

  async save(): Promise<void> {
    this.saving.set(true);
    this.clearFeedback();

    try {
      const savedContent = await firstValueFrom(
        this.facilitiesContentService.updateContent(this.buildContentPayload())
      );

      this.applyContent(savedContent);
      this.feedbackTone.set('success');
      this.feedback.set('El contenido de facilidades se guardó correctamente.');
    } catch (error) {
      this.feedbackTone.set('error');
      this.feedback.set(this.resolveError(error, 'No fue posible guardar el contenido de facilidades.'));
    } finally {
      this.saving.set(false);
    }
  }

  private applyContent(content: FacilitiesPageContent): void {
    this.facilitiesContent = cloneFacilitiesPageContent(content);
    this.primaryListItemsText = this.facilitiesContent.primaryListItems.join('\n');
    this.secondaryListItemsText = this.facilitiesContent.secondaryListItems.join('\n');
  }

  private buildContentPayload(): FacilitiesPageContent {
    // Los campos multilinea del formulario se guardan como arreglos limpios en el JSON.
    return cloneFacilitiesPageContent({
      ...this.facilitiesContent,
      primaryListItems: this.parseLines(this.primaryListItemsText),
      secondaryListItems: this.parseLines(this.secondaryListItemsText),
      serviceCards: this.facilitiesContent.serviceCards.map((service) => ({
        title: service.title.trim(),
        description: service.description.trim()
      }))
    });
  }

  private parseLines(value: string): string[] {
    return value
      .split(/\r?\n/)
      .map((line) => line.trim())
      .filter((line) => line.length > 0);
  }

  private clearFeedback(): void {
    this.feedback.set('');
    this.feedbackTone.set('');
  }

  private resolveError(error: unknown, fallbackMessage: string): string {
    if (error instanceof HttpErrorResponse) {
      return error.error?.message || error.message || fallbackMessage;
    }

    if (error instanceof Error && error.message) {
      return error.message;
    }

    return fallbackMessage;
  }
}
