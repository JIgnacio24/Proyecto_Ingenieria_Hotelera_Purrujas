import { CommonModule, DOCUMENT } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { AfterViewInit, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  cloneGettingTherePageContent,
  createDefaultGettingTherePageContent,
  GettingThereContentService,
  GettingTherePageContent
} from '../../core/getting-there-content.service';

@Component({
  selector: 'app-getting-there-editor',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './getting-there-editor.component.html',
  styleUrl: './getting-there-editor.component.css'
})
export class GettingThereEditorComponent implements AfterViewInit {
  private readonly document = inject(DOCUMENT);
  private readonly router = inject(Router);
  private readonly gettingThereContentService = inject(GettingThereContentService);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly feedback = signal('');
  readonly feedbackTone = signal<'success' | 'error' | ''>('');
  readonly panelError = signal('');
  readonly fieldErrors = signal<Record<string, string>>({});
  readonly hasChanges = signal(false);

  gettingThereContent: GettingTherePageContent = createDefaultGettingTherePageContent();
  originalContent: GettingTherePageContent = createDefaultGettingTherePageContent();
  directionsItemsText = this.gettingThereContent.directionsItems.join('\n');

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
      const content = await firstValueFrom(this.gettingThereContentService.getContent());
      this.gettingThereContent = cloneGettingTherePageContent(content);
      this.originalContent = cloneGettingTherePageContent(content);
      this.directionsItemsText = this.gettingThereContent.directionsItems.join('\n');
      this.hasChanges.set(false);
    } catch (error) {
      this.gettingThereContent = createDefaultGettingTherePageContent();
      this.originalContent = createDefaultGettingTherePageContent();
      this.directionsItemsText = this.gettingThereContent.directionsItems.join('\n');
      this.feedbackTone.set('error');
      this.feedback.set(
        this.resolveError(error, 'No fue posible cargar el contenido de Cómo llegar. Se muestran los valores predeterminados.')
      );
    } finally {
      this.loading.set(false);
    }
  }

  markChanged(): void {
    this.hasChanges.set(true);
    this.panelError.set('');
  }

  async save(): Promise<void> {
    this.saving.set(true);
    this.clearFeedback();

    try {
      if (!this.validateContent()) {
        return;
      }

      const savedContent = await firstValueFrom(
        this.gettingThereContentService.updateContent(this.buildPayload())
      );

      this.gettingThereContent = cloneGettingTherePageContent(savedContent);
      this.originalContent = cloneGettingTherePageContent(savedContent);
      this.directionsItemsText = this.gettingThereContent.directionsItems.join('\n');
      this.hasChanges.set(false);
      await this.navigateToPanelWithFeedback('success', 'El contenido de Cómo llegar se guardó correctamente.');
    } catch (error) {
      await this.navigateToPanelWithFeedback(
        'error',
        this.resolveError(error, 'No fue posible guardar el contenido de Cómo llegar.')
      );
    } finally {
      this.saving.set(false);
    }
  }

  discard(): void {
    this.gettingThereContent = cloneGettingTherePageContent(this.originalContent);
    this.directionsItemsText = this.gettingThereContent.directionsItems.join('\n');
    this.panelError.set('');
    this.fieldErrors.set({});
    this.feedback.set('');
    this.feedbackTone.set('');
    this.hasChanges.set(false);
  }

  private buildPayload(): GettingTherePageContent {
    return cloneGettingTherePageContent({
      ...this.gettingThereContent,
      directionsItems: this.parseLines(this.directionsItemsText)
    });
  }

  private validateContent(): boolean {
    const errors: Record<string, string> = {};

    if (!this.gettingThereContent.sectionTag.trim()) {
      errors['sectionTag'] = 'La etiqueta de sección es obligatoria.';
    }

    if (!this.gettingThereContent.sectionTitle.trim()) {
      errors['sectionTitle'] = 'El título de sección es obligatorio.';
    }

    if (!this.gettingThereContent.sectionSubtext.trim()) {
      errors['sectionSubtext'] = 'El texto introductorio es obligatorio.';
    }

    if (!this.gettingThereContent.coordinatesTitle.trim()) {
      errors['coordinatesTitle'] = 'El título de coordenadas es obligatorio.';
    }

    if (!this.gettingThereContent.coordinatesDescription.trim()) {
      errors['coordinatesDescription'] = 'La descripción de coordenadas es obligatoria.';
    }

    if (!this.gettingThereContent.mapButtonLabel.trim()) {
      errors['mapButtonLabel'] = 'El texto del botón del mapa es obligatorio.';
    }

    if (this.parseLines(this.directionsItemsText).length === 0) {
      errors['directionsItemsText'] = 'Agrega al menos una indicación de llegada.';
    }

    this.fieldErrors.set(errors);

    if (Object.keys(errors).length === 0) {
      return true;
    }

    this.panelError.set('Revisa los campos marcados antes de guardar.');
    this.scrollToEditorPanel();
    return false;
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
    this.panelError.set('');
    this.fieldErrors.set({});
  }

  private scrollToEditorPanel(): void {
    const element = this.document.getElementById('getting-there-editor-panel');
    if (!element) {
      return;
    }

    const top = element.getBoundingClientRect().top + (this.document.defaultView?.scrollY ?? 0) - 92;
    this.document.defaultView?.scrollTo({ top, behavior: 'smooth' });
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

  private navigateToPanelWithFeedback(tone: 'success' | 'error', message: string): Promise<boolean> {
    return this.router.navigate(['/panel'], {
      state: {
        adminFeedback: {
          tone,
          message
        }
      }
    });
  }
}
