import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, HostListener, Inject } from '@angular/core';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { filter } from 'rxjs';

const HOME_SECTION_IDS = ['home', 'facilidades', 'tarifas', 'como-llegar', 'contactenos'] as const;
type HomeSectionId = typeof HOME_SECTION_IDS[number];

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css'
})
export class NavbarComponent {
  currentPath = '/';
  currentFragment = 'home';
  menuOpen = false;

  constructor(
    private router: Router,
    @Inject(DOCUMENT) private document: Document
  ) {
    this.updateCurrentLocation(this.router.url);

    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => {
        this.updateCurrentLocation(event.urlAfterRedirects);
        this.closeMenu();
        queueMicrotask(() => this.updateActiveSection());
      });
  }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    this.updateActiveSection();
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    if ((this.document.defaultView?.innerWidth ?? 0) > 900) {
      this.closeMenu();
    }
    this.updateActiveSection();
  }

  toggleMenu(): void {
    this.menuOpen = !this.menuOpen;
  }

  closeMenu(): void {
    this.menuOpen = false;
  }

  isRouteActive(path: string): boolean {
    return this.currentPath === path;
  }

  isHomeFragmentActive(fragment: string): boolean {
    return this.currentPath === '/' && this.currentFragment === fragment;
  }

  async goToSection(event: Event, sectionId: HomeSectionId): Promise<void> {
    event.preventDefault();
    this.currentFragment = sectionId;
    this.closeMenu();

    if (this.currentPath !== '/') {
      await this.router.navigate(['/'], { fragment: sectionId });
      this.document.defaultView?.setTimeout(() => this.scrollToSection(sectionId), 80);
      return;
    }

    await this.router.navigate([], { fragment: sectionId });
    this.scrollToSection(sectionId);
  }

  private updateCurrentLocation(url: string): void {
    const tree = this.router.parseUrl(url);
    const primarySegments = tree.root.children['primary']?.segments ?? [];
    const path = primarySegments.map((segment) => segment.path).join('/');

    this.currentPath = path ? `/${path}` : '/';
    this.currentFragment = tree.fragment ?? 'home';
  }

  private scrollToSection(sectionId: HomeSectionId): void {
    const element = this.document.getElementById(sectionId);
    if (!element) {
      return;
    }

    const targetTop = element.getBoundingClientRect().top
      + (this.document.defaultView?.scrollY ?? 0)
      - this.navOffset();

    this.document.defaultView?.scrollTo({
      top: Math.max(targetTop, 0),
      left: 0,
      behavior: 'smooth'
    });

    this.currentFragment = sectionId;
  }

  private updateActiveSection(): void {
    const activeOffset = this.navOffset() + 8;
    let activeSection: HomeSectionId | null = null;

    for (const sectionId of HOME_SECTION_IDS) {
      const element = this.document.getElementById(sectionId);
      if (!element) {
        continue;
      }

      const rect = element.getBoundingClientRect();
      if (rect.top <= activeOffset && rect.bottom > activeOffset) {
        activeSection = sectionId;
        break;
      }

      if (rect.top <= activeOffset) {
        activeSection = sectionId;
      }
    }

    if (activeSection) {
      this.currentPath = '/';
      this.currentFragment = activeSection;
    }
  }

  private navOffset(): number {
    const nav = this.document.querySelector('.top-nav');
    return nav instanceof HTMLElement ? nav.offsetHeight : 0;
  }
}
