import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { NotificationService } from '../services/notification.service';
import { catchError, throwError } from 'rxjs';

function isBusinessError(status: number): boolean {
  return status >= 400 && status < 500;
}

export const errorHandlerInterceptor: HttpInterceptorFn = (req, next) => {
  const notification = inject(NotificationService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let title = 'Error';
      let message = 'Ocurrió un error inesperado.';

      if (error.error instanceof ErrorEvent) {
        message = `Error de conexión: ${error.error.message}`;
      } else if (error.status === 0) {
        message = 'No se puede conectar con el servidor. Verifique que el backend esté corriendo.';
      } else if (error.status >= 500) {
        message = 'Error del servidor. Intente nuevamente más tarde.';
      } else if (isBusinessError(error.status)) {
        return throwError(() => error);
      }

      notification.error(title, message);
      return throwError(() => error);
    }),
  );
};
