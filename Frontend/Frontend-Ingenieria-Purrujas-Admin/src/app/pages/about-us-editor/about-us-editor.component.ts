import { CommonModule, DOCUMENT } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { AfterViewInit, Component, Pipe, PipeTransform, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AboutUsContentService,
  AboutUsPageContent,
  cloneAboutUsPageContent,
  createDefaultAboutUsPageContent
} from '../../core/about-us-content.service';
import { GalleryImage, GalleryImagesService } from '../../core/gallery-images.service';

type EditableBlock = 'history' | 'team' | 'director' | 'philosophy' | 'mvv' | 'gallery' | null;

@Pipe({
  name: 'replaceNewlines',
  standalone: true,
  pure: true
})
export class ReplaceNewlinesPipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    if (!value) {
      return '';
    }

    // Conserva los saltos de parrafo escritos por el administrador.
    return value
      .split('\n')
      .map((paragraph) => `<p>${paragraph.trim()}</p>`)
      .join('');
  }
}

@Component({
  selector: 'app-about-us-editor',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, ReplaceNewlinesPipe],
  templateUrl: './about-us-editor.component.html',
  styleUrl: './about-us-editor.component.css'
})
export class AboutUsEditorComponent implements AfterViewInit {
  private readonly document = inject(DOCUMENT);
  private readonly aboutUsContentService = inject(AboutUsContentService);
  private readonly galleryImagesService = inject(GalleryImagesService);
  private readonly apiAssetBaseUrl = environment.apiBaseUrl.replace(/\/api\/?$/, '');
  private readonly selectedGalleryFiles = new Map<number, File>();
  private readonly changedGalleryImageIds = new Set<number>();
  private readonly galleryImageErrors = new Map<number, string>();

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly feedback = signal('');
  readonly feedbackTone = signal<'success' | 'error' | ''>('');
  readonly inlineFeedback = signal('');
  readonly inlineFeedbackTone = signal<'success' | 'error' | ''>('');
  readonly editingBlock = signal<EditableBlock>(null);
  readonly hasChanges = signal(false);

  activeFilter: 'todos' | 'hotel' | 'lugares' = 'todos';
  aboutUsContent: AboutUsPageContent = createDefaultAboutUsPageContent();
  originalContent: AboutUsPageContent = createDefaultAboutUsPageContent();
  aboutUsValuesText = this.aboutUsContent.values.join('\n');
  historyMilestonesText = this.aboutUsContent.historyMilestones.join('\n');
  galleryItems: GalleryImage[] = [];

  get filteredItems(): GalleryImage[] {
    if (this.activeFilter === 'todos') {
      return this.galleryItems.filter((item) => item.category === 'hotel' || item.category === 'lugares');
    }

    return this.galleryItems.filter((item) => item.category === this.activeFilter);
  }

  constructor() {
    void this.loadContent();
    void this.loadGalleryImages();
  }

  ngAfterViewInit(): void {
    // La entrada desde el dashboard debe abrir el editor desde su encabezado.
    this.scrollToTop('auto');
  }

  async loadContent(): Promise<void> {
    // Carga el JSON vigente para que la vista editable sea un espejo del frontend cliente.
    this.loading.set(true);
    this.clearFeedback();

    try {
      const content = await firstValueFrom(this.aboutUsContentService.getContent());
      this.applyContent(content);
      this.hasChanges.set(false);
      this.editingBlock.set(null);
    } catch (error) {
      this.applyContent(createDefaultAboutUsPageContent());
      this.feedbackTone.set('error');
      this.feedback.set(
        this.resolveError(error, 'No fue posible cargar el contenido. Se muestran los valores base.')
      );
    } finally {
      this.loading.set(false);
    }
  }

  async saveContent(): Promise<void> {
    // Persiste el contenido completo; el cliente lo consumira en su siguiente lectura del endpoint.
    this.saving.set(true);
    this.clearFeedback();

    try {
      const savedContent = await firstValueFrom(
        this.aboutUsContentService.updateContent(this.buildPayload())
      );

      await this.saveChangedGalleryImages();
      this.applyContent(savedContent);
      await this.loadGalleryImages();
      this.hasChanges.set(false);
      this.editingBlock.set(null);
      this.feedbackTone.set('success');
      this.feedback.set('Los cambios de Sobre Nosotros se guardaron correctamente.');
      this.scrollToTop();
    } catch (error) {
      this.feedbackTone.set('error');
      const message = this.resolveError(error, 'No fue posible guardar los cambios.');
      this.feedback.set(message);
      this.inlineFeedbackTone.set('error');
      this.inlineFeedback.set(message);
    } finally {
      this.saving.set(false);
    }
  }

