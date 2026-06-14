import { Routes } from '@angular/router';
import { ReservationListComponent } from './pages/reservation-list/reservation-list.component';
import { ReservationFormComponent } from './pages/reservation-form/reservation-form.component';
import { ReservationDetailComponent } from './pages/reservation-detail/reservation-detail.component';
import { ReservationEditComponent } from './pages/reservation-edit/reservation-edit.component';
import { PaymentConfirmComponent } from './pages/payment-confirm/payment-confirm.component';

export const reservationRoutes: Routes = [
  { path: '', component: ReservationListComponent },
  { path: 'create', component: ReservationFormComponent },
  { path: 'confirm', component: PaymentConfirmComponent },
  { path: ':id', component: ReservationDetailComponent },
  { path: ':id/edit', component: ReservationEditComponent },
];
