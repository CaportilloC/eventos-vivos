import { Routes } from '@angular/router';
import { ShellComponent } from './core/layout/shell.component';

export const routes: Routes = [
  {
    path: '',
    component: ShellComponent,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/pages/dashboard.component').then(
            (m) => m.DashboardComponent,
          ),
      },
      {
        path: 'events',
        loadChildren: () =>
          import('./features/events/event.routes').then((m) => m.eventRoutes),
      },
      {
        path: 'reservations',
        loadChildren: () =>
          import('./features/reservations/reservation.routes').then(
            (m) => m.reservationRoutes,
          ),
      },
      {
        path: 'venues',
        loadChildren: () =>
          import('./features/venues/venue.routes').then((m) => m.venueRoutes),
      },
      {
        path: 'reports',
        loadChildren: () =>
          import('./features/reports/reports.routes').then((m) => m.reportRoutes),
      },
      { path: '**', redirectTo: 'dashboard' },
    ],
  },
];
