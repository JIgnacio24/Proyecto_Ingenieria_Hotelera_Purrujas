import { CommonModule, DOCUMENT } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { AfterViewInit, Component, Pipe, PipeTransform, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  AboutUsContentService,
  AboutUsPageContent,
  cloneAboutUsPageContent,
  createDefaultAboutUsPageContent
} from '../../core/about-us-content.service';

type EditableBlock = 'history' | 'team' | 'director' | 'philosophy' | 'mvv' | 'gallery' | null;

interface GalleryItem {
  src: string;
  alt: string;
  caption: string;
  category: 'hotel' | 'lugares';
}

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

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly feedback = signal('');
  readonly feedbackTone = signal<'success' | 'error' | ''>('');
  readonly editingBlock = signal<EditableBlock>(null);
  readonly hasChanges = signal(false);

  activeFilter: 'todos' | 'hotel' | 'lugares' = 'todos';
  aboutUsContent: AboutUsPageContent = createDefaultAboutUsPageContent();
  originalContent: AboutUsPageContent = createDefaultAboutUsPageContent();
  aboutUsValuesText = this.aboutUsContent.values.join('\n');
  historyMilestonesText = this.aboutUsContent.historyMilestones.join('\n');

  readonly galleryItems: GalleryItem[] = [
    {
      src: '/images/foto_fondo.png',
      alt: 'Hotel Las Purrujas',
      caption: 'Hotel Las Purrujas',
      category: 'hotel'
    },
    {
      src: '/images/habitaciÃ³n_doble.png',
      alt: 'Habitacion doble',
      caption: 'Habitacion doble',
      category: 'hotel'
    },
    {
      src: '/images/habitacion_doble_2.png',
      alt: 'Habitacion doble con vista',
      caption: 'Habitacion doble con vista balcon',
      category: 'hotel'
    },
    {
      src: '/images/piscinas_naturales.png',
      alt: 'Piscinas naturales del hotel',
      caption: 'Piscinas naturales',
      category: 'hotel'
    },
    {
      src: '/images/restaurante_la_ceiba.png',
      alt: 'Restaurante La Ceiba',
      caption: 'Restaurante La Ceiba',
      category: 'hotel'
    },
    {
      src: '/images/spa.png',
      alt: 'Spa y bienestar',
      caption: 'Spa y bienestar',
      category: 'hotel'
    },
    {
      src: '/images/vista_balcon.png',
      alt: 'Vista desde el balcon',
      caption: 'Vista desde el balcon',
      category: 'hotel'
    },
    {
      src: '/images/villa_familiar.png',
      alt: 'Villa familiar',
      caption: 'Villa familiar',
      category: 'hotel'
    },
    {
      src: '/images/gastronomia_tipica.png',
      alt: 'Gastronomia tipica',
      caption: 'Gastronomia tipica',
      category: 'hotel'
    },
    {
      src: '/images/avistamiento_aves.png',
      alt: 'Avistamiento de aves en los alrededores',
      caption: 'Avistamiento de aves',
      category: 'lugares'
    },
    {
      src: '/images/senderismo_volcan.png',
      alt: 'Senderismo en el volcan Turrialba',
      caption: 'Senderismo en el volcan',
      category: 'lugares'
    },
    {
      src: '/images/senderos.png',
      alt: 'Senderos ecologicos de la zona',
      caption: 'Senderos ecologicos',
      category: 'lugares'
    }
  ];

  get filteredItems(): GalleryItem[] {
    if (this.activeFilter === 'todos') {
      return this.galleryItems;
    }

    return this.galleryItems.filter((item) => item.category === this.activeFilter);
  }

  constructor() {
    void this.loadContent();
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

      this.applyContent(savedContent);
      this.hasChanges.set(false);
      this.editingBlock.set(null);
      this.feedbackTone.set('success');
      this.feedback.set('Los cambios de Sobre Nosotros se guardaron correctamente.');
      this.scrollToTop();
    } catch (error) {
      this.feedbackTone.set('error');
      this.feedback.set(this.resolveError(error, 'No fue posible guardar los cambios.'));
    } finally {
      this.saving.set(false);
    }
  }

  discardChanges(): void {
    // Restaura la ultima version cargada desde la base de datos.
    this.aboutUsContent = cloneAboutUsPageContent(this.originalContent);
    this.aboutUsValuesText = this.aboutUsContent.values.join('\n');
    this.historyMilestonesText = this.aboutUsContent.historyMilestones.join('\n');
    this.hasChanges.set(false);
    this.editingBlock.set(null);
    this.feedbackTone.set('');
    this.feedback.set('Cambios descartados.');
    this.scrollToTop();
  }

  editBlock(block: EditableBlock): void {
    this.editingBlock.set(block);
    this.clearFeedback();
  }

  setFilter(filter: 'todos' | 'hotel' | 'lugares'): void {
    this.activeFilter = filter;
  }

  trackBySrc(_index: number, item: GalleryItem): string {
    return item.src;
  }

  markChanged(): void {
    this.hasChanges.set(true);
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

  private buildPayload(): AboutUsPageContent {
    // Convierte los campos multilinea del editor en listas del contrato AboutUsPageContent.
    return cloneAboutUsPageContent({
      ...this.aboutUsContent,
      historyMilestones: this.parseLines(this.historyMilestonesText),
      values: this.parseLines(this.aboutUsValuesText)
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

  private scrollToTop(behavior: ScrollBehavior = 'smooth'): void {
    this.document.defaultView?.scrollTo({ top: 0, left: 0, behavior });
  }
}
