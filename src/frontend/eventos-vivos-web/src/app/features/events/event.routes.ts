import { Routes } from '@angular/router';
import { EventListComponent } from './pages/event-list/event-list.component';
import { EventCreateComponent } from './pages/event-create/event-create.component';
import { EventDetailComponent } from './pages/event-detail/event-detail.component';
import { EventEditComponent } from './pages/event-edit/event-edit.component';

export const eventRoutes: Routes = [
  { path: '', component: EventListComponent },
  { path: 'create', component: EventCreateComponent },
  { path: ':id', component: EventDetailComponent },
  { path: ':id/edit', component: EventEditComponent },
];
