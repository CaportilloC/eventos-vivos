import { Injectable } from '@angular/core';
import Swal from 'sweetalert2';

export interface ConfirmResult {
  isConfirmed: boolean;
  isDenied: boolean;
  isDismissed: boolean;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  success(title: string, message?: string): void {
    Swal.fire({
      icon: 'success',
      title,
      text: message,
      confirmButtonColor: '#2e37a4',
      confirmButtonText: 'Aceptar',
      timer: 3000,
      timerProgressBar: true,
      customClass: {
        popup: 'ev-swal-popup',
        confirmButton: 'ev-swal-confirm',
      },
    });
  }

  error(title: string, message?: string): void {
    Swal.fire({
      icon: 'error',
      title,
      text: message,
      confirmButtonColor: '#d32f2f',
      confirmButtonText: 'Cerrar',
      customClass: {
        popup: 'ev-swal-popup',
        confirmButton: 'ev-swal-confirm',
      },
    });
  }

  warning(title: string, message?: string): void {
    Swal.fire({
      icon: 'warning',
      title,
      text: message,
      confirmButtonColor: '#f57f17',
      confirmButtonText: 'Entendido',
      customClass: {
        popup: 'ev-swal-popup',
        confirmButton: 'ev-swal-confirm',
      },
    });
  }

  info(title: string, message?: string): void {
    Swal.fire({
      icon: 'info',
      title,
      text: message,
      confirmButtonColor: '#1565c0',
      confirmButtonText: 'Aceptar',
      customClass: {
        popup: 'ev-swal-popup',
        confirmButton: 'ev-swal-confirm',
      },
    });
  }

  async confirm(title: string, message?: string): Promise<ConfirmResult> {
    const result = await Swal.fire({
      icon: 'question',
      title,
      text: message,
      showCancelButton: true,
      confirmButtonColor: '#2e37a4',
      cancelButtonColor: '#6c757d',
      confirmButtonText: 'Sí, continuar',
      cancelButtonText: 'Cancelar',
      reverseButtons: true,
      customClass: {
        popup: 'ev-swal-popup',
        confirmButton: 'ev-swal-confirm',
      },
    });

    return {
      isConfirmed: result.isConfirmed,
      isDenied: result.isDenied ?? false,
      isDismissed: result.dismiss === Swal.DismissReason.cancel || result.dismiss === Swal.DismissReason.backdrop,
    };
  }

  async confirmDanger(title: string, message?: string): Promise<ConfirmResult> {
    const result = await Swal.fire({
      icon: 'warning',
      title,
      text: message,
      showCancelButton: true,
      confirmButtonColor: '#d32f2f',
      cancelButtonColor: '#6c757d',
      confirmButtonText: 'Sí, confirmar',
      cancelButtonText: 'Cancelar',
      reverseButtons: true,
      customClass: {
        popup: 'ev-swal-popup',
        confirmButton: 'ev-swal-confirm',
      },
    });

    return {
      isConfirmed: result.isConfirmed,
      isDenied: result.isDenied ?? false,
      isDismissed: result.dismiss === Swal.DismissReason.cancel || result.dismiss === Swal.DismissReason.backdrop,
    };
  }

  toast(title: string, icon: 'success' | 'error' | 'info' | 'warning' = 'success'): void {
    Swal.fire({
      icon,
      title,
      toast: true,
      position: 'top-end',
      showConfirmButton: false,
      timer: 3000,
      timerProgressBar: true,
      customClass: {
        popup: 'ev-swal-toast',
      },
    });
  }
}
