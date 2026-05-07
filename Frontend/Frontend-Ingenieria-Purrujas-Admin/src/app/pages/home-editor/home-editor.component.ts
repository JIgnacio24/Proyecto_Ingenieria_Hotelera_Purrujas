import { CommonModule, DOCUMENT } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { AfterViewInit, Component, ElementRef, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { GalleryImage, GalleryImagesService } from '../../core/gallery-images.service';
import {
  cloneHomePageContent,
  createDefaultHomePageContent,
  HomeContentService,
  HomePageContent
} from '../../core/home-content.service';

@Component({
  selector: 'app-home-editor',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './home-editor.component.html',
  styleUrl: './home-editor.component.css'
})
export class HomeEditorComponent implements AfterViewInit {
  private readonly document = inject(DOCUMENT);
  private readonly homeContentService = inject(HomeContentService);
  private readonly galleryImagesService = inject(GalleryImagesService);
  private readonly apiAssetBaseUrl = environment.apiBaseUrl.replace(/\/api\/?$/, '');
  private readonly allowedImageExtensions = ['.jpg', '.jpeg', '.png', '.webp'];

  @ViewChild('imageInput') imageInput?: ElementRef<HTMLInputElement>;

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly feedback = signal('');
  readonly feedbackTone = signal<'success' | 'error' | ''>('');
  readonly imageError = signal('');
  readonly hasChanges = signal(false);

  homeContent: HomePageContent = createDefaultHomePageContent();
  originalContent: HomePageContent = createDefaultHomePageContent();
  heroImage: GalleryImage | null = null;
  selectedHeroFile: File | null = null;
  heroImagePreviewUrl = '';

  constructor() {
    void this.loadContent();
  }

  ngAfterViewInit(): void {
    // La ruta debe iniciar siempre en el encabezado del editor.
    this.document.defaultView?.requestAnimationFrame(() => {
      this.document.defaultView?.scrollTo({ top: 0, behavior: 'auto' });
    });
  }

  async loadContent(): Promise<void> {
    this.loading.set(true);
    this.clearFeedback();
    this.imageError.set('');

    try {
      const [content, images] = await Promise.all([
        firstValueFrom(this.homeContentService.getContent()),
        firstValueFrom(this.galleryImagesService.getAll())
      ]);

      this.homeContent = cloneHomePageContent(content);
      this.originalContent = cloneHomePageContent(content);
      this.heroImage = images.find((image) => image.category === 'fondo')
        ?? images.find((image) => image.name === 'foto_fondo.png')
        ?? null;
      this.heroImagePreviewUrl = this.heroImage ? this.imageUrl(this.heroImage.src) : '';
      this.selectedHeroFile = null;
      this.hasChanges.set(false);
    } catch (error) {
      this.homeContent = createDefaultHomePageContent();
      this.originalContent = createDefaultHomePageContent();
      this.feedbackTone.set('error');
      this.feedback.set(this.resolveError(error, 'No fue posible cargar el contenido del inicio.'));
    } finally {
      this.loading.set(false);
    }
  }

  markChanged(): void {
    this.hasChanges.set(true);
  }

  onHeroImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.imageError.set('');

    if (!file) {
      return;
    }

    const lowerName = file.name.toLowerCase();
    const isAllowed = this.allowedImageExtensions.some((extension) => lowerName.endsWith(extension));

    if (!isAllowed) {
      this.selectedHeroFile = null;
      input.value = '';
      this.imageError.set('Formato no permitido. Usa una imagen JPG, JPEG, PNG o WEBP.');
      return;
    }

    this.selectedHeroFile = file;
    this.heroImagePreviewUrl = URL.createObjectURL(file);
    this.markChanged();
  }

  async save(): Promise<void> {
    this.saving.set(true);
    this.clearFeedback();

    try {
      const savedContent = await firstValueFrom(
        this.homeContentService.updateContent(cloneHomePageContent(this.homeContent))
      );

      if (this.heroImage && this.selectedHeroFile) {
        this.heroImage = await firstValueFrom(
          this.galleryImagesService.update(this.heroImage.id, {
            name: this.selectedHeroFile.name,
            alt: this.heroImage.alt || savedContent.heroTitle,
            caption: this.heroImage.caption || savedContent.heroTitle,
            category: 'fondo',
            file: this.selectedHeroFile
          })
        );
        this.heroImagePreviewUrl = this.imageUrl(this.heroImage.src);
      }

      this.homeContent = cloneHomePageContent(savedContent);
      this.originalContent = cloneHomePageContent(savedContent);
      this.selectedHeroFile = null;
      this.hasChanges.set(false);
      this.feedbackTone.set('success');
      this.feedback.set('El hero del inicio se guardo correctamente.');
    } catch (error) {
      this.feedbackTone.set('error');
      this.feedback.set(this.resolveError(error, 'No fue posible guardar el hero del inicio.'));
    } finally {
      this.saving.set(false);
    }
  }

  discard(): void {
    this.homeContent = cloneHomePageContent(this.originalContent);
    this.selectedHeroFile = null;
    this.heroImagePreviewUrl = this.heroImage ? this.imageUrl(this.heroImage.src) : '';
    this.imageInput?.nativeElement && (this.imageInput.nativeElement.value = '');
    this.imageError.set('');
    this.hasChanges.set(false);
  }

  imageUrl(src: string): string {
    if (!src) {
      return '';
    }

    return src.startsWith('http') ? src : `${this.apiAssetBaseUrl}${src}`;
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