  discardChanges(): void {
    // Restaura la ultima version cargada desde la base de datos.
    this.aboutUsContent = cloneAboutUsPageContent(this.originalContent);
    this.aboutUsValuesText = this.aboutUsContent.values.join('\n');
    this.historyMilestonesText = this.aboutUsContent.historyMilestones.join('\n');
    this.selectedGalleryFiles.clear();
    this.changedGalleryImageIds.clear();
    this.galleryImageErrors.clear();
    this.hasChanges.set(false);
    this.editingBlock.set(null);
    this.feedbackTone.set('');
    this.feedback.set('Cambios descartados.');
    this.inlineFeedback.set('');
    this.inlineFeedbackTone.set('');
    void this.loadGalleryImages();
    this.scrollToTop();
  }

  editBlock(block: EditableBlock): void {
    this.editingBlock.set(block);
    this.clearFeedback();
  }

  setFilter(filter: 'todos' | 'hotel' | 'lugares'): void {
    this.activeFilter = filter;
  }

  trackBySrc(_index: number, item: GalleryImage): string {
    return String(item.id);
  }

  markChanged(): void {
    this.hasChanges.set(true);
  }

  imageUrl(src: string): string {
    if (src.startsWith('http') || src.startsWith('data:') || src.startsWith('blob:')) {
      return src;
    }

    return `${this.apiAssetBaseUrl}${src}`;
  }

  updateGalleryImage(image: GalleryImage): void {
    this.changedGalleryImageIds.add(image.id);
    this.markChanged();
  }

  onGalleryImageFileSelected(event: Event, image: GalleryImage): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    const allowedExtensions = ['.jpg', '.jpeg', '.png', '.webp'];
    const extension = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();
    if (!allowedExtensions.includes(extension)) {
      this.galleryImageErrors.set(
        image.id,
        'Formato no permitido. Usa una imagen JPG, PNG o WEBP.'
      );
      this.selectedGalleryFiles.delete(image.id);
      input.value = '';
      return;
    }

    this.galleryImageErrors.delete(image.id);
    this.selectedGalleryFiles.set(image.id, file);
    this.changedGalleryImageIds.add(image.id);
    image.src = URL.createObjectURL(file);
    this.markChanged();
  }

  galleryImageError(imageId: number): string {
    return this.galleryImageErrors.get(imageId) ?? '';
  }

  getInitials(name: string): string {
    return name
      .split(' ')
      .slice(0, 2)
      .map((word) => word.charAt(0))
      .join('')
      .toUpperCase();
  }

  private applyContent(content: AboutUsPageContent): void {
    // Mantiene una copia editable y otra de referencia para descartar cambios.
    this.aboutUsContent = cloneAboutUsPageContent(content);
    this.originalContent = cloneAboutUsPageContent(content);
    this.aboutUsValuesText = this.aboutUsContent.values.join('\n');
    this.historyMilestonesText = this.aboutUsContent.historyMilestones.join('\n');
  }

  private async loadGalleryImages(): Promise<void> {
    try {
      this.galleryItems = await firstValueFrom(this.galleryImagesService.getAll());
    } catch (error) {
      const message = this.resolveError(error, 'No fue posible cargar las imagenes de galeria.');
      this.feedbackTone.set('error');
      this.feedback.set(message);
      this.inlineFeedbackTone.set('error');
      this.inlineFeedback.set(message);
    }
  }

  private buildPayload(): AboutUsPageContent {
    // Convierte los campos multilinea del editor en listas del contrato AboutUsPageContent.
    return cloneAboutUsPageContent({
      ...this.aboutUsContent,
      historyMilestones: this.parseLines(this.historyMilestonesText),
      values: this.parseLines(this.aboutUsValuesText)
    });
  }

  private async saveChangedGalleryImages(): Promise<void> {
    const changedImages = this.galleryItems.filter((image) => this.changedGalleryImageIds.has(image.id));

    for (const image of changedImages) {
      await firstValueFrom(
        this.galleryImagesService.update(image.id, {
          name: image.name,
          alt: image.alt,
          caption: image.caption,
          category: image.category,
          file: this.selectedGalleryFiles.get(image.id) ?? null
        })
      );
    }

    this.selectedGalleryFiles.clear();
    this.changedGalleryImageIds.clear();
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
    this.inlineFeedback.set('');
    this.inlineFeedbackTone.set('');
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

  private scrollToTop(behavior: ScrollBehavior = 'smooth'): void {
    this.document.defaultView?.scrollTo({ top: 0, left: 0, behavior });
  }
}
