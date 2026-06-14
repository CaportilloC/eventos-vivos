import { Routes } from '@angular/router';
import { OccupancyReportComponent } from './pages/occupancy-report/occupancy-report.component';

export const reportRoutes: Routes = [
  { path: '', component: OccupancyReportComponent },
  { path: ':id', component: OccupancyReportComponent },
];
