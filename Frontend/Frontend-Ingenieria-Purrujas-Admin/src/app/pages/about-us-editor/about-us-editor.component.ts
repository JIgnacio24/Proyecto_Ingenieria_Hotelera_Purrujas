import { CommonModule, DOCUMENT } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { AfterViewInit, Component, HostListener, Pipe, PipeTransform, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
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
  private readonly router = inject(Router);
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
  readonly sectionErrors = signal<Record<string, string>>({});
  readonly editingBlock = signal<EditableBlock>(null);
  readonly hasChanges = signal(false);
  readonly activeSection = signal('editor-hero');
  readonly mobileMenuOpen = signal(false);

  readonly editorSections = [
    { id: 'editor-hero', label: 'Encabezado', compactLabel: 'Inicio', icon: 'landscape' },
    { id: 'editor-historia', label: 'Historia', compactLabel: 'Historia', icon: 'history_edu' },
    { id: 'editor-equipo', label: 'Equipo y filosofía', compactLabel: 'Equipo', icon: 'groups' },
    { id: 'editor-mvv', label: 'Misión, visión y valores', compactLabel: 'MVV', icon: 'track_changes' },
    { id: 'editor-galeria', label: 'Galería', compactLabel: 'Galería', icon: 'photo_library' }
  ];

  activeFilter: 'todos' | 'hotel' | 'lugares' = 'todos';
  aboutUsContent: AboutUsPageContent = createDefaultAboutUsPageContent();
  originalContent: AboutUsPageContent = createDefaultAboutUsPageContent();
  aboutUsValuesText = this.aboutUsContent.values.join('\n');
  historyMilestonesText = this.aboutUsContent.historyMilestones.join('\n');
  galleryItems: GalleryImage[] = [];

  get filteredItems(): GalleryImage[] {
    if (this.activeFilter === 'todos') {
      return this.galleryItems.filter(
        (item) => item.id !== 3 && (item.category === 'hotel' || item.category === 'lugares')
      );
    }

    return this.galleryItems.filter((item) => item.id !== 3 && item.category === this.activeFilter);
  }

  constructor() {
    void this.loadContent();
    void this.loadGalleryImages();
  }

  ngAfterViewInit(): void {
    // La entrada desde el dashboard debe abrir el editor desde su encabezado.
    this.scrollToTop('auto');
    this.updateActiveSection();
  }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    this.updateActiveSection();
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
        this.resolveError(error, 'No fue posible cargar el contenido. Se muestran los valores predeterminados.')
      );
    } finally {
      this.loading.set(false);
    }
  }

  async saveContent(): Promise<void> {
    // Persiste el contenido completo; el cliente lo consumirá en su siguiente lectura del endpoint.
    this.saving.set(true);
    this.clearFeedback();

    try {
      if (!this.validateContent()) {
        return;
      }

      const savedContent = await firstValueFrom(
        this.aboutUsContentService.updateContent(this.buildPayload())
      );

      await this.saveChangedGalleryImages();
      this.applyContent(savedContent);
      await this.loadGalleryImages();
      this.hasChanges.set(false);
      this.editingBlock.set(null);
      await this.navigateToPanelWithFeedback('success', 'Los cambios de Sobre nosotros se guardaron correctamente.');
    } catch (error) {
      await this.navigateToPanelWithFeedback(
        'error',
        this.resolveError(error, 'No fue posible guardar los cambios.')
      );
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
    this.feedback.set('Los cambios fueron descartados.');
    this.inlineFeedback.set('');
    this.inlineFeedbackTone.set('');
    this.sectionErrors.set({});
    void this.loadGalleryImages();
    this.scrollToTop();
  }

  editBlock(block: EditableBlock): void {
    this.editingBlock.set(block);
    this.clearFeedback();
  }

  sectionError(sectionId: string): string {
    return this.sectionErrors()[sectionId] ?? '';
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

  scrollToSection(sectionId: string): void {
    this.closeMobileMenu();
    const element = this.document.getElementById(sectionId);
    if (!element) {
      return;
    }

    const top = element.getBoundingClientRect().top + (this.document.defaultView?.scrollY ?? 0) - 92;
    this.document.defaultView?.scrollTo({ top, behavior: 'smooth' });
    this.activeSection.set(sectionId);
  }

  scrollToTop(behavior: ScrollBehavior = 'smooth'): void {
    this.closeMobileMenu();
    this.document.defaultView?.scrollTo({ top: 0, left: 0, behavior });
    this.activeSection.set('editor-hero');
  }

  toggleMobileMenu(): void {
    this.mobileMenuOpen.update((open) => !open);
  }

  closeMobileMenu(): void {
    this.mobileMenuOpen.set(false);
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
      const message = this.resolveError(error, 'No fue posible cargar las imágenes de la galería.');
      this.setSectionError('editor-galeria', message);
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
    this.sectionErrors.set({});
  }

  private validateContent(): boolean {
    const errors: Record<string, string> = {};
    const addError = (sectionId: string, message: string): void => {
      if (!errors[sectionId]) {
        errors[sectionId] = message;
      }
    };

    if (!this.aboutUsContent.historyTag.trim() || !this.aboutUsContent.historyTitle.trim() ||
        !this.aboutUsContent.historyDescription.trim() || !this.aboutUsContent.historyTimelineStartYear.trim() ||
        !this.aboutUsContent.historyTimelineEndLabel.trim() || this.parseLines(this.historyMilestonesText).length === 0) {
      addError('editor-historia', 'Completa todos los campos de Historia antes de guardar.');
    }

    if (!this.aboutUsContent.teamTag.trim() || !this.aboutUsContent.teamTitle.trim() ||
        !this.aboutUsContent.collaboratorsLabel.trim() || !this.aboutUsContent.localTalentLabel.trim() ||
        !this.aboutUsContent.experienceLabel.trim() || this.aboutUsContent.collaboratorsCount === null ||
        this.aboutUsContent.localTalentPercentage === null || this.aboutUsContent.experienceYears === null) {
      addError('editor-equipo', 'Completa todos los campos de Equipo antes de guardar.');
    }

    if (!this.aboutUsContent.directorName.trim() || !this.aboutUsContent.directorTitle.trim() ||
        !this.aboutUsContent.directorBiography.trim()) {
      addError('editor-equipo', 'Completa todos los campos de Direccion antes de guardar.');
    }

    if (!this.aboutUsContent.philosophyTitle.trim() || !this.aboutUsContent.philosophyDescription.trim() ||
        !this.aboutUsContent.philosophyQuote.trim()) {
      addError('editor-equipo', 'Completa todos los campos de Filosofia antes de guardar.');
    }

    if (!this.aboutUsContent.mvvTag.trim() || !this.aboutUsContent.mvvTitle.trim() ||
        !this.aboutUsContent.missionTitle.trim() || !this.aboutUsContent.visionTitle.trim() ||
        !this.aboutUsContent.valuesTitle.trim() || !this.aboutUsContent.mission.trim() ||
        !this.aboutUsContent.vision.trim() || this.parseLines(this.aboutUsValuesText).length === 0) {
      addError('editor-mvv', 'Completa todos los campos de Mision, Vision y Valores antes de guardar.');
    }

    if (!this.aboutUsContent.galleryTag.trim() || !this.aboutUsContent.galleryTitle.trim() ||
        !this.aboutUsContent.gallerySubtext.trim()) {
      addError('editor-galeria', 'Completa todos los textos de Galeria antes de guardar.');
    }

    if (this.galleryItems.some((image) => !image.name.trim() || !image.alt.trim() || !image.caption.trim() || !image.category.trim())) {
      addError('editor-galeria', 'Completa nombre, texto alternativo, leyenda y categoria en todas las imagenes.');
    }

    this.sectionErrors.set(errors);
    const firstSectionId = Object.keys(errors)[0];

    if (!firstSectionId) {
      return true;
    }

    this.editingBlock.set(this.blockForSectionId(firstSectionId));
    this.scrollToSection(firstSectionId);
    return false;
  }

  private setSectionError(sectionId: string, message: string): void {
    this.sectionErrors.set({ ...this.sectionErrors(), [sectionId]: message });
  }

  private sectionIdForBlock(block: EditableBlock): string {
    switch (block) {
      case 'history':
        return 'editor-historia';
      case 'team':
      case 'director':
      case 'philosophy':
        return 'editor-equipo';
      case 'mvv':
        return 'editor-mvv';
      case 'gallery':
        return 'editor-galeria';
      default:
        return 'editor-hero';
    }
  }

  private blockForSectionId(sectionId: string): EditableBlock {
    switch (sectionId) {
      case 'editor-historia':
        return 'history';
      case 'editor-equipo':
        return 'team';
      case 'editor-mvv':
        return 'mvv';
      case 'editor-galeria':
        return 'gallery';
      default:
        return null;
    }
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

  private updateActiveSection(): void {
    const viewportOffset = 130;
    let activeId = this.editorSections[0].id;

    for (const section of this.editorSections) {
      const element = this.document.getElementById(section.id);
      if (!element) {
        continue;
      }

      if (element.getBoundingClientRect().top <= viewportOffset) {
        activeId = section.id;
      }
    }

    this.activeSection.set(activeId);
  }
}
