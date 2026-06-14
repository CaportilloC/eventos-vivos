import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatToolbar } from '@angular/material/toolbar';
import { MatSidenav, MatSidenavContainer, MatSidenavContent } from '@angular/material/sidenav';
import { MatIcon } from '@angular/material/icon';
import { MatIconButton } from '@angular/material/button';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';

interface NavItem {
  icon: string;
  label: string;
  route?: string;
  externalUrl?: string;
  exact?: boolean;
}

interface NavSection {
  label: string;
  items: NavItem[];
}

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbar,
    MatSidenav,
    MatSidenavContainer,
    MatSidenavContent,
    MatIcon,
    MatIconButton,
  ],
  template: `
    <mat-sidenav-container class="shell-container">
      <!-- Sidebar -->
      <mat-sidenav
        #drawer
        [mode]="isSmallScreen() ? 'over' : 'side'"
        [opened]="!isSmallScreen()"
        class="shell-sidenav"
      >
        <!-- Branding block -->
        <div class="sidebar-brand">
          <div class="brand-icon">
            <mat-icon>celebration</mat-icon>
          </div>
          <div class="brand-text">
            <span class="brand-name">Eventos Vivos</span>
            <span class="brand-sub">Plataforma de Gestión</span>
          </div>
        </div>

        <!-- Navigation -->
        <nav class="sidebar-nav">
          @for (section of navSections; track section.label) {
            <div class="nav-section-label">{{ section.label }}</div>
            @for (item of section.items; track item.label) {
              @if (item.route) {
                <a
                  class="nav-item"
                  [routerLink]="[item.route]"
                  routerLinkActive="active"
                  [routerLinkActiveOptions]="{ exact: item.exact ?? false }"
                >
                  <mat-icon class="nav-icon">{{ item.icon }}</mat-icon>
                  <span class="nav-label">{{ item.label }}</span>
                </a>
              } @else if (item.externalUrl) {
                <a
                  class="nav-item"
                  [href]="item.externalUrl"
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  <mat-icon class="nav-icon">open_in_new</mat-icon>
                  <span class="nav-label">{{ item.label }}</span>
                </a>
              }
            }
          }
        </nav>

        <!-- Footer -->
        <div class="sidebar-footer">
          <div class="sidebar-footer-info">
            <div class="footer-dot"></div>
            <span class="footer-text">Sistema Operativo</span>
          </div>
        </div>
      </mat-sidenav>

      <!-- Main content area -->
      <mat-sidenav-content class="shell-content">
        <!-- Top header/toolbar -->
        <mat-toolbar class="shell-toolbar">
          <button mat-icon-button (click)="drawer.toggle()" class="menu-btn">
            <mat-icon>menu</mat-icon>
          </button>
          <span class="toolbar-breadcrumb">Eventos Vivos</span>
          <span class="toolbar-spacer"></span>
          <div class="toolbar-meta">
            <span class="toolbar-meta-dot"></span>
            <span class="toolbar-meta-text">Prueba Técnica</span>
          </div>
        </mat-toolbar>

        <!-- Page content -->
        <main class="shell-main">
          <router-outlet />
        </main>
      </mat-sidenav-content>
    </mat-sidenav-container>
  `,
  styles: [
    `
    /* ─── Container ────────────────────────────────────────── */
    .shell-container {
      height: 100vh;
    }

    /* ─── Sidebar ──────────────────────────────────────────── */
    .shell-sidenav {
      width: 260px;
      border-right: 0;
      background: var(--ev-surface-sidebar);
      display: flex;
      flex-direction: column;
      overflow: hidden;
    }

    .sidebar-brand {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 20px 16px 24px;
      border-bottom: 1px solid rgba(255, 255, 255, 0.08);
      flex-shrink: 0;
    }

    .brand-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 40px;
      height: 40px;
      border-radius: 10px;
      background: linear-gradient(135deg, #2e37a4, #4f56b9);
      color: #fff;
      flex-shrink: 0;

      mat-icon {
        font-size: 22px;
        width: 22px;
        height: 22px;
        line-height: 22px;
      }
    }

    .brand-text {
      display: flex;
      flex-direction: column;
      line-height: 1.3;
    }

    .brand-name {
      font-size: 16px;
      font-weight: 600;
      color: #ffffff;
      letter-spacing: 0.01em;
    }

    .brand-sub {
      font-size: 10px;
      color: rgba(255, 255, 255, 0.4);
      letter-spacing: 0.04em;
      text-transform: uppercase;
    }

    /* ─── Navigation (custom flex — no MDC) ────────────────── */
    .sidebar-nav {
      padding: 0 12px 12px;
      flex: 1;
      overflow-y: auto;
    }

    .nav-section-label {
      font-size: 10px;
      font-weight: 700;
      color: rgba(255, 255, 255, 0.35);
      text-transform: uppercase;
      letter-spacing: 0.08em;
      padding: 20px 12px 6px;
    }

    .nav-item {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 9px 12px;
      margin-bottom: 1px;
      border-radius: 8px;
      cursor: pointer;
      color: var(--ev-text-sidebar);
      text-decoration: none;
      transition: background var(--ev-transition-fast), color var(--ev-transition-fast);
      height: 40px;
      line-height: 1;
    }

    .nav-item:hover {
      background: var(--ev-surface-sidebar-hover);
      color: var(--ev-text-sidebar-active);
    }

    .nav-item:hover .nav-icon {
      color: #818cf8;
    }

    .nav-item.active {
      background: var(--ev-surface-sidebar-active) !important;
      color: var(--ev-text-sidebar-active) !important;
    }

    .nav-item.active .nav-icon {
      color: #818cf8 !important;
    }

    /* ─── Icon alignment — definitive fix ──────────────────── */
    .nav-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 20px;
      width: 20px;
      height: 20px;
      line-height: 20px;
      flex-shrink: 0;
      color: rgba(255, 255, 255, 0.5);
      transition: color var(--ev-transition-fast);
    }

    .nav-label {
      font-size: 13px;
      font-weight: 400;
      letter-spacing: 0.01em;
      line-height: 1;
      white-space: nowrap;
    }

    /* Sidebar footer */
    .sidebar-footer {
      flex-shrink: 0;
      padding: 12px 16px 16px;
      border-top: 1px solid rgba(255, 255, 255, 0.06);
    }

    .sidebar-footer-info {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .footer-dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background: #22c55e;
      flex-shrink: 0;
    }

    .footer-text {
      font-size: 11px;
      color: rgba(255, 255, 255, 0.4);
      letter-spacing: 0.03em;
    }

    /* ─── Toolbar / Header ─────────────────────────────────── */
    .shell-content {
      display: flex;
      flex-direction: column;
    }

    .shell-toolbar {
      position: sticky;
      top: 0;
      z-index: 10;
      background: var(--ev-surface-card);
      color: var(--ev-text-primary);
      border-bottom: 1px solid rgba(0, 0, 0, 0.06);
      height: 56px;
      padding: 0 16px;
    }

    .menu-btn {
      color: var(--ev-text-secondary);
      margin-right: 8px;
    }

    .toolbar-breadcrumb {
      font-size: 15px;
      font-weight: 500;
      color: var(--ev-text-primary);
    }

    .toolbar-spacer {
      flex: 1;
    }

    .toolbar-meta {
      display: flex;
      align-items: center;
      gap: 6px;
      font-size: 12px;
      color: var(--ev-text-secondary);
    }

    .toolbar-meta-dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background: #22c55e;
      flex-shrink: 0;
    }

    .toolbar-meta-text {
      letter-spacing: 0.02em;
    }

    /* ─── Main content ─────────────────────────────────────── */
    .shell-main {
      flex: 1;
      padding: 28px 32px;
      overflow-y: auto;
      background: var(--ev-surface-page);
    }

    @media (max-width: 768px) {
      .shell-main {
        padding: 20px 16px;
      }
    }
    `,
  ],
})
export class ShellComponent {
  private readonly breakpointObserver = inject(BreakpointObserver);

  protected readonly isSmallScreen = toSignal(
    this.breakpointObserver.observe([Breakpoints.Handset]).pipe(
      map((result) => result.matches),
    ),
    { initialValue: false },
  );

  protected readonly navSections: NavSection[] = [
    {
      label: 'Principal',
      items: [
        { icon: 'dashboard', label: 'Dashboard', route: '/dashboard', exact: true },
      ],
    },
    {
      label: 'Gestión',
      items: [
        { icon: 'event', label: 'Eventos', route: '/events' },
        { icon: 'confirmation_number', label: 'Reservas', route: '/reservations' },
        { icon: 'location_city', label: 'Catálogos', route: '/venues' },
      ],
    },
    {
      label: 'Reportes',
      items: [
        { icon: 'bar_chart', label: 'Ocupación', route: '/reports' },
      ],
    },
    {
      label: 'Sistema',
      items: [
        { icon: 'api', label: 'API / Swagger', externalUrl: '/swagger' },
      ],
    },
  ];
}
